using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.ComponentModel.DataAnnotations;

namespace RavenDBMembership.Web.Models
{
	public class EditUserModel
	{                
		public string Username { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
		public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

		public string[] Roles { get; set; }

		public string[] UserRoles { get; set; }

		public EditUserModel()
		{ }

		public EditUserModel(string username, string email, string[] roles, string[] userRoles)
		{
			this.Username = username;
			this.Email = email;                        
			this.Roles = roles;
			this.UserRoles = userRoles;
		}
	}
}
