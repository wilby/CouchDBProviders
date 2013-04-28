using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Security;
using Wcjj.CouchClient;

namespace CouchDBProviders
{
    public class RoleProvider : System.Web.Security.RoleProvider
    {
        private string _twoKeyViewFormatString = "[\"{0}\",\"{1}\"]";
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
                        new NameValueCollection() { { "key", string.Format(_twoKeyViewFormatString, roleName, ApplicationName) } });

            if (roleView.HasRows)
                throw new ProviderException(string.Format("The role: {0} for application: {1} already exists.", roleName, ApplicationName));

            var role = new Role(roleName, null) { ApplicationName = ApplicationName };
            _client.SaveDocument<Role>(role);
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}
