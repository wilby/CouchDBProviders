using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamSeat;

namespace CouchDBMembershipProvider
{
    public class Role : CouchDocument
    {
        
        public string ApplicationName { get; set; }
        public string Name { get; set; }
        public string ParentRoleId { get; set; }

        public string Type { get; set; }

        public Role()
        {
            Type = this.GetType().ToString();
        }

        public Role(string name, Role parentRole) : this()
        {
            this.Name = name;
            if (parentRole != null)
            {
                this.ParentRoleId = parentRole.Id;
            }
        }
    }

}
