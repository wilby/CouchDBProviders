using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Security;
using Wcjj.CouchClient;

namespace CouchDBProviders
{
    public class RoleProvider : System.Web.Security.RoleProvider
    {
        private string _twoStringKeyViewFormatString = "[\"{0}\",\"{1}\"]";
        private string _threeStringKeyViewFormatString = "[\"{0}\",\"{1}\",\"{2}\"]";
        private Client _client;

        public override string ApplicationName { get; set; }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            var conStringName = config["connectionStringName"];
            if (string.IsNullOrEmpty(conStringName))
                throw new ProviderException("The connection string name must be set for the role provider.");

            var proxyConStringName = config["proxyConnectionStringName"];

            CouchDBClient.ConnectionStringName = conStringName;
            CouchDBClient.ProxyConnectionStringName = proxyConStringName;
            _client = CouchDBClient.Instance;

            CouchViews cv = new CouchViews();
            cv.CreateViews(_client);

            ApplicationName = config["applicationName"] ?? System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;

            base.Initialize(name, config);
        }

        
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (usernames.Any(x => x == "") || roleNames.Any(x => x == ""))
                throw new ArgumentException("Arguments usernames and roleNames can not be empty.");

            if (usernames.Any(x => x == null) || roleNames.Any(x => x == null))
                throw new ArgumentNullException("Arguments usernames and roleNames can not be null.");

            try
            {
                var userKeys = "";
                foreach (var user in usernames)
                {
                    userKeys += string.Format("[\"{0}\",\"{1}\"],", user, ApplicationName);
                }
                userKeys = string.Format("[{0}]", userKeys.Substring(0, userKeys.Length - 1));

                var userView = _client.GetView<User, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.MEMVIEW_BY_USERNAME_AND_APPNAME,
                        new NameValueCollection() { { "keys", userKeys } });

                if (userView.Rows.Count < usernames.Count())
                    throw new ProviderException("All users in the usernames argument must exist.");

                //Verify that all roles exist by attempting to retrieve them from the db
                //A WebException with 404 is thrown if they do not
                var roleKeys = "";
                foreach (var role in roleNames)
                {
                    roleKeys += string.Format("[\"{0}\",\"{1}\"],", role, ApplicationName);
                }
                roleKeys = string.Format("[{0}]", roleKeys.Substring(0, roleKeys.Length - 1));

                var roleView = _client.GetView<Role, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                        new NameValueCollection() { { "keys", roleKeys } });

                if (roleView.Rows.Count < roleNames.Count())
                    throw new ProviderException("All roles in the roleNames argument must exist.");

                foreach (var user in userView.Rows)
                {
                    var dbUser = user.Value;
                    foreach (var role in roleNames)
                    {                       
                        if(!dbUser.Roles.Contains(role)) {
                            dbUser.Roles.Add(role);
                        }
                    }
                    _client.SaveDocument<User>(dbUser);
                }

            }
            catch (WebException wex)
            {
                if (wex.Message.Contains("404"))
                    throw new ProviderException("A username or rolename provided could not be found.", wex);
                throw wex;
            }

        }

        public override void CreateRole(string roleName)
        {
            if (roleName == "" || roleName.Contains(','))
                throw new ArgumentException("roleName cannot be empty or contain commas");

            if (roleName == null)
                throw new ArgumentNullException("roleName");

            var roleView = _client.GetView<Role, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });

            if (roleView.HasRows)
                throw new ProviderException(string.Format("The role: {0} for application: {1} already exists.", roleName, ApplicationName));

            var role = new Role(roleName, null) { ApplicationName = ApplicationName };
            _client.SaveDocument<Role>(role);
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {            
            if (roleName == null)
                throw new ArgumentNullException("roleName");

            var roleView = _client.GetView<Role, CouchDocument>(
                      CouchViews.DESIGN_DOC_AUTH,
                      CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                      new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });

            if (roleName == "" || !roleView.HasRows)
                throw new ArgumentNullException("roleName");

            var userView = _client.GetView<User, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_USERS_WITH_ROLES,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName)  } });

            if (throwOnPopulatedRole && userView.HasRows)
                throw new ProviderException(string.Format("Cannot delete, users with role: {0} still exist.", roleName));

            try
            {
                foreach (var user in userView.Rows)
                {
                    var dbUser = user.Value;
                    dbUser.Roles.Remove(roleName);

                    _client.SaveDocument<User>(dbUser);
                }

                _client.DeleteDocument<Role>(roleView.Rows[0].Value);
                return true;
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Returns usernames that start with the value of usernameToMatch and are in the role roleName. 
        /// If a asterisk *, empty string or null value is passed in to usernameToMatch then all usesr with the roleName are returned.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="usernameToMatch"></param>
        /// <returns></returns>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            try
            {
                CouchViewResult<string, CouchDocument> view;
                if (usernameToMatch == "*" || string.IsNullOrEmpty(usernameToMatch))
                {
                    view = _client.GetView<string, CouchDocument>(
                           CouchViews.DESIGN_DOC_AUTH,
                           CouchViews.ROLEVIEW_BY_USERNAMES_WITH_ROLE,
                           new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });
                }

                view = _client.GetView<string, CouchDocument>(
                           CouchViews.DESIGN_DOC_AUTH,
                           CouchViews.ROLEVIEW_BY_USER_ROLES,
                           new NameValueCollection() { { "startkey", string.Format(_threeStringKeyViewFormatString, roleName, usernameToMatch ,ApplicationName) },
                           { "endkey", string.Format(_threeStringKeyViewFormatString, roleName, usernameToMatch + "z" ,ApplicationName) }});

                if (view.HasRows)
                {
                    var result = new List<string>();
                    foreach (var row in view.Rows)
                    {
                        result.Add(row.Value);
                    }

                    return result.Distinct().OrderBy(x => x).ToArray();
                }
                return new string[0];

            }
            catch (WebException wex)
            {
                if (wex.Message.Contains("404"))
                    throw new ProviderException("Role does not exist.", wex);
                throw wex;
            }
        }

        public override string[] GetAllRoles()
        {
            var view = _client.GetView<string, CouchDocument>(
                          CouchViews.DESIGN_DOC_AUTH,
                          CouchViews.ROLEVIEW_ROLES_BY_APPNAME,
                          new NameValueCollection() { { "key", string.Format("\"{0}\"", ApplicationName) } });

            if(!view.HasRows)
                return new string[0];

            return view.Rows.Select(x => x.Value).OrderBy(y => y).ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            if (username == null)
                throw new ArgumentNullException("username");

            if (username == "")
                throw new ArgumentException("username");
           
            var userView = _client.GetView<User, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.MEMVIEW_BY_USERNAME_AND_APPNAME,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, username, ApplicationName) } });

            if(!userView.HasRows)
                return new string[0];

            return userView.Rows[0].Value.Roles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            if (roleName == null)
                throw new ArgumentNullException("roleName");

            if (roleName == "")
                throw new ArgumentException("roleName");
            
            var roleView = _client.GetView<Role, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });

            if (!roleView.HasRows)
                throw new ProviderException(string.Format("The role: {0} does not exist for application {1}.", roleView, ApplicationName));
            

            var usernamesVIew = _client.GetView<string, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_USERNAMES_WITH_ROLE,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });

            if (!usernamesVIew.HasRows)
                return new string[0];

            return usernamesVIew.Rows.Select(x => x.Value).OrderBy(y => y).ToArray();
        }

        
        /// <summary>
        /// Given a username and roleName this method determines if the user is in the role.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        ///<remarks>
        /// Not throwing a provider exception when user or role does not exists, that would require a total of 3 round trips
        /// before returning the requested data.
        /// </remarks>
        public override bool IsUserInRole(string username, string roleName)
        {
            if (username == null || roleName == null)
                throw new ArgumentNullException();
             
            if (username == "" || roleName == "")
                throw new ArgumentException();

            var usernamesVIew = _client.GetView<string, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_USER_ROLES,
                        new NameValueCollection() { { "key", string.Format(_threeStringKeyViewFormatString, roleName, username, ApplicationName) } });

            if (usernamesVIew.HasExactlyOneRow)
                return true;
            return false;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if(usernames.Any(x => x == null) || roleNames.Any(x => x == null))
                throw new ArgumentNullException();

            if (usernames.Any(x => x == "") || roleNames.Any(x => x == ""))
                throw new ArgumentException();

            var userKeys = "";
            foreach (var user in usernames)
            {
                userKeys += string.Format("[\"{0}\",\"{1}\"],", user, ApplicationName);
            }
            userKeys = string.Format("[{0}]", userKeys.Substring(0, userKeys.Length - 1));

            var userView = _client.GetView<User, CouchDocument>(
                    CouchViews.DESIGN_DOC_AUTH,
                    CouchViews.MEMVIEW_BY_USERNAME_AND_APPNAME,
                    new NameValueCollection() { { "keys", userKeys } });

            foreach (var row in userView.Rows)
            {
                var user = row.Value;
                foreach(var role in roleNames) {
                    user.Roles.Remove(role);
                }
                _client.SaveDocument<User>(user);
            }
        }

        public override bool RoleExists(string roleName)
        {
            if (roleName == null)
                throw new ArgumentNullException();

            if (roleName == "")
                throw new ArgumentException();


            var roleView = _client.GetView<Role, CouchDocument>(
                        CouchViews.DESIGN_DOC_AUTH,
                        CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                        new NameValueCollection() { { "key", string.Format(_twoStringKeyViewFormatString, roleName, ApplicationName) } });

            return roleView.HasRows;
        }
    }
}
