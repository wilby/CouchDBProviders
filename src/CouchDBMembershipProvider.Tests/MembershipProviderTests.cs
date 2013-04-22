using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Wcjj.CouchClient;
using System.Web.Security;
using System.Collections.Specialized;
using System.Net;
using System.Configuration.Provider;


namespace CouchDBMembershipProvider.Tests
{
    [TestFixture]
    public class MembershipProviderTests : TestBase
    {
        
        private MembershipProvider _provider;
        private Client _Client;

        public MembershipProviderTests()
        {

        }

        [TestFixtureSetUp]
        public void SetUp() {

            _provider = new MembershipProvider();
            var config = GetMembershipConfigFake();
            _provider.Initialize("CouchDBMembershipProvider", config);
     
            _Client = CouchDBClient.Instance;

            if (!_Client.DatabaseExists())
                _Client.CreateDatabase();                        
        }

        [TestFixtureTearDown]
        public void TearDown() {

            if (_Client.DatabaseExists())
                _Client.DeleteDatabase();

            _Client = null;
            _provider = null;

        }

        //[Test]
        //public void CreateTestDB()
        //{
        //    var client = new Client("auth");
        //    if (!client.DatabaseExists())
        //        client.CreateDatabase();

        //    var fakes = CreateMultipleUserFakes(500);

        //    foreach (var fake in fakes)
        //    {
        //        client.SaveDocument<User>(fake);
        //    }

        //}

        [Test]
        public void Test_GetUser_by_username()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            var user = _provider.GetUser(fakeUser.Username, false);

            Assert.IsNotNull(fakeUser);
            Assert.AreEqual(user.UserName, fakeUser.Username);
            Assert.AreEqual(user.Email, fakeUser.Email);
        }

        [Test]
        public void Test_GetUser_by_provider_key()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            var user = _provider.GetUser((object)fakeUser.Id, true);

            Assert.IsNotNull(fakeUser);
            Assert.AreEqual(user.UserName, fakeUser.Username);
            Assert.AreEqual(user.Email, fakeUser.Email);
            Assert.IsTrue(user.IsOnline);

            var userView = _Client.GetView<User, User>(CouchViews.DESIGN_DOC_AUTH, CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
               new NameValueCollection() { { "key", string.Format("[\"{0}\",\"{1}\"]", user.UserName, "TestApp") } });
            //Check db was updated            
            Assert.IsTrue(userView.Rows[0].Value.LastActivityDate > DateTime.Now.Subtract(new TimeSpan(0, 1, 0)));
            
            var user2 = _provider.GetUser((object)"FAKE_KEY", false);
            Assert.IsNull(user2);
        }

        [Test]
        public void Test_CreateUser()
        {
            var fakeUser = CreateUserFake();
            MembershipCreateStatus status;
            var user = _provider.CreateUser(fakeUser.Username, Password, fakeUser.Email, fakeUser.PasswordQuestion,
                fakeUser.PasswordAnswer, fakeUser.IsApproved, fakeUser.Username, out status);

            var userView = _Client.GetView<User, CouchDocument>(
               CouchViews.DESIGN_DOC_AUTH,
               CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
               new NameValueCollection() { { "key", string.Format("[\"{0}\", \"{1}\"]", fakeUser.Username, _provider.ApplicationName) } });

            Assert.IsNotNull(user);
            Assert.IsNotNull(user.CreationDate);
            Assert.AreEqual(user.ProviderUserKey, fakeUser.Username);
            Assert.AreEqual(userView.Rows.FirstOrDefault().Value.Email, fakeUser.Email);
            Assert.AreEqual(MembershipCreateStatus.Success, status);

            MembershipCreateStatus badPassStatus;
            var badPassUser = _provider.CreateUser(fakeUser.Username + "2", "shrtpas", "2" + fakeUser.Email, fakeUser.PasswordQuestion,
                fakeUser.PasswordAnswer, fakeUser.IsApproved, fakeUser.Username, out badPassStatus);

            Assert.AreEqual(MembershipCreateStatus.InvalidPassword, badPassStatus);
        }

        [Test]
        public void Test_CreateUser_Fails_When_RequireUniqueEmail_AND_IS_DUPLICATE_EMAIL()
        {
            var fakeUser = CreateUserFake();
            MembershipCreateStatus status;
            var user = _provider.CreateUser(fakeUser.Username, Password, fakeUser.Email, fakeUser.PasswordQuestion,
                fakeUser.PasswordAnswer, fakeUser.IsApproved, fakeUser.Username, out status);

            MembershipCreateStatus status2;
            if (fakeUser == null)
                fakeUser = CreateUserFake();
            var newFakeUser = fakeUser.Clone<User>();
            newFakeUser.Username = "fakeuser" + UniqueId;
            var user2 = _provider.CreateUser(newFakeUser.Username, Password, newFakeUser.Email, newFakeUser.PasswordQuestion,
                newFakeUser.PasswordAnswer, newFakeUser.IsApproved, newFakeUser.Username, out status2);
            
            Assert.IsNotNull(user);
            Assert.IsNull(user2);
            Assert.AreEqual(status2, MembershipCreateStatus.DuplicateEmail);                        
        }

        [Test]
        public void Test_UpdateUser()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument(fakeUser);
            var toAppend = "Test_UpdateUser";            
            var newEmail = fakeUser.Email + toAppend;            
            var memUser = _provider.GetUser(fakeUser.Username, userIsOnline: false);            
            memUser.Email = newEmail;

            _provider.UpdateUser(memUser);
            var updatedUser = _Client.GetDocument<User>(fakeUser.Id);

            Assert.AreEqual(memUser.Email, updatedUser.Email);
        }

        [Test]
        [ExpectedException(typeof(WebException))]
        public void Test_DeleteUser()
        {

            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            _provider.DeleteUser(fakeUser.Username, deleteAllRelatedData: false);

            //Should throw a webexception with 404 not found
            _Client.GetDocument(fakeUser.Id);
        }

        [Test]
        public void Test_ValidateUser()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument(fakeUser);

            Assert.IsTrue(_provider.ValidateUser(fakeUser.Username, Password));
            Assert.IsFalse(_provider.ValidateUser(fakeUser.Username, "BadPass"));

            var maxAttempts = Convert.ToInt32(GetMembershipConfigFake()["maxInvalidPasswordAttempts"]);
            while (maxAttempts > 0)
            {
                _provider.ValidateUser(fakeUser.Username, "BadPass");
                maxAttempts--;
            }

            var lockedOutUser = _Client.GetDocument<User>(fakeUser.Id);
            Assert.IsTrue(lockedOutUser.IsLockedOut);
            Assert.IsTrue(lockedOutUser.FailedPasswordAttempts >= Convert.ToInt32(GetMembershipConfigFake()["maxInvalidPasswordAttempts"]));
            Assert.IsTrue(lockedOutUser.LastFailedPasswordAttempt > DateTime.Now.AddMinutes(-1));
        }

        [Test]
        public void Test_FindUsersByEmail()
        {
            var fakeUsers = CreateMultipleUserFakes(10);
            var email = "wilby@wcjj.net";

            foreach (var user in fakeUsers)
            {
                user.Email = email;
                _Client.SaveDocument<User>(user);
            }

            int totalRecords = 0;
            var memUsers = _provider.FindUsersByEmail(email, 0, 5, out totalRecords);
            Assert.AreEqual(5, memUsers.Count);
            Assert.AreEqual(5, totalRecords);

            memUsers = _provider.FindUsersByEmail(email, 1, 5, out totalRecords);
            Assert.AreEqual(5, memUsers.Count);
            Assert.AreEqual(5, totalRecords);

            memUsers = _provider.FindUsersByEmail(email, 2, 5, out totalRecords);
            Assert.AreEqual(0, memUsers.Count);
            Assert.AreEqual(0, totalRecords);
        }

        [Test]
        public void Test_FindUsersByUsername()
        {
            //Get rid of all the other users before running this test or it will fail
            //due to all users starting with the name wilby
            _Client.DeleteDatabase();
            SetUp();

            var fakeUsers = CreateMultipleUserFakes(10);
            var email = "wilby@wcjj.net";
            var username = "wilby";

            foreach (var user in fakeUsers)
            {
                user.Email = email;
                _Client.SaveDocument<User>(user);
            }

            int totalRecords = 0;
            var memUsers = _provider.FindUsersByName(username, 0, 5, out totalRecords);
            Assert.AreEqual(5, memUsers.Count);
            Assert.AreEqual(5, totalRecords);

            memUsers = _provider.FindUsersByName(username, 1, 5, out totalRecords);
            Assert.AreEqual(5, memUsers.Count);
            Assert.AreEqual(5, totalRecords);

            memUsers = _provider.FindUsersByName(username, 2, 5, out totalRecords);
            Assert.AreEqual(0, memUsers.Count);
            Assert.AreEqual(0, totalRecords);
        }

        [Test]
        public void Test_GetNumberOfUsersOnline()
        {
            var fakeUser = CreateUserFake();
            fakeUser.DateLastLogin = DateTime.Now.Subtract(new TimeSpan(0, 2, 0));
            _Client.SaveDocument<User>(fakeUser);

            var totalUserOnline = _provider.GetNumberOfUsersOnline();

            Assert.IsTrue(totalUserOnline >= 1);
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void Test_GetPassword_throws_exception_when_enable_pasword_retrieval_not_set()
        {
            var provider = new MembershipProvider();
            var config = GetMembershipConfigFake();
            config["enablePasswordRetrieval"] = "false";
            provider.Initialize("CouchDBMembershipProvider", config);

            var fakeUser = CreateUserFake();            
            _Client.SaveDocument<User>(fakeUser);

            provider.GetPassword(fakeUser.Username, fakeUser.PasswordAnswer);
        }

        [Test]
        [ExpectedException(typeof(MembershipPasswordException))]
        public void Test_GetPassword_throws_exception_when_password_answer_is_wrong()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            _provider.GetPassword(fakeUser.Username, "WRONG_ANSWER");
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void Test_GetPassword_throws_exception_when_password_is_hashed()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            _provider.GetPassword(fakeUser.Username, fakeUser.PasswordAnswer);
        }

        [Test]
        public void Test_GetUserNameByEmail()
        {
            var fake = CreateUserFake();            
            _Client.SaveDocument<User>(fake);

            var username = _provider.GetUserNameByEmail(fake.Email);

            Assert.AreEqual(fake.Username, username);

            username = _provider.GetUserNameByEmail("FAKE_EMAIL@FAKEMAIL.COM");

            Assert.AreEqual("", username);

        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void Test_ResetPassword_throws_exception_when_EnablePasswordReset_is_false()
        {

            var provider = new MembershipProvider();
            var config = GetMembershipConfigFake();
            config["enablePasswordReset"] = "false";
            provider.Initialize("CouchDBMembershipProvider", config);

            provider.ResetPassword("", "");
        }

        [Test]
        [ExpectedException(typeof(MembershipPasswordException))]
        public void Test_ResetPassword_throws_exception_when_password_answer_is_wrong()
        {
            var fake = CreateUserFake();            
            _Client.SaveDocument<User>(fake);
            

            _provider.ResetPassword(fake.Username, "");
        }

        [Test]        
        public void Test_ResetPassword()
        {
            var fake = CreateUserFake();
            _Client.SaveDocument<User>(fake);


            var pass = _provider.ResetPassword(fake.Username, fake.PasswordAnswer);

             var userView = _Client.GetView<User, User>(CouchViews.DESIGN_DOC_AUTH, CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
              new NameValueCollection() { { "key", string.Format("[\"{0}\",\"{1}\"]", fake.Username, "TestApp") } });
            var user = userView.Rows[0].Value;
            Assert.AreNotEqual(fake.PasswordHash, PasswordUtil.HashPassword(pass, user.PasswordSalt, "SHA1", null));
            Assert.AreNotEqual(fake.PasswordSalt, user.PasswordSalt);
            Assert.IsTrue(user.LastPasswordChangedDate > DateTime.Now.Subtract(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public void Test_UnlockUser()
        {
            var fake = CreateUserFake();
            fake.IsLockedOut = true;

            _Client.SaveDocument<User>(fake);
            
            var unlocked = _provider.UnlockUser(fake.Username);

            var userView = _Client.GetView<User, User>(CouchViews.DESIGN_DOC_AUTH, CouchViews.AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME,
              new NameValueCollection() { { "key", string.Format("[\"{0}\",\"{1}\"]", fake.Username, "TestApp") } });
            var user = userView.Rows[0].Value;

            Assert.IsTrue(unlocked);
            Assert.IsFalse(user.IsLockedOut);
            Assert.IsTrue(user.LastLockedOutDate > DateTime.Now.Subtract(new TimeSpan(0, 1, 0)));
        }

        [Test]
        public void Test_ChangePassword()
        {
            var fake = CreateUserFake();
            _Client.SaveDocument<User>(fake);

            var badPass = "BadPass";
            var newPass = "!@3Password";
            
            var changed = _provider.ChangePassword(fake.Username, Password, newPass);
            Assert.IsTrue(changed);

            changed = _provider.ChangePassword(fake.Username, Password, badPass);
            Assert.IsFalse(changed);

            changed = _provider.ChangePassword("badUser", Password, newPass);
            Assert.IsFalse(changed);
        }
    }
}
