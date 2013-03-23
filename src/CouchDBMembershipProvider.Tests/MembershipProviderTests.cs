using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using DreamSeat;
using System.Web.Security;
using DreamSeat.Support;


namespace CouchDBMembershipProvider.Tests
{
    [TestFixture]
    public class MembershipProviderTests : TestBase
    {
        private CouchDatabase _db;
        private MembershipProvider _provider;

        public MembershipProviderTests()
        {

        }

        [TestFixtureSetUp]
        public void SetUp() {
            CouchDBClient.MembershipSettings = GetMembershipConfigFake();
            var client = CouchDBClient.Instance;            
            
            if (client.HasDatabase(CouchSettings.Database))
                client.DeleteDatabase(CouchSettings.Database);

            client.CreateDatabase(CouchSettings.Database);
                        
            _db = client.GetDatabase(CouchSettings.Database);

            _provider = new MembershipProvider();
            _provider.Initialize("CouchDBMembershipProvider", GetMembershipConfigFake());
        }

        [TestFixtureTearDown]
        public void TearDown() {                        
            _db = null;
            _provider = null;
        }

        [Test]
        public void Test_GetUser()
        {
            var fakeUser = CreateUserFake();
            _db.CreateDocument<User>(fakeUser);

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

            ViewOptions vo = new ViewOptions();
            vo.Key = new KeyOptions(new string[] { fakeUser.Username });
            var dbUser = _db.GetView<string, User>("auth", "byUserName", vo);

            Assert.IsNotNull(user);
            Assert.IsNotNull(user.CreationDate);
            Assert.AreEqual(user.ProviderUserKey, fakeUser.Username);
            Assert.AreEqual(dbUser.Rows.FirstOrDefault().Value.Email, fakeUser.Email);
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
            _db.CreateDocument<User>(fakeUser);
            var toAppend = "Test_UpdateUser";            
            var newEmail = fakeUser.Email + toAppend;            
            var memUser = _provider.GetUser(fakeUser.Username, userIsOnline: false);            
            memUser.Email = newEmail;

            _provider.UpdateUser(memUser);
            var updatedUser = _db.GetDocument<User>(fakeUser.Id);

            Assert.AreEqual(memUser.Email, updatedUser.Email);
        }

        [Test]
        public void Test_DeleteUser()
        {
            var fakeUser = CreateUserFake();
            _db.CreateDocument<User>(fakeUser);

            _provider.DeleteUser(fakeUser.Username, deleteAllRelatedData: false);

            Assert.IsFalse(_db.DocumentExists(fakeUser.Id));
        }

        [Test]
        public void Test_ValidateUser()
        {
            //var fakeUser = CreateUserFake();
            //_db.CreateDocument<User>(fakeUser);

            //Assert.IsTrue(_provider.ValidateUser(fakeUser.Username, Password));
            //Assert.IsFalse(_provider.ValidateUser(fakeUser.Username, "BadPass"));

            //var maxAttempts = Convert.ToInt32(GetMembershipConfigFake()["maxInvalidPasswordAttempts"]);
            //while (maxAttempts > 0)
            //    _provider.ValidateUser(fakeUser.Username, "BadPass");
            
            //var lockedOutUser = _db.GetDocument<User>(fakeUser.Id);
            //Assert.IsTrue(lockedOutUser.IsLockedOut);
            //Assert.IsTrue(lockedOutUser.FailedPasswordAnswerAttempts >= Convert.ToInt32(GetMembershipConfigFake()["maxInvalidPasswordAttempts"]));
            //Assert.IsTrue(lockedOutUser.LastFailedPasswordAttempt > DateTime.Now.AddMinutes(-1));
        }

        [Test]
        public void Test_FindUsersByEmail()
        {
            var fakeUsers = CreateMultipleUserFakes(10);
            var email = "wilby@wcjj.net";

            foreach (var user in fakeUsers)
            {
                user.Email = email;
                _db.CreateDocument<User>(user);
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

    }
}
