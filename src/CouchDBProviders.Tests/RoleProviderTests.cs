using CouchDBProviders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
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

    }
}
