using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wcjj.CouchClient;

namespace CouchDBProviders
{
    [Serializable]
    public class User : CouchDocument
    {
       
        public string ApplicationName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastLogin { get; set; }
        public IList<string> Roles { get; set; }

        #region Extended User

        public string PasswordQuestion { get; set; }
        public string PasswordAnswer { get; set; }
        public bool IsLockedOut { get; set; }        
        public int FailedPasswordAttempts { get; set; }
        public int FailedPasswordAnswerAttempts { get; set; }
        public DateTime LastFailedPasswordAttempt { get; set; }
        public string Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime LastActivityDate { get; set; }
        public DateTime LastPasswordChangedDate { get; set; }
        public DateTime LastLockedOutDate { get; set; }

        #endregion

        public User() : base()
        {
            Roles = new List<string>();
        }
    }

}
