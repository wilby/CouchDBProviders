using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CouchDBProviders
{
    public class CouchMembershipSettingsException : Exception
    {
        public CouchMembershipSettingsException() : base("There was an error in CouchDBProviders Settings.") 
        {}

        public CouchMembershipSettingsException(string message) : base(message) { }

        public CouchMembershipSettingsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
