using CouchDBProviders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wcjj.CouchClient;


namespace CouchDBProviders.Tests
{
    public class RoleProviderTests : TestBase
    {
        private MembershipProvider _provider;
        private Client _Client;

        public RoleProviderTests()
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
    }
}
