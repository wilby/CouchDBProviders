using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Web.Security;
using Wcjj.CouchClient;
using System.Text.RegularExpressions;
using System.Net;
using System.Diagnostics;

namespace CouchDBMembershipProvider
{
    /// <summary>
    /// Implemented as defined by http://msdn.microsoft.com/en-us/library/f1kyba5e%28v=vs.100%29.aspx
    /// </summary>
    public class MembershipProvider : System.Web.Security.MembershipProvider
    {

        #region Private Members

        private const string ProviderName = "CouchDBMembership";

        private int _maxInvalidPasswordAttempts;
        private int _passwordAttemptWindow;
        private int _minRequiredNonAlphanumericCharacters;
        private int _minRequiredPasswordLength;
        private string _passwordStrengthRegularExpression;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private MembershipPasswordFormat _passwordFormat;
        private string _hashAlgorithm;
        private string _validationKey;
        private Client _Client;
        private string _twoKeyViewFormatString = "[\"{0}\", \"{1}\"]";
        #endregion

        #region Overriden Public Members

        public string CouchConnectionStringName { get; set; }

        public string ProxyConnectionStringName { get; set; }

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _passwordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }

        #endregion

        #region Overriden Public Methods
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ProviderException("There are no membership configuration settings.");
            if (string.IsNullOrEmpty(name))
                name = "CouchDBMembershipProvider";
            if (string.IsNullOrEmpty(config["description"]))
                config["description"] = "An Asp.Net membership provider for the CouchDB document database.";

            base.Initialize(name, config);

            //Set all the public properties from the membership settings in web.config
            InitConfigSettings(config);
            //Get the machine key and hash algorithm from the web.config
            InitPasswordEncryptionSettings(config);
            //Get the couch connection settings from the web.config
            InitializeCouchSpecificSettings(config);
        }

        private void InitConfigSettings(NameValueCollection config)
        {
            ApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            _maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            _passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            _minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            _minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            _passwordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
            _enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            _enablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            _requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            _requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
        }

        private void InitPasswordEncryptionSettings(NameValueCollection config)
        {
            MachineKeySection machineKey = ConfigurationManager.GetSection("system.web/machineKey") as MachineKeySection;

            //System.Configuration.Configuration cfg = WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);            
            //MachineKeySection machineKey = cfg.GetSection("system.web/machineKey") as MachineKeySection;
            _hashAlgorithm = machineKey.ValidationAlgorithm;
            _validationKey = machineKey.ValidationKey;

            if (machineKey.ValidationKey.Contains("AutoGenerate"))
            {
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                {
                    throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
                }
            }

            string passFormat = config["passwordFormat"];
            if (passFormat == null)
            {
                passFormat = "Hashed";
            }

            switch (passFormat)
            {
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("The password format from the custom provider is not supported.");
            }
        }

        private void InitializeCouchSpecificSettings(NameValueCollection config)
        {
            string conString = config["connectionStringName"];
            string proxyConString = config["proxyConnectionStringName"];

            if (string.IsNullOrEmpty(conString))
                throw new ProviderException(string.Format(
                    "The connection string name in membership settings is wrong or not set or the connection string {0} does not exist.",
                    config["connectionStringName"]));

            CouchDBClient.ConnectionStringName = conString;

            if (!string.IsNullOrEmpty(proxyConString))
                CouchDBClient.ProxyConnectionStringName = proxyConString;

            _Client = CouchDBClient.Instance;

            CouchViews cv = new CouchViews();            
            cv.CreateViews(_Client);
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {

            var exists = false;
            var existsView = _Client.GetView<bool, CouchDocument>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME_EXISTS,
                new NameValueCollection() { { "key", string.Format(_twoKeyViewFormatString, username, ApplicationName) } });

            if (existsView.Rows.Count() == 1)
                exists = true;

            if(exists) {
                status = MembershipCreateStatus.DuplicateUserName;
                return null;
            }

            

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);

            var validPass = IsValidPassword(password);

            if (args.Cancel || !validPass)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            //If we require a question and answer for password reset/retrieval and they were not provided throw exception
            if (((_enablePasswordReset || _enablePasswordRetrieval) && _requiresQuestionAndAnswer) && string.IsNullOrEmpty(passwordAnswer))
                throw new ArgumentException("Requires question and answer is set to true and a question and answer were not provided.");

            var user = new User();
            user.Username = username;
            user.PasswordSalt = PasswordUtil.CreateRandomSalt();
            user.PasswordHash = EncodePassword(password, user.PasswordSalt);
            user.Email = email;
            user.ApplicationName = ApplicationName;
            user.DateCreated = DateTime.Now;
            user.PasswordQuestion = passwordQuestion;
            user.PasswordAnswer = string.IsNullOrEmpty(passwordAnswer) ? passwordAnswer : EncodePassword(passwordAnswer, user.PasswordSalt);
            user.IsApproved = isApproved;
            user.IsLockedOut = false;
            user.IsOnline = false;

            if (RequiresUniqueEmail)
            {       
                var existingUser = _Client.GetView<string, CouchDocument>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_BY_Email_AND_APPNAME,
                new NameValueCollection() { {"key", string.Format(_twoKeyViewFormatString, email, ApplicationName) } });                        

                if (existingUser.Rows.Count() > 0)
                {
                    status = MembershipCreateStatus.DuplicateEmail;
                    return null;
                }
            }

            _Client.SaveDocument<User>(user);
            status = MembershipCreateStatus.Success;
            return UserToMembershipUser(user);
        }

        /// <summary>
        /// Deletes a user from the membership database. 
        /// The parameter deleteAllRelatedData is currently ignored as roles are stored in the user document and no profile provider is in place.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="deleteAllRelatedData"></param>
        /// <returns></returns>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            var userView = _Client.GetView<User, User>(CouchViews.DESIGN_DOC_AUTH, CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME, 
                new NameValueCollection() { {"key", string.Format(_twoKeyViewFormatString,username, ApplicationName) } });

            if (userView.Rows.Count() == 0)            
                return false;

            var doc = userView.Rows.FirstOrDefault().Value;
            try
            {
                _Client.DeleteDocument(doc);
            }
            catch (WebException wex)
            {
                Debug.Write(wex.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Searches for any user whose email starts with the value provided by emailToMatch. 
        /// The original implementation guide calls for returning any users whose email 
        /// contains the emailToMatch but without couchdb-lucene or other full text search
        /// provider this is not possible.
        /// </summary>
        /// <param name="emailToMatch">A full email or substring of an email to match</param>
        /// <param name="pageIndex">The index of a page for pagination</param>
        /// <param name="pageSize">The number of records to return per page</param>
        /// <param name="totalRecords">Total records returned, feedback for when search results are less than pageSize</param>
        /// <returns>MembershipUserCollection</returns>
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsersBy(emailToMatch, CouchViews.AUTH_VIEW_NAME_BY_Email_AND_APPNAME, pageIndex, pageSize, out totalRecords);
        }

        /// <summary>
        /// Searches for any user that starts with the name provided by usernameToMatch. 
        /// The original implementation guide calls for returning any users whose username 
        /// contains the usernameToMatch but without couchdb-lucene or other full text search
        /// provider this is not possible.
        /// </summary>
        /// <param name="usernameToMatch">A full username or substring of a username to match</param>
        /// <param name="pageIndex">The index of a page for pagination</param>
        /// <param name="pageSize">The number of records to return per page</param>
        /// <param name="totalRecords">Total records returned, feedback for when search results are less than pageSize</param>
        /// <returns>MembershipUserCollection</returns>
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsersBy(usernameToMatch, CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME, pageIndex, pageSize, out totalRecords);
        }

        private MembershipUserCollection FindUsersBy(string nameOrEmailToMatch, string authViewName, int pageIndex, int pageSize, out int totalRecords)
        {
            var userView = _Client.GetView<User, User>(
                CouchViews.DESIGN_DOC_AUTH,
                authViewName,
                new NameValueCollection() { { "startkey", string.Format(_twoKeyViewFormatString, nameOrEmailToMatch, ApplicationName) },
                    { "endkey", string.Format(_twoKeyViewFormatString, nameOrEmailToMatch + "9999", ApplicationName) },
                {"limit", pageSize.ToString() }, {"skip", (pageSize * pageIndex).ToString() }, { "include_docs", true.ToString() }});

            MembershipUserCollection userColl = new MembershipUserCollection();
            if (userView.Rows.Count() == 0)
            {
                totalRecords = userView.Rows.Count();
                return userColl;
            }

            foreach (var row in userView.Rows)
            {
                userColl.Add(UserToMembershipUser(row.Doc));
            }
            totalRecords = userView.Rows.Count();
            return userColl;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            var userView = _Client.GetView<string, User>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_ALL_USERS_FOR_APP,
                new NameValueCollection() { { "key", string.Format("{0}", ApplicationName) },
                {"limit", pageSize.ToString() }, {"skip", (pageSize * pageIndex).ToString() }});

            MembershipUserCollection userColl = new MembershipUserCollection();
            if (userView.Rows.Count() == 0)
            {
                totalRecords = userView.Rows.Count();
                return userColl;
            }

            foreach (var row in userView.Rows)
            {
                userColl.Add(UserToMembershipUser(row.Doc));
            }
            totalRecords = userView.Rows.Count();
            return userColl;
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return UserToMembershipUser(GetCouchUser(username, userIsOnline));
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }
        
        public override void UpdateUser(MembershipUser user)
        {
            var userView = _Client.GetView<User,CouchDocument>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
                new NameValueCollection() { { "key", string.Format(_twoKeyViewFormatString, user.UserName, ApplicationName) } });

            if(userView.Rows.Count() == 0)
                throw new ProviderException(string.Format("Cannot update the user {0}, they do not exist.", user.UserName));

            var dbUser = userView.Rows.FirstOrDefault().Value;

            dbUser.Username = user.UserName;
            dbUser.Email = user.Email;
            dbUser.DateCreated = user.CreationDate;
            dbUser.DateLastLogin = user.LastLoginDate;
            dbUser.IsOnline = user.IsOnline;
            dbUser.IsApproved = user.IsApproved;
            dbUser.IsLockedOut = user.IsLockedOut;            

            _Client.SaveDocument<User>(dbUser);

        }

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            var userView = _Client.GetView<User, CouchDocument>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
                new NameValueCollection() { { "key", string.Format(_twoKeyViewFormatString, username, ApplicationName) } });

            if(userView.Rows.Count() == 0)
                return false;

            User user = userView.Rows.FirstOrDefault().Value;

            if (user.PasswordHash == EncodePassword(password, user.PasswordSalt))
            {
                user.DateLastLogin = DateTime.Now;
                user.IsOnline = true;
                user.FailedPasswordAttempts = 0;
                user.FailedPasswordAnswerAttempts = 0;
                _Client.SaveDocument(user);
                return true;
            }
            else
            {
                user.LastFailedPasswordAttempt = DateTime.Now;
                user.FailedPasswordAttempts++;
                user.IsLockedOut = IsLockedOutValidationHelper(user);
                _Client.SaveDocument<User>(user);
            }            
            return false;
        }

        private bool IsLockedOutValidationHelper(User user)
        {
            long minutesSinceLastAttempt = DateTime.Now.Ticks - user.LastFailedPasswordAttempt.Ticks;
            if (user.FailedPasswordAttempts >= MaxInvalidPasswordAttempts
                && minutesSinceLastAttempt < (long)PasswordAttemptWindow)
                return true;
            return false;
        }

        private bool IsValidPassword(string password)
        {
            var minLength = MinRequiredPasswordLength;
            var minAlphaNumeric = MinRequiredNonAlphanumericCharacters;

            if (password.Length < minLength)
                return false;

            if (minAlphaNumeric > 0)
            {
                int alphaNumericChars = 0;
                foreach (var c in password)
                {
                    var alphanums = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
                    if (alphanums.Contains(c))
                        alphaNumericChars++;
                }
                var isValid = ((password.Length - alphaNumericChars) >= minAlphaNumeric);
                return isValid;
            }
            return true;
        }

        #endregion
        
        #region Couch Specific Helper Methods        

        private User GetCouchUser(string username, bool userIsOnline)
        {

            var userView = _Client.GetView<User, CouchDocument>(
                CouchViews.DESIGN_DOC_AUTH,
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
                new NameValueCollection() { { "key", string.Format(_twoKeyViewFormatString, username, ApplicationName) } });

            if (userView.Rows.Count() > 1)
                throw new ProviderException(string.Format("The user {0} has more than one record in the Membership database.", username));

            var user = userView.Rows.FirstOrDefault().Value;
            return user;            
        }

        private MembershipUser UserToMembershipUser(User user)
        {
            return new MembershipUser(ProviderName, user.Username, user.Username, user.Email, user.PasswordQuestion, user.Comment, user.IsApproved, user.IsLockedOut
                , user.DateCreated, user.DateLastLogin.HasValue ? user.DateLastLogin.Value : new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1));
        }


        private void SaveCouchUser(User user)
        {
            CouchDBClient.Instance.SaveDocument(user);            
        }

        #endregion

        #region Password Encyption


        /// <summary>
        /// Encode the password //Chris Pels
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private string EncodePassword(string password, string salt)
        {
            string encodedPassword = password;

            switch (_passwordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword =
                      Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    if (string.IsNullOrEmpty(salt))
                        throw new ProviderException("A random salt is required with hashed passwords.");
                    encodedPassword = PasswordUtil.HashPassword(password, salt, _hashAlgorithm, _validationKey);
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return encodedPassword;
        }

        /// <summary>
        /// UnEncode the password //Chris Pels
        /// </summary>
        /// <param name="encodedPassword"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private string UnEncodePassword(string encodedPassword, string salt)
        {
            string password = encodedPassword;

            switch (_passwordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password =
                      Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Hashed passwords do not require decoding, just compare hashes.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return password;
        }

        private string GetConfigValue(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            return value;
        }

        #endregion

    }
}
