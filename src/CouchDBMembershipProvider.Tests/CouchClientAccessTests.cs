using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CouchDBMembershipProvider;
using DreamSeat;

namespace CouchDBMembershipProvider.Tests
{
    [TestFixture]
    public class CouchClientAccessTests : TestBase
    {
        public class Person : CouchDocument
        {
            public string Name { get; set; }
            public string Hobby { get; set; }
        }

        [SetUp]
        public void SetUp() { 
        
        }

        [TearDown]
        public void TearDown() { 
        
        }

        [Test]
        public void TEST_Connect_Using_CouchDBClient_Static_Class_Singleton()
        {
            CouchDBClient.MembershipSettings = GetMembershipConfigFake();
            CouchClient client = CouchDBClient.Instance;

            Assert.IsFalse(client.HasDatabase(CouchSettings.Database));
            client.CreateDatabase(CouchSettings.Database);
            Assert.IsTrue(client.HasDatabase(CouchSettings.Database));
            client.DeleteDatabase(CouchSettings.Database);
        }

        [Test]
        public void TEST_Connect_Using_CouchDBClient_CouchDatabase()
        {
            try
            {
                CouchDBClient.MembershipSettings = GetMembershipConfigFake();
                var db = CouchDBClient.Instance.GetDatabase(CouchSettings.Database);

                var model = new Person() { Name = "Wilby", Hobby = "Programming!" };
                db.CreateDocument<Person>(model);

                var savedPerson = db.GetDocument<Person>(model.Id);

                Assert.AreEqual(savedPerson.Name, "Wilby");
                Assert.AreEqual(savedPerson.Hobby, "Programming!");
            }
            finally
            {
                CouchDBClient.Instance.DeleteDatabase(CouchSettings.Database);
            }
        }

        //[Test]        
        //[ExpectedException(typeof(CouchMembershipSettingsException))]
        //public void Test_CouchMembershipSettingsException_Thrown_When_MembershipSettings_Is_Not_Set()
        //{
        //    CouchDBClient.MembershipSettings = null;            
        //    var db = CouchDBClient.Instance.GetDatabase(CouchSettings.Database);
        //}
    }
}
