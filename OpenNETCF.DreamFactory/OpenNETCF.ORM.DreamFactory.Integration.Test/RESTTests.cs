using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using OpenNETCF.DreamFactory;
using System.Diagnostics;

namespace OpenNETCF.ORM.DreamFactory.Integration.Test
{
    [TestClass]
    public class RESTTests
    {
        private bool CertCallbackHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private Session GetSession()
        {
            return new Session(
                "https://dsp-opennetcf.cloud.dreamfactory.com/",
                "ORM",
                TestCreds.UID,
                TestCreds.PWD);
        }

        [TestMethod]
        public void GetTableSchemaTest()
        {
            var s = GetSession();
            s.Initialize();

            foreach (var table in s.Data.GetTables())
            {
                Debug.WriteLine(string.Format("Table: {0}", table.Name));

                foreach (var field in table.Fields)
                {
                    Debug.WriteLine(string.Format("  Field: {0} [{1}]", field.Name, field.TypeName));
                }
            }

        }

        [TestMethod]
        public void GetTableBadNameTest()
        {
            var s = GetSession();
            s.Initialize();

            var t = s.Data.GetTable("Foo");
        }

        [TestMethod]
        public void TableCreationTest()
        {
            var session = GetSession();
            session.Initialize();

            var fields = new List<Field>();
            fields.Add(new Field<int>("ID") { IsPrimaryKey = true });
            fields.Add(new Field<int>("FieldA"));
            fields.Add(new Field<byte>("FieldB"));
            fields.Add(new Field<short>("FieldC"));
            fields.Add(new Field<string>("FieldD"));
            fields.Add(new Field<bool>("FieldE"));
            fields.Add(new Field<float>("FieldF"));
            fields.Add(new Field<double>("FieldG"));
            fields.Add(new Field<decimal>("FieldH"));
            fields.Add(new Field<DateTime>("FieldI"));
            fields.Add(new Field<TimeSpan>("FieldJ"));
            fields.Add(new Field<byte[]>("FieldK"));

            // create the table
            var newTable = session.Data.CreateTable("TestTable", "Integration Test", fields);

            // query the table list
            var tableList = session.Data.GetTables();

            // make sure it's there
            Assert.IsTrue(tableList.Any(t => t.Name == "TestTable"));

            // delete it
            session.Data.DeleteTable("TestTable");

            // query the table list
            tableList = session.Data.GetTables();

            // make sure it's not there
            Assert.IsFalse(tableList.Any(t => t.Name == "TestTable"));
        }

        [TestMethod]
        public void RecordRequestTest()
        {
            var session = GetSession();
            session.Initialize();

            var rows = session.Data.GetTable("MyTestTable").GetRecords();

        }

        [TestMethod]
        public void RecordInsertTest()
        {
            var session = GetSession();
            session.Initialize();

            var row = new Dictionary<string, object>();
//            row.Add("FieldA", 42);
            row.Add("FieldB", 1);
            row.Add("FieldC", 2);
            row.Add("FieldD", "Foo");

            var table = session.Data.GetTable("MyTestTable");

            var pk = table.InsertRecord(row);

            var count = table.GetRecordCount();

            var before = table.GetRecords(pk);

            var update = new Dictionary<string, object>();
            update.Add("FieldA", pk);
            update.Add("FieldD", "Bar");

            table.UpdateRecord(update);

            var after = table.GetRecords(pk);

            table.DeleteRecords(pk);

            var after2 = table.GetRecords(pk);
        }

        [TestMethod]
        public void ApplicationsTest()
        {
            var session = GetSession();
            session.Initialize();

            var apps = session.Applications.GetContainers();

            var name = apps[0].Name;

            var c = session.Applications.GetContainer(name);


            var c2 = session.Applications.CreateContainer("NewApp");
            session.Applications.DeleteContainer(c2.Name);
            var c3 = session.Applications.GetContainer("NewApp");
        }

        [TestMethod]
        public void RecordDeleteTest()
        {
        }
    }
}

