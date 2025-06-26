using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.ComponentModel;
using System.Data;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using System.Windows;


namespace UiPathTeam.XLExcel.Activities
{

    public class ReadRange: ExcelSheetExtensionCodeActivity
    {
        public ReadRange() {

            SheetName = "Sheet1";
            Range = "";
        }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> SheetName { get; set; }

        [Category("Input")]
        public InArgument<string> Range { get; set; }

        [Category("Input")]
        public bool Headers { get; set; }

        [Category("Output")]
        public OutArgument<DataTable> Result { get; set; }

        protected override void Execute(CodeActivityContext context)
        {

            //get Excel context
            XLExcelContextInfo customContext = Utils.GetXLExcelContextInfo(context);

            //retrieve the parameters from the Context
            string filePath = customContext.Path;

            //retrieve the parameters from the Context
            string range = Range.Get(context);
            string sheetName = SheetName.Get(context);

            bool addHeaders = Headers;//.Get(context);

            //excel range init
            ExcelRange excelRange = new ExcelRange(range);

            DataTable result = Utils.ReadSAXRange(excelRange, filePath, sheetName, addHeaders);
            //"C:\\Users\\bucur\\source\\repos\\ExcelSheetExtensionClasses\\TestExcelExtensions\\TestFiles\\LALALA.xlsx", "Sheet1", false);

            //set the result
            Result.Set(context, result);
        }
    }
}
