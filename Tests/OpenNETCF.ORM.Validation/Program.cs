using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenNETCF.ORM.Validation
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().RunTests();
        }

        List<DataStoreValidator> m_validators = new List<DataStoreValidator>();

        public Program()
        {
            m_validators.Add(new SQLCEValidator());
            m_validators.Add(new SQLiteValidator());
        }

        public void RunTests()
        {
            foreach (var v in m_validators)
            {
                Debug.WriteLine(string.Format("Beginning Validation of '{0}'...", v.StoreType));

                if (!v.DoCreateStore())
                {
                    Debug.WriteLine("  FAILED Create Store");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }

                if (!v.DoInserts())
                {
                    Debug.WriteLine("  FAILED Insert");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }

                if (!v.DoSelects())
                {
                    Debug.WriteLine("  FAILED Select");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }

                if (!v.DoUpdates())
                {
                    Debug.WriteLine("  FAILED Update");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }

                if (!v.DoDeletes())
                {
                    Debug.WriteLine("  FAILED Delete");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }

                if (!v.DoReferentialInserts())
                {
                    Debug.WriteLine("  FAILED Referential inserts");
                    if (Debugger.IsAttached) Debugger.Break();
                    continue;
                }
            }
        }
    }
}
