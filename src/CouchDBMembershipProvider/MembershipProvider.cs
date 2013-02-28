﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using DreamSeat;
using System.Configuration.Provider;
using System.Web.Configuration;
using System.Collections.Specialized;
using DreamSeat.Support;
using System.Configuration;

namespace CouchDBMembershipProvider
{
    public class MembershipProvider : System.Web.Security.MembershipProvider
    {

        #region Private Members
        
        private CouchDatabase _DB;       

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

        #endregion

        #region Overriden Public Members

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
            _minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredAlphaNumericCharacters"], "1"));
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
            string conString = ConfigurationManager.ConnectionStrings[
                config["connectionStringName"]].ConnectionString;
            if (string.IsNullOrEmpty(conString))
                throw new ProviderException(string.Format(
                    "The connection string name in membership settings is wrong or not set or the connection string {0} does not exist.",
                    config["connectionStringName"]));

            CouchDBClient.MembershipSettings = config;
            CouchSettings.InitializeSettings(conString);

            _DB = CouchDBClient.Instance.GetDatabase(CouchSettings.Database);

            CouchViews cv = new CouchViews();            
            cv.CreateViews();
            
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
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            if (args.Cancel)
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
                ViewResult<string[], User> existingUser = _DB.GetView<string[], User>(CouchViews.AUTH_VIEW_ID, 
                CouchViews.AUTH_VIEW_NAME_BY_Email_AND_APP_NAME, 
                CouchViews.ViewOptionsForDualKeyViewSelectSingle(email, ApplicationName));                        

                if (existingUser.TotalRows > 0)
                {
                    status = MembershipCreateStatus.DuplicateEmail;
                    return null;
                }
            }

            _DB.CreateDocument<User>(user);
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
            var userView = _DB.GetView<string[], User>(CouchViews.AUTH_VIEW_ID, 
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME, 
                CouchViews.ViewOptionsForDualKeyViewSelectSingle(username, ApplicationName));

            if (userView.TotalRows == 0)            
                return false;
            
            _DB.DeleteDocument(userView.Rows.FirstOrDefault().Value);
            return true;
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
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

            ViewOptions vo = new ViewOptions() { 
                Key = new KeyOptions(new string[] { user.UserName, ApplicationName }) 
            };            
            var userView = _DB.GetView<string[], User>(CouchViews.AUTH_VIEW_ID, 
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,vo);

            if(userView.TotalRows == 0)
                throw new ProviderException(string.Format("Cannot update the user {0}, they do not exist.", user.UserName));

            var dbUser = userView.Rows.FirstOrDefault().Value;

            dbUser.Username = user.UserName;
            dbUser.Email = user.Email;
            dbUser.DateCreated = user.CreationDate;
            dbUser.DateLastLogin = user.LastLoginDate;
            dbUser.IsOnline = user.IsOnline;
            dbUser.IsApproved = user.IsApproved;
            dbUser.IsLockedOut = user.IsLockedOut;            

            _DB.UpdateDocument<User>(dbUser);

        }

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            var userView = _DB.GetView<string[], User>(CouchViews.AUTH_VIEW_ID,
                CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
                CouchViews.ViewOptionsForDualKeyViewSelectSingle(username, ApplicationName));

            if(userView.TotalRows == 0)
                return false;

            var user = userView.Rows.FirstOrDefault().Value;
            if (user.PasswordHash == EncodePassword(password, user.PasswordSalt))
            {
                user.DateLastLogin = DateTime.Now;
                user.IsOnline = true;
                user.FailedPasswordAttempts = 0;
                user.FailedPasswordAnswerAttempts = 0;
                _DB.UpdateDocument(user);
                return true;
            }
            else
            {
                user.LastFailedPasswordAttempt = DateTime.Now;
                user.FailedPasswordAttempts++;
                user.IsLockedOut = IsLockedOutValidationHelper(user);
                _DB.UpdateDocument<User>(user);
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

        #endregion
        
        #region Couch Specific Helper Methods        

        private User GetCouchUser(string username, bool userIsOnline)
        {
            var db = CouchDBClient.Instance.GetDatabase(CouchSettings.Database);
            DreamSeat.CouchView cv = new CouchView();
            var viewOptions = new ViewOptions();
            viewOptions.Key = new KeyOptions(new string[] { username });

            var result = db.GetView<string, User>("auth", "byUserName");

            if (result.TotalRows > 1)
                throw new ProviderException(string.Format("The user {0} has more than one record in the Membership database.", username));

            var user = result.Rows.FirstOrDefault().Value;
            return user;            
        }

        private MembershipUser UserToMembershipUser(User user)
        {
            return new MembershipUser(ProviderName, user.Username, user.Username, user.Email, user.PasswordQuestion, user.Comment, user.IsApproved, user.IsLockedOut
                , user.DateCreated, user.DateLastLogin.HasValue ? user.DateLastLogin.Value : new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1));
        }


        private void SaveCouchUser(User user)
        {
            CouchDBClient.Instance.GetDatabase(CouchSettings.Database).CreateDocument<User>(user);            
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