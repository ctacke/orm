using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace OpenNETCF.ORM.Oracle.Integration.Test
{
    [TestClass()]
    public class OracleDynamicEntityTest
    {
        public TestContext TestContext { get; set; }

        private OracleConnectionInfo GetInfo()
        {
            return new OracleConnectionInfo("192.168.10.246", 1521, "XE", "system", "password");
        }

        private OracleDataStore GetTestStore()
        {
            var store = new OracleDataStore(GetInfo());

            store.EnsureCompatibility();

            return store;
        }

        private const string ODACRoot = "ODAC";
        private const string DLL_NAME = "kernel32.dll";

        private class LoadFile
        {
            public LoadFile(string name, bool isNative)
            {
                Name = name;
                IsNative = isNative;
            }

            public string Name;
            public bool IsNative;
        }

        private LoadFile[] m_files = new LoadFile[]
        {
            new LoadFile("oci.dll", true),
            new LoadFile("orannzsbb11.dll", true),
            new LoadFile("oraocci11.dll", true),
            new LoadFile("oraociei11.dll", true),
            new LoadFile("OraOps11w.dll", true),
//            new LoadFile("Oracle.DataAccess.dll", false)
        };

        [DllImport(DLL_NAME)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private void MapRequiredReferences()
        {
            var sourcepath = Path.Combine(TestContext.TestDir, @"..\..\OpenNETCF.ORM.Oracle\ODAC\x86");

            foreach (var file in m_files)
            {
                var src = Path.Combine(sourcepath, file.Name);

                if (!File.Exists(src))
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    throw new FileNotFoundException("Unable to locate Oracle DAC file: " + file);
                }

                if (file.IsNative)
                {
                    LoadLibrary(src);
                }
            }
        }

        [TestInitialize]
        public void TestSetup()
        {
            MapRequiredReferences();
            //var src = Path.Combine(TestContext.TestDir, @"..\..\OpenNETCF.ORM.Oracle\ODAC\x86");
            //foreach(var file in Directory.GetFiles(src))
            //{
            //    var dst = Path.Combine(TestContext.DeploymentDirectory, Path.GetFileName(file));
            //    File.Copy(file,  dst, true);

            //    LoadLibrary(dst);
            //}
        }

        [TestMethod()]
        public void BasicDynamicCRUDTest()
        {

            var fieldList = new List<FieldAttribute>();
            fieldList.Add(new FieldAttribute()
            {
                FieldName = "ID",
                IsPrimaryKey = true,
                DataType = System.Data.DbType.Int32
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = "FirstName",
                DataType = System.Data.DbType.String
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = "LastName",
                DataType = System.Data.DbType.String,
                AllowsNulls = false
            });

            var definition = new DynamicEntityDefinition("People", fieldList, KeyScheme.None);

            var store = GetTestStore();

            var exists = store.TableExists(definition.EntityName);
            if (exists)
            {
                store.DropTable(definition.EntityName);
            }

            store.RegisterDynamicEntity(definition);

            Assert.IsTrue(store.TableExists(definition.EntityName));

            var entity = new DynamicEntity("People");
            entity.Fields["id"] = 1;
            entity.Fields["FirstName"] = "John";
            entity.Fields["LastName"] = "Doe";
            store.Insert(entity);

            entity = new DynamicEntity("People");
            entity.Fields["id"] = 2;
            entity.Fields["FirstName"] = "Jim";
            entity.Fields["LastName"] = "Smith";
            store.Insert(entity);

            entity = new DynamicEntity("People");
            entity.Fields["id"] = 3;
            entity.Fields["FirstName"] = "Sam";
            entity.Fields["LastName"] = "Adams";
            store.Insert(entity);

            var items = store.Select("People");
            DumpData(items);

            store.Delete("People", items.First().Fields["ID"]);

            items = store.Select("People");
            DumpData(items);

            store.Delete("People", "LastName", "Smith");

            items = store.Select("People");
            DumpData(items);

            var person = items.First();
            person.Fields["FirstName"] = "Joe";
            person.Fields["LastName"] = "Satriani";
            store.Update(person);

            items = store.Select("People");
            DumpData(items);

            // now let's change the structure and see what happens
            fieldList.Add(new FieldAttribute()
            {
                FieldName = "Middle_Name",
                DataType = System.Data.DbType.Double,
                AllowsNulls = true // this has to be true to add a column
            });

            var newDefinition = new DynamicEntityDefinition("People", fieldList, KeyScheme.Identity);
            store.RegisterDynamicEntity(newDefinition, true);

            items = store.Select("People");
            DumpData(items);
        }

        void DumpData(IEnumerable<DynamicEntity> items)
        {
            var first = true;

            foreach (var p in items)
            {
                if (first)
                {
                    Debug.WriteLine(p.EntityName);
                    Debug.WriteLine(new string('=', p.EntityName.Length));

                    foreach (var f in p.Fields)
                    {
                        Debug.Write(string.Format("| {0} ", f.Name));
                    }

                    Debug.WriteLine("|");

                    first = false;
                }

                foreach (var f in p.Fields)
                {
                    Debug.Write(string.Format("| {0} ", f.Value));
                }
                Debug.WriteLine("|");
            }
        }
    }
}
