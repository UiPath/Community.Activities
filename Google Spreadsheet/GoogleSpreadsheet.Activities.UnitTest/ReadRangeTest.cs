using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleSpreadsheet.Activities;

namespace GoogleSpreadSheetUnitTests
{
    [TestClass]
    public class ReadRangeTest
    {
        [TestMethod]
        public void GetNumberOfColumnsFromRange()
        {
            var range = "A1:C7";

            var numberOfColumns = ReadRange.GetNumberOfColumnsFromRange(range);

            Assert.AreEqual(3, numberOfColumns);
        }

        [TestMethod]
        public void GetNumberOfRowsFromRange()
        {
            var range = "A3:C7";

            var numberOfRows = ReadRange.GetNumberOfRowsFromRange(range);

            Assert.AreEqual(5, numberOfRows);
        }

        [TestMethod]
        public void GetNumberOfRowsFromRangeGivenIncludeHeadersTrue()
        {
            var range = "A3:C7";

            var numberOfRows = ReadRange.GetNumberOfRowsFromRange(range, true);

            Assert.AreEqual(4, numberOfRows);
        }

    }
}
