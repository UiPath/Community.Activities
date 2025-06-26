using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.ComponentModel;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;


using UiPathTeam.XLExcel;


namespace UiPathTeam.XLExcel.Activities
{
    public class GetNumberOfRows: ExcelSheetExtensionCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> SheetName { get; set; }

        [Category("Output")]
        public OutArgument<int> NumberOfRows { get; set; }


        protected override void Execute(CodeActivityContext context)
        {

            WorkflowDataContext dc = context.DataContext;
            //get Excel context
            XLExcelContextInfo customContext = Utils.GetXLExcelContextInfo(context);


            //retrieve the parameters from the Context
            string filePath = customContext.Path;
            string sheetName = SheetName.Get(context);

            string rowNum = "";
           
            //open file
            using (SpreadsheetDocument myDoc = SpreadsheetDocument.Open(filePath, true))
            {

                WorkbookPart workbookPart = myDoc.WorkbookPart;
                //determine ID of the Sheet
                string relId = workbookPart.Workbook.Descendants<Sheet>().First(s => sheetName.Equals(s.Name)).Id;

                if (String.IsNullOrEmpty(relId))
                    throw new Exception("Could not indentify the Excel Sheet");

                //open reader for Sheet
                WorksheetPart worksheetPart = workbookPart.GetPartById(relId) as WorksheetPart;
                OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);

               
                //read the XML objects until we reach the rows
                while (reader.Read())
                {
                    //check if current elem is row
                    if (reader.ElementType == typeof(Row))
                    {
                        //loop through row siblings
                        do
                        {
                            if (reader.HasAttributes)
                            {
                                //at each step, read the current rowNum
                                rowNum = reader.Attributes.First(a => a.LocalName == "r").Value;
                            }

                        } while (reader.ReadNextSibling());
                        break;
                    }

                }
            }

            int result;
            //convert the result to Int and pass it as an output argument
            if (Int32.TryParse(rowNum, out result))
            {
                NumberOfRows.Set(context, result);
            }
            else
            {
                throw new Exception("Unable to parse the line number");
            }
        }
    }
}
