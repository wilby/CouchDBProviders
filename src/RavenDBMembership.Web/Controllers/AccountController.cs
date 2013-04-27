using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using RavenDBMembership.Web.Models;
using System.Net.Mail;

namespace RavenDBMembership.Web.Controllers
{
	public class AccountController : Controller
	{

		public IFormsAuthenticationService FormsService { get; set; }
		public IMembershipService MembershipService { get; set; }

		protected override void Initialize(RequestContext requestContext)
		{
			if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
			if (MembershipService == null) { MembershipService = new AccountMembershipService(); }

			base.Initialize(requestContext);
		}

		// **************************************
		// URL: /Account/LogOn
		// **************************************

		public ActionResult LogOn()
		{
			return View();
		}

		[HttpPost]
		public ActionResult LogOn(LogOnModel model, string returnUrl)
		{
			if (ModelState.IsValid)
			{
				if (MembershipService.ValidateUser(model.UserName, model.Password))
				{
					FormsService.SignIn(model.UserName, model.RememberMe);
					if (Url.IsLocalUrl(returnUrl))
					{
						return Redirect(returnUrl);
					}
					else
					{
						return RedirectToAction("Index", "Home");
					}
				}
				else
				{
					ModelState.AddModelError("", "The user name or password provided is incorrect.");
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		// **************************************
		// URL: /Account/LogOff
		// **************************************

		public ActionResult LogOff()
		{
			FormsService.SignOut();

			return RedirectToAction("Index", "Home");
		}

		// **************************************
		// URL: /Account/Register
		// **************************************

		public ActionResult Register()
		{
			ViewBag.PasswordLength = MembershipService.MinPasswordLength;
			return View();
		}

		[HttpPost]
		public ActionResult Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				// Attempt to register the user
				MembershipCreateStatus createStatus = MembershipService.CreateUser(model.UserName, model.Password, model.Email,
                   model.PasswordQuestion, model.PasswordQuestionAnswer);
                
				if (createStatus == MembershipCreateStatus.Success)
				{
					FormsService.SignIn(model.UserName, false /* createPersistentCookie */);
					return RedirectToAction("Index", "Home");
				}
				else
				{
					ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
				}
			}

			// If we got this far, something failed, redisplay form
			ViewBag.PasswordLength = MembershipService.MinPasswordLength;
			return View(model);
		}

		// **************************************
		// URL: /Account/ChangePassword
		// **************************************

		[Authorize]
		public ActionResult ChangePassword()
		{
			ViewBag.PasswordLength = MembershipService.MinPasswordLength;
			return View();
		}

		[Authorize]
		[HttpPost]
		public ActionResult ChangePassword(ChangePasswordModel model)
		{
			if (ModelState.IsValid)
			{
				if (MembershipService.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword))
				{
					return RedirectToAction("ChangePasswordSuccess");
				}
				else
				{
					ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
				}
			}

			// If we got this far, something failed, redisplay form
			ViewBag.PasswordLength = MembershipService.MinPasswordLength;
			return View(model);
		}

        [Authorize]
        public ActionResult ChangePasswordQuestionAndAnswer()
        {
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            var user = MembershipService.GetUser(User.Identity.Name);
            var model = new ChangePasswordQuestionAndAnswerModel(user.UserName, user.PasswordQuestion);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePasswordQuestionAndAnswer(ChangePasswordQuestionAndAnswerModel model)
        {

            if (ModelState.IsValid)
            {
                if (MembershipService.ChangePasswordQuestionAndAnswer(User.Identity.Name, model.Password, 
                    model.PasswordQuestion, model.PasswordQuestionAnswer))
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The password is incorrect or the new question and answer are invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View(model);
        }

		// **************************************
		// URL: /Account/ChangePasswordSuccess
		// **************************************

		public ActionResult ChangePasswordSuccess()
		{
			return View();
		}

		public ActionResult ManageUsers()
		{
			var users = MembershipService.GetAllUsers();
			return View(users);
		}

		public ActionResult ManageRoles()
		{
			var roles = MembershipService.GetAllRoles();
			return View(roles);
		}

		[HttpPost]
		public ActionResult ManageRoles(string roleName)
		{
			if (String.IsNullOrEmpty(roleName))
			{
				ModelState.AddModelError("roleName", "Name is required");
				return ManageRoles();
			}
			else
			{
				MembershipService.AddRole(roleName);
				return RedirectToAction("ManageRoles");
			}

		}

		public ActionResult EditUser(string username)
		{
			var user = MembershipService.GetUser(username);
			var roles = MembershipService.GetAllRoles();
			var userRoles = MembershipService.GetRolesForUser(user.UserName);           
           
            return View(new EditUserModel(user.UserName, user.Email, roles, userRoles));
		}

		[HttpPost]
		public ActionResult EditUser(EditUserModel model)
		{
			var user = MembershipService.GetUser(model.Username);
            user.Email = model.Email;            
			MembershipService.UpdateUser(user, model.UserRoles);            
			return RedirectToAction("ManageUsers");
		}

		[HttpPost]
		public ActionResult DeleteRole(string roleName)
		{
			MembershipService.DeleteRole(roleName);
			return RedirectToAction("ManageRoles");
		}

        //enablePasswordReset must be set to true in the web.config for this action
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = Membership.GetUser(model.UserName);
                if (user != null)
                {
                    return RedirectToAction("ResetPasswordQuestionAndAnswer", new { username=model.UserName});
                }
                else
                {
                    ModelState.AddModelError("UserName", "Bad username.");
                }
            }

            return View(model);
        }

        public ActionResult ResetPasswordQuestionAndAnswer(string username)
        {
            var user = Membership.GetUser(username);

            return View(new ResetPasswordQuesitonAndAnswerModel() { PasswordQuestion = user.PasswordQuestion, UserName=username });
        }

        [HttpPost]
        public ActionResult ResetPasswordQuestionAndAnswer(ResetPasswordQuesitonAndAnswerModel model)
        {
            try {
                string newPass = MembershipService.ResetPassword(model.UserName, model.PasswordQuestionAnswer);
                var user = Membership.GetUser(model.UserName);
                var toAddress = user.Email;
                var mm = new MailMessage("myapp@myap.com", toAddress);
                                
                mm.Subject = "MyApp, Your new password";
                mm.Body = string.Format("Your new password is {0}.", newPass);
                mm.IsBodyHtml = false;

                var smtp = new SmtpClient();
                smtp.Host = "localhost";
                smtp.Port = 25;
                smtp.Send(mm);
                
                return RedirectToAction("ResetPasswordSuccess");
            }
            catch(MembershipPasswordException) {
                ModelState.AddModelError("PasswordQuestionAnswer", "The answer is incorrect");
                return View(model);
            }
            
        }

        public ActionResult ResetPasswordSuccess()
        {
            return View();
        }
	}
}
