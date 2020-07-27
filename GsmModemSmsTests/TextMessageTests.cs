using Microsoft.VisualStudio.TestTools.UnitTesting;
using GsmModemSmsLibrary;
using GsmModemSmsLibrary.Entities;

namespace GsmModemSmsTests
{
    [TestClass]
    public class TextMessageTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var testString = "KR1_3 - Porucha zařízení";
            var msg = new TextMessage(testString, "+420123456789", 3);

            var expected = "KR1.3 - Porucha zarizeni";
            Assert.AreEqual(expected, msg.Text, "Not convered correctly to GSM character table");
        }

        [TestMethod]
        public void TestMethod2()
        {
            var testString = "KR1_5 - Maximální teplota přesažena [G.XXX]";
            var msg = new TextMessage(testString, "+420123456789", 3);

            var expected = "KR1.5 - Maximalni teplota presazena  G.XXX ";
            Assert.AreEqual(expected, msg.Text, "Not convered correctly to GSM character table");
        }
    }
}
