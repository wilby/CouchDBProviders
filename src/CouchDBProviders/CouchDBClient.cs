using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Configuration;
using Wcjj.CouchClient;

namespace CouchDBProviders
{
    public sealed class CouchDBClient
    {
        private static volatile Client _couchClient;
        private static object syncRoot = new Object();
        public static string ConnectionStringName { get; set; }
        public static string ProxyConnectionStringName { get; set; }

        private CouchDBClient() { }
        
        public static Client Instance {
            get
            {   
                if (_couchClient == null)
                {
                    lock (syncRoot)
                    {
                        if (_couchClient == null)
                        {
                            if (string.IsNullOrEmpty(ConnectionStringName))
                                throw new CouchMembershipSettingsException("connectionStringName must be set for the CouchDBProviders");

                            if (!string.IsNullOrEmpty(ProxyConnectionStringName))
                            {   
                                _couchClient = new Client(ConnectionStringName, ProxyConnectionStringName);
                            }
                            else
                            {
                                _couchClient = new Client(ConnectionStringName);
                            }                            
                        }
                    }
                }
                if (!_couchClient.DatabaseExists())
                    _couchClient.CreateDatabase();
                return _couchClient;
            }
        }
    }
}
