using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web.Configuration;

namespace CouchDBMembershipProvider.Tests
{
    public abstract class TestBase
    {
        private static int _uniqueId = 0;
        private static object syncRoot = new object();

        public static int UniqueId { get {
                lock (syncRoot)
                {
                    _uniqueId++;
                    return _uniqueId;
                }        
            } 
        }

        protected string Password = "1234ABCD@!";

        protected static NameValueCollection GetMembershipConfigFake()
        {

            NameValueCollection config = new NameValueCollection();
            config.Add("applicationName", "TestApp");
            config.Add("connectionStringName", "Server");
            config.Add("proxyConnectionStringName", "");
            config.Add("enablePasswordReset", "true");
            config.Add("enablePasswordRetrieval", "true");
            config.Add("maxInvalidPasswordAttempts", "5");
            config.Add("minRequiredNonAlphanumericCharacters", "2");
            config.Add("minRequiredPasswordLength", "8");
            config.Add("requiresQuestionAndAnswer", "true");
            config.Add("requiresUniqueEmail", "true");
            config.Add("passwordAttemptWindow", "10");
            config.Add("passwordFormat", "Hashed");            
            config.Add("enableEmbeddableDocumentStore", "true");
            
            return config;
        }

        protected User CreateUserFake()
        {            
            var salt = PasswordUtil.CreateRandomSalt();
            var config = GetMembershipConfigFake();
            return new User()
            {
                Username = string.Format("wilby{0}", UniqueId),
                PasswordHash = PasswordUtil.HashPassword(Password, salt, "SHA1", null),
                PasswordSalt = salt,
                Email = string.Format("wilby{0}@wcjj.net", UniqueId),
                PasswordQuestion = "A QUESTION",
                PasswordAnswer = "A ANSWER",                
                IsApproved = true,
                Comment = "A FAKE USER",
                ApplicationName = config["applicationName"],
                DateCreated = DateTime.Now,
                DateLastLogin = DateTime.Now,
                FailedPasswordAttempts = 0,
                FullName = string.Format("Wilby Jackson {0}", UniqueId),
                IsLockedOut = false
            };
        }

        protected IEnumerable<User> CreateMultipleUserFakes(int nbrToCreate = 5)
        {
            while (nbrToCreate > 0)
            {
                yield return CreateUserFake();
                nbrToCreate--;
            }
        }
    }
}
