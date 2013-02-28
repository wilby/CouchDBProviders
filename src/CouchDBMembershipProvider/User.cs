using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamSeat;

namespace CouchDBMembershipProvider
{
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
        public bool IsOnline { get; set; }
        public int FailedPasswordAttempts { get; set; }
        public int FailedPasswordAnswerAttempts { get; set; }
        public DateTime LastFailedPasswordAttempt { get; set; }
        public string Comment { get; set; }
        public bool IsApproved { get; set; }

        #endregion

        public string Type { get; set; }

        public User()
        {
            Roles = new List<string>();
            Type = this.GetType().ToString();
        }
    }

}
