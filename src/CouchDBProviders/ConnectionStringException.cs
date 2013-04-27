using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CouchDBProviders
{
    /// <summary>
    ///  Class for showing errors in the connection string.
    /// </summary>
    public class ConnectionStringException : Exception
    {
        public ConnectionStringException() : base("There is an error in the connection string.") 
        {}

        public ConnectionStringException(string message) : base(message) { }

        public ConnectionStringException(string message, Exception innerException) : base(message, innerException) { }
      
    }
}
