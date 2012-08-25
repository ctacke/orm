using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Text;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [TestClass()]
    public class DataTypeTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        [DeploymentItem("typetest.sdf")]
        public void BinaryTest()
        {
            var store = new SqlCeDataStore("typetest.sdf");
            store.AddType<BinaryData>();

            Assert.IsTrue(store.StoreExists);

            store.TruncateTable("BinaryData");

            var text =                 "Section 1\r\n" +
                "All legislative Powers herein granted shall be vested in a Congress of the United States, which shall consist of a Senate and House of Representatives.\r\n" +
                "Section 2\r\n" +
                "1:  The House of Representatives shall be composed of Members chosen every second Year by the People of the several States, and the Electors in each State shall have the Qualifications requisite for Electors of the most numerous Branch of the State Legislature.\r\n" +
                "2:  No Person shall be a Representative who shall not have attained to the Age of twenty five Years, and been seven Years a Citizen of the United States, and who shall not, when elected, be an Inhabitant of that State in which he shall be chosen.\r\n" +
                "3:  Representatives and direct Taxes shall be apportioned among the several States which may be included within this Union, according to their respective Numbers, which shall be determined by adding to the whole Number of free Persons, including those bound to Service for a Term of Years, and excluding Indians not taxed, three fifths of all other Persons.2   The actual Enumeration shall be made within three Years after the first Meeting of the Congress of the United States, and within every subsequent Term of ten Years, in such Manner as they shall by Law direct.  The Number of Representatives shall not exceed one for every thirty Thousand, but each State shall have at Least one Representative; and until such enumeration shall be made, the State of New Hampshire shall be entitled to chuse three, Massachusetts eight, Rhode-Island and Providence Plantations one, Connecticut five, New-York six, New Jersey four, Pennsylvania eight, Delaware one, Maryland six, Virginia ten, North Carolina five, South Carolina five, and Georgia three.\r\n" +
                "4:  When vacancies happen in the Representation from any State, the Executive Authority thereof shall issue Writs of Election to fill such Vacancies.\r\n" +
                "5:  The House of Representatives shall chuse their Speaker and other Officers; and shall have the sole Power of Impeachment.\r\n";

            var item = new BinaryData()
            {
                NTextField = text,
                ImageField = Encoding.UTF8.GetBytes(text),
                BinaryField = Encoding.Unicode.GetBytes(text)

            };

            store.Insert(item);

            var item2 = store.Select<BinaryData>(item.ID);
            Assert.AreEqual(item.NTextField, item2.NTextField);
            var text2 = Encoding.UTF8.GetString(item2.ImageField);
            Assert.IsTrue(string.Compare(text, text2) == 0);
            var text3 = Encoding.Unicode.GetString(item2.BinaryField);
            Assert.IsTrue(string.Compare(text, text3) == 0);
        }
    }

}
