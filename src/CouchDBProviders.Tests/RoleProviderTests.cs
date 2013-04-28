using CouchDBProviders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Net;
using System.Text;
using Wcjj.CouchClient;


namespace CouchDBProviders.Tests
{
    public class RoleProviderTests : TestBase
    {
        private RoleProvider _provider;
        private Client _Client;

        public RoleProviderTests()
        {

        }

        [TestFixtureSetUp]
        public void SetUp() {

            _provider = new RoleProvider();
            var config = GetRoleConfigFake();

            _provider.Initialize("CouchDBRoleProvider", config);
     
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
        [ExpectedException(typeof(ProviderException))]
        public void Test_AddUsersToRoles_throws_provider_exception_when_user_or_role_does_not_exists_for_applicationName()
        {
            _provider.AddUsersToRoles(new string[1] { "NON_EXISTENT_USER" }, new string[] { "ROLE1" });
        }

        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_AddUsersToRoles_throws_argument_exception_when_user_or_role_is_empty_string()
        {
            _provider.AddUsersToRoles(new string[1] { "" }, new string[] { "ROLE1" });
            _provider.AddUsersToRoles(new string[1] { "FAKE_USER" }, new string[] { "" });
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_AddUsersToRoles_throws_argument_null_exception_when_user_or_role_is_null()
        {
            _provider.AddUsersToRoles(new string[1] { null }, new string[] { "ROLE1" });
        }

        [Test]        
        public void Test_AddUsersToRoles()
        {
            var user = CreateUserFake();
            _Client.SaveDocument<User>(user);

            var role = new Role("ROLE" + UniqueId, null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);
            
            _provider.AddUsersToRoles(new string[1] { user.Username }, new string[] { role.Name });

            var savedUser = _Client.GetDocument<User>(user.Id);
            Assert.IsTrue(savedUser.Roles.Contains(role.Name));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_CreateRole_throws_argument_exception_when_role_name_has_comma_or_is_empty()
        {
            _provider.CreateRole("My,Role");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_CreateRole_throws_argument_null_exception_when_roleName_is_null()
        {
            _provider.CreateRole(null);
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void Test_CreateRole_throws_provider_exception_when_roleName_already_exists()
        {
            var role = new Role("MYROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);
            
            _provider.CreateRole(role.Name);
        }

        [Test]
        public void Test_CreateRole()
        {
            var roleName = "CREATE_ROLE_TEST";
            _provider.CreateRole(roleName);

            var roleView = _Client.GetView<Role, CouchDocument>(
                     CouchViews.DESIGN_DOC_AUTH,
                     CouchViews.ROLEVIEW_BY_ROLE_NAME_AND_APPNAME,
                     new NameValueCollection() { { "key", string.Format("[\"{0}\",\"{1}\"]", roleName, "TestApp") } });

            Assert.IsTrue(roleView.HasRows);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_DeleteRole_throws_argument__exception_when_roleName_is_empty()
        {
            _provider.DeleteRole("", true);
            _provider.DeleteRole("NON_EXISTENT_ROLE", true);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_DeleteRole_throws_argument_null_exception_when_roleName_is_null()
        {
            _provider.DeleteRole(null, true);
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void Test_DeleteRole_throws_provider_exception_when_role_is_in_use_when_throwOnPopulated()
        {
            var role = new Role("MY_DELETE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            var user = CreateUserFake();
            user.Roles.Add(role.Name);
            _Client.SaveDocument<User>(user);

            _provider.DeleteRole(role.Name, true);
        }

        [Test]
        [ExpectedException(typeof(WebException))]
        public void Test_DeleteRole()
        {
            var role = new Role("DELETE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            _provider.DeleteRole(role.Name, true);

            var doc = _Client.GetDocument<Role>(role.Id);            
        }

        [Test]
        public void Test_FindUserInRole()
        {
            var role = new Role("FIND_USER_IN_ROLE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            var user = CreateUserFake();
            user.Roles.Add(role.Name);
            _Client.SaveDocument<User>(user);

            var user2 = CreateUserFake();
            user2.Roles.Add(role.Name);
            _Client.SaveDocument<User>(user2);

            var user3 = CreateUserFake();
            user3.Username = "FIND_USER_USERNAME";
            user3.Roles.Add(role.Name);
            _Client.SaveDocument<User>(user3);

            var usernames = _provider.FindUsersInRole(role.Name, null);

            Assert.IsTrue(usernames.Contains(user.Username));
            Assert.IsTrue(usernames.Contains(user2.Username));
            Assert.AreEqual(3, usernames.Length);

            usernames = _provider.FindUsersInRole(role.Name, "wilby");

            Assert.IsTrue(usernames.Contains(user.Username));
            Assert.IsTrue(usernames.Contains(user2.Username));
            Assert.AreEqual(2, usernames.Length);
            Assert.IsFalse(usernames.Contains(user3.Username));
        }

        [Test]
        public void Test_GetAllRoles()
        {
            var role = new Role("GET_ALL_ROLES_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            var role2 = new Role("GET_ALL_ROLES_ROLE2", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role2);

            var roles = _provider.GetAllRoles();

            Assert.IsTrue(roles.Length >= 2);
            Assert.IsTrue(roles.Contains(role.Name) && roles.Contains(role2.Name));

        }

        [Test]
        public void Test_GetRolesForUser()
        {
            var role = new Role("FIND_USER_IN_ROLE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);
            var role2 = new Role("FIND_USER_IN_ROLE_ROLE2", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role2);
                        
            var user = CreateUserFake();
            user.Roles.Add(role.Name);
            user.Roles.Add(role2.Name);
            _Client.SaveDocument<User>(user);

            var roles = _provider.GetRolesForUser(user.Username);
            Assert.AreEqual(2, roles.Length);
            Assert.IsTrue(roles.Contains(role.Name) && roles.Contains(role2.Name));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_GetUserInRole_throws_argument_exception_when_roleName_is_empty() {
            _provider.GetUsersInRole("");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_GetUserInRole_throws_argument_null_exception_when_roleName_is_null()
        {
            _provider.GetUsersInRole(null);
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void Test_GetUserInRole_throws_provider_exception_when_role_does_not_exist()
        {
            _provider.GetUsersInRole("NON_EXISTENT_ROLE");
        }

        [Test]
        public void Test_GetUsersInRole()
        {
            var role = new Role("GET_USERS_IN_ROLE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);            

            var user = CreateUserFake();
            user.Roles.Add(role.Name);            
            _Client.SaveDocument<User>(user);

            var user2 = CreateUserFake();
            user2.Roles.Add(role.Name);            
            _Client.SaveDocument<User>(user2);

            var users = _provider.GetUsersInRole(role.Name);
            Assert.AreEqual(2, users.Length);
            Assert.IsTrue(users.Contains(user.Username) && users.Contains(user2.Username));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_IsUserInRole_throws_argument_exception_when_role_or_user_is_empty()
        {
            _provider.IsUserInRole("", "");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_IsUserInRole_throws_argument_null_exception_when_role_or_user_is_null()
        {
            _provider.IsUserInRole(null, null);
        }

        [Test]
        public void Test_IsUserInRole()
        {
            var role = new Role("IS_USER_IN_ROLE_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            var user = CreateUserFake();
            user.Roles.Add(role.Name);
            _Client.SaveDocument<User>(user);

            var isInRole = _provider.IsUserInRole(user.Username, role.Name);
            Assert.IsTrue(isInRole);

            var user2 = CreateUserFake();            
            _Client.SaveDocument<User>(user2);

            isInRole = _provider.IsUserInRole(user2.Username, role.Name);
            Assert.IsFalse(isInRole);
        }

        [Test]
        public void Test_RemoveUsersFromRoles()
        {
            var role = new Role("REMOVE_USERS_FROM_ROLES_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);
            var role2 = new Role("REMOVE_USERS_FROM_ROLES_ROLE2", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role2);

            var user = CreateUserFake();
            user.Roles.Add(role.Name);
            user.Roles.Add(role2.Name);
            _Client.SaveDocument<User>(user);

            var user2 = CreateUserFake();
            user2.Roles.Add(role.Name);
            user2.Roles.Add(role2.Name);
            _Client.SaveDocument<User>(user2);

            _provider.RemoveUsersFromRoles(new string[2] { user.Username, user2.Username }, new string[2] { role.Name, role2.Name });
            var saveduser1 = _Client.GetDocument<User>(user.Id);
            var saveduser2 = _Client.GetDocument<User>(user2.Id);

            Assert.IsFalse(saveduser1.Roles.Contains(role.Name));
            Assert.IsFalse(saveduser2.Roles.Contains(role2.Name));
        }

        [Test]
        public void Test_RoleExists()
        {
            var result = _provider.RoleExists("NON_EXISTENT_ROLE");
            Assert.IsFalse(result);

            var role = new Role("ROLE_EXISTS_ROLE", null) { ApplicationName = "TestApp" };
            _Client.SaveDocument<Role>(role);

            Assert.IsTrue(_provider.RoleExists(role.Name));
        }
    }
}
