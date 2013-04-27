﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Transactions;


namespace RavenDBMembership.Web.Models
{

	#region Models

	public class ChangePasswordModel
	{
		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Current password")]
		public string OldPassword { get; set; }

		[Required]
		[ValidatePasswordLength]
		[DataType(DataType.Password)]
		[Display(Name = "New password")]
		public string NewPassword { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm new password")]
		[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

    public class ResetPasswordQuesitonAndAnswerModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "New Security Question")]
        public string PasswordQuestion { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Security Question, Answer")]
        public string PasswordQuestionAnswer { get; set; }

        
    }

    public class ChangePasswordQuestionAndAnswerModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [ValidatePasswordLength]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "New Security Question")]
        public string PasswordQuestion { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Security Question, Answer")]
        public string PasswordQuestionAnswer { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("PasswordQuestionAnswer")]
        [Display(Name = "Confirm Answer")]
        public string ConfirmAnswer { get; set; }
               

        public ChangePasswordQuestionAndAnswerModel(string username, string passwordQuestion)
        {
            this.UserName = username;
            this.PasswordQuestion = passwordQuestion;
        }
    }

    public class ResetPasswordModel {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }
    }

	public class LogOnModel
	{
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }
	}
    
	public class RegisterModel
	{
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

        [Required]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required]
        [DisplayName("Last Name")]
        public string LastName { get; set; }

		[Required]
		[DataType(DataType.EmailAddress)]
		[Display(Name = "Email address")]
		public string Email { get; set; }

		[Required]
		[ValidatePasswordLength]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

        [Required(ErrorMessage="You must confirm the password.")]
		[DataType(DataType.Password)]
		[Display(Name = "Confirm password")]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

        [Required]
        [DisplayName("Security Question")]
        public string PasswordQuestion { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Security Question, Answer")]
        public string PasswordQuestionAnswer { get; set; }

        [Required(ErrorMessage="You must confirm the question's answer.")]
        [DataType(DataType.Password)]
        [DisplayName("Answer Confirmation")]
        [Compare("PasswordQuestionAnswer", ErrorMessage = "The question answer and confirmation answer do not match.")]
        public string ConfirmPasswordQuestionAnswer { get; set; }
	}
    

	#endregion

	#region Services
	// The FormsAuthentication type is sealed and contains static members, so it is difficult to
	// unit test code that calls its members. The interface and helper class below demonstrate
	// how to create an abstract wrapper around such a type in order to make the AccountController
	// code unit testable.

	public interface IMembershipService
	{
		int MinPasswordLength { get; }

		bool ValidateUser(string userName, string password);

		MembershipCreateStatus CreateUser(string userName, string password, string email);

        MembershipCreateStatus CreateUser(string userName, string password, string email, string question, string answer);

		bool ChangePassword(string userName, string oldPassword, string newPassword);

        bool ChangePasswordQuestionAndAnswer(string username, string password,
            string newPasswordQuestion, string newPasswordAnswer);

        string ResetPassword(string username, string passwordAnswer);        

		MembershipUserCollection GetAllUsers();

		MembershipUser GetUser(string username);

		string[] GetAllRoles();

		string[] GetRolesForUser(string username);

		void AddRole(string roleName);

		void UpdateUser(MembershipUser user, string[] roles);

		void DeleteRole(string roleName);
	}

	public class AccountMembershipService : IMembershipService
	{
		private readonly CouchDBMembershipProvider.MembershipProvider _provider;
		private readonly RoleProvider _roleProvider;

		public AccountMembershipService() : this(null, null)
		{
		}

		public AccountMembershipService(MembershipProvider provider, RoleProvider roleProvider)
		{
            _provider = (CouchDBMembershipProvider.MembershipProvider)(provider ?? Membership.Provider);
			//_roleProvider = roleProvider ?? Roles.Provider;
		}

		public int MinPasswordLength
		{
			get
			{
				return _provider.MinRequiredPasswordLength;
			}
		}

		public bool ValidateUser(string userName, string password)
		{
			if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");
			if (String.IsNullOrEmpty(password)) throw new ArgumentException("Value cannot be null or empty.", "password");

			return _provider.ValidateUser(userName, password);
		}

		public MembershipCreateStatus CreateUser(string userName, string password, string email)
		{
			if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");
			if (String.IsNullOrEmpty(password)) throw new ArgumentException("Value cannot be null or empty.", "password");
			if (String.IsNullOrEmpty(email)) throw new ArgumentException("Value cannot be null or empty.", "email");

			MembershipCreateStatus status;
			_provider.CreateUser(userName, password, email, null, null, true, null, out status);
			return status;
		}

        public MembershipCreateStatus CreateUser(string userName, string password, string email, string question, string answer) 
        {
            if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");
            if (String.IsNullOrEmpty(password)) throw new ArgumentException("Value cannot be null or empty.", "password");
            if (String.IsNullOrEmpty(email)) throw new ArgumentException("Value cannot be null or empty.", "email");

            MembershipCreateStatus status;
            
            _provider.CreateUser(userName, password, email, question, answer, true, null, out status);
            
            return status;
        }

		public bool ChangePassword(string userName, string oldPassword, string newPassword)
		{
			if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");
			if (String.IsNullOrEmpty(oldPassword)) throw new ArgumentException("Value cannot be null or empty.", "oldPassword");
			if (String.IsNullOrEmpty(newPassword)) throw new ArgumentException("Value cannot be null or empty.", "newPassword");

			// The underlying ChangePassword() will throw an exception rather
			// than return false in certain failure scenarios.
			try
			{
				MembershipUser currentUser = _provider.GetUser(userName, true /* userIsOnline */);
				return currentUser.ChangePassword(oldPassword, newPassword);
			}
			catch (ArgumentException)
			{
				return false;
			}
			catch (MembershipPasswordException)
			{
				return false;
			}
		}

        public string ResetPassword(string username, string passwordAnswer)
        {
            return _provider.ResetPassword(username, passwordAnswer);
        }

        public bool ChangePasswordQuestionAndAnswer(string username, string password,
            string newPasswordQuestion, string newPasswordAnswer)
        {
            return _provider.ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer);
        }

		public MembershipUserCollection GetAllUsers()
		{
			int totalRecords;
			return _provider.GetAllUsers(0, 1000, out totalRecords);
		}

		public MembershipUser GetUser(string username)
		{
			return _provider.GetUser(username, false);
		}

		public string[] GetAllRoles()
		{
			return _roleProvider.GetAllRoles();
		}

		public string[] GetRolesForUser(string username)
		{
			return _roleProvider.GetRolesForUser(username);
		}

		public void AddRole(string roleName)
		{
			_roleProvider.CreateRole(roleName);
		}

		public void UpdateUser(MembershipUser user, string[] roles)
		{
			using (var ts = new TransactionScope())
			{
				_provider.UpdateUser(user);
				var existingRoles = _roleProvider.GetRolesForUser(user.UserName);
				if (roles != null && roles.Length > 0)
				{
					var rolesToBeAdded = roles.Except(existingRoles).ToArray();
                    _roleProvider.AddUsersToRoles(new[] { user.UserName }, rolesToBeAdded);
				}
				if (existingRoles.Length > 0)
				{
					var rolesToBeDeleted = (roles != null ? existingRoles.Except(roles) : existingRoles).ToArray();
                    _roleProvider.RemoveUsersFromRoles(new[] { user.UserName }, rolesToBeDeleted);
				}

				ts.Complete();
			}
		}

		public void DeleteRole(string roleName)
		{
			using (var ts = new TransactionScope())
			{
				// Delete role
				_roleProvider.DeleteRole(roleName, false);

				ts.Complete();
			}
		}
	}

	public interface IFormsAuthenticationService
	{
		void SignIn(string userName, bool createPersistentCookie);
		void SignOut();
	}

	public class FormsAuthenticationService : IFormsAuthenticationService
	{
		public void SignIn(string userName, bool createPersistentCookie)
		{
			if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");

			FormsAuthentication.SetAuthCookie(userName, createPersistentCookie);
		}

		public void SignOut()
		{
			FormsAuthentication.SignOut();
		}
	}
	#endregion

	#region Validation
	public static class AccountValidation
	{
		public static string ErrorCodeToString(MembershipCreateStatus createStatus)
		{
			// See http://go.microsoft.com/fwlink/?LinkID=177550 for
			// a full list of status codes.
			switch (createStatus)
			{
				case MembershipCreateStatus.DuplicateUserName:
					return "Username already exists. Please enter a different user name.";

				case MembershipCreateStatus.DuplicateEmail:
					return "A username for that e-mail address already exists. Please enter a different e-mail address.";

				case MembershipCreateStatus.InvalidPassword:
					return "The password provided is invalid. Please enter a valid password value.";

				case MembershipCreateStatus.InvalidEmail:
					return "The e-mail address provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidAnswer:
					return "The password retrieval answer provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidQuestion:
					return "The password retrieval question provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidUserName:
					return "The user name provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.ProviderError:
					return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				case MembershipCreateStatus.UserRejected:
					return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				default:
					return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
			}
		}
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public sealed class CompareAttribute : ValidationAttribute, IClientValidatable
	{
		private const string _defaultErrorMessage = "'{0}' and '{1}' do not match.";
		private readonly object _typeId = new object();

		public CompareAttribute(string confirmProperty)
			: base(_defaultErrorMessage)
		{
			ConfirmProperty = confirmProperty;
		}

		public string ConfirmProperty { get; private set; }

		public override object TypeId
		{
			get
			{
				return _typeId;
			}
		}

		public override string FormatErrorMessage(string name)
		{
			return String.Format(CultureInfo.CurrentCulture, ErrorMessageString,
				name, ConfirmProperty);
		}

		protected override ValidationResult IsValid(object value, ValidationContext context)
		{
			var confirmValue = context.ObjectType.GetProperty(ConfirmProperty).GetValue(context.ObjectInstance, null);
			if (!Equals(value, confirmValue))
			{
				return new ValidationResult(FormatErrorMessage(context.DisplayName));
			}
			return null;
		}

		public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
		{
			return new[]{
                new ModelClientValidationEqualToRule(FormatErrorMessage(metadata.GetDisplayName()), ConfirmProperty)
            };
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class ValidatePasswordLengthAttribute : ValidationAttribute, IClientValidatable
	{
		private const string _defaultErrorMessage = "'{0}' must be at least {1} characters long.";
		private readonly int _minCharacters = Membership.Provider.MinRequiredPasswordLength;

		public ValidatePasswordLengthAttribute()
			: base(_defaultErrorMessage)
		{
		}

		public override string FormatErrorMessage(string name)
		{
			return String.Format(CultureInfo.CurrentCulture, ErrorMessageString,
				name, _minCharacters);
		}

		public override bool IsValid(object value)
		{
			string valueAsString = value as string;
			return (valueAsString != null && valueAsString.Length >= _minCharacters);
		}

		public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
		{
			return new[]{
                new ModelClientValidationStringLengthRule(FormatErrorMessage(metadata.GetDisplayName()), _minCharacters, int.MaxValue)
            };
		}
	}
	#endregion

}
