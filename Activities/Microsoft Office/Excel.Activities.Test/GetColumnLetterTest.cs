using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Excel.Activities;

namespace Excel.Activities.Test
{
    [TestClass]
    public class GetColumnLetterTest
    {
        [TestMethod]
        public void CalculateColumnLetterTest()
        {
            var columns = new []{ (0,"A"), (26, "AA"), (702, "AAA"), (4082, "FAA"),  (16383,"XFD") };

            foreach(var col in columns)
            {
                Assert.AreEqual(GetColumnLetter.CalculateColumnLetter(col.Item1 + 1), col.Item2);
            }
        }
    }
}
