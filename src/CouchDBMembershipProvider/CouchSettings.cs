using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Security;

namespace CouchDBMembershipProvider
{
    /// <summary>
    /// Easy Access Settings for the CouchDB Membership connection string values.
    /// </summary>
    public static class CouchSettings
    {
        public static string Host { get; set; }
        public static int Port { get; set; }
        public static string Database { get; set; }
        public static string UserName { get; set; }
        public static SecureString Password { get; set; }

        public static void InitializeSettings(string connectionString)
        {
            DbConnectionStringBuilder csBuilder = new DbConnectionStringBuilder(useOdbcRules: true);            
            csBuilder.ConnectionString = connectionString.ToLower();

            
            object host = null;
            bool hasHost = csBuilder.TryGetValue("host", out host);
            if(!hasHost)
                throw new ConnectionStringException("The 'Host' portion of the conneciton string is required and does not currently exist.");
            Host = (string)host;

            object port = null;
            bool hasPort = csBuilder.TryGetValue("port", out port);
            if (hasPort)
            {
                Port = Convert.ToInt32(port);
            }
            else
            {
                Port = 5984;
            }

            object db = null;
            bool hasdb = csBuilder.TryGetValue("database", out db);
            if (!hasdb)
                throw new ConnectionStringException("The 'Database' portion of the conneciton string is required and does not currently exist.");
            Database = ((string)db).ToLower();

            object user = null;
            bool hasUser = csBuilder.TryGetValue("username", out user);
            if (hasUser)            
                UserName = (string)user;
            

            object pass = null;
            bool hasPass = csBuilder.TryGetValue("password", out pass);
            if (hasPass) {
                Password = new SecureString();
                foreach(var c in ((string)pass).ToCharArray()) {
                    Password.AppendChar(c);
                }
                pass = null;
                Password.MakeReadOnly();
            }
        }
    }
}
