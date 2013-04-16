using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Wcjj.CouchClient;
using System.Web.Security;
using System.Collections.Specialized;
using System.Net;


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

        [Test]
        public void Test_GetUser()
        {
            var fakeUser = CreateUserFake();
            _Client.SaveDocument<User>(fakeUser);

            var user = _provider.GetUser(fakeUser.Username, false);

            Assert.IsNotNull(fakeUser);
            Assert.AreEqual(user.UserName, fakeUser.Username);
            Assert.AreEqual(user.Email, fakeUser.Email);
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

    }
}
