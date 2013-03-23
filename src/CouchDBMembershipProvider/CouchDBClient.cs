using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamSeat;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Configuration;

namespace CouchDBMembershipProvider
{
    public sealed class CouchDBClient
    {
        private static volatile CouchClient _couchClient;
        private static object syncRoot = new Object();

        private CouchDBClient() { }

        public static NameValueCollection MembershipSettings { get; set; }

        public static CouchClient Instance {
            get
            {   
                if (_couchClient == null)
                {
                    lock (syncRoot)
                    {
                        if (_couchClient == null)
                        {
                            if (MembershipSettings == null)
                                throw new CouchMembershipSettingsException("The membership settings have not been set for CouchDBClient");

                            string connectionString = ConfigurationManager.ConnectionStrings[MembershipSettings["connectionStringName"]].ConnectionString;
                            CouchSettings.InitializeSettings(connectionString);

                            IntPtr ssPassPtr = Marshal.SecureStringToBSTR(CouchSettings.Password);
                            try
                            {   
                                _couchClient = new CouchClient(CouchSettings.Host, CouchSettings.Port,
                               CouchSettings.UserName, Marshal.PtrToStringBSTR(ssPassPtr));
                            }
                            finally
                            {
                                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ssPassPtr);
                            }
                        }
                    }
                }
                return _couchClient;
            }
        }
    }
}
