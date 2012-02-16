using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM.Xml;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new SqlCeTest();
            test.RunTests();
        }
    }
}
