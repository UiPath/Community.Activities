using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;


using System.ComponentModel;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using UiPathTeam.XLExcel;
using UiPathTeam.XLExcel.Activities.Design;
using System.IO;
using System.Activities;
using System.Activities.XamlIntegration;

namespace TestExcelExtensions
{
    public class Program
    {
        static void Main(string[] args)
        {

           /* ActivityXamlServicesSettings settings = new ActivityXamlServicesSettings
            {
                CompileExpressions = true
            };

            Activity workflow = ActivityXamlServices.Load("C:\\UiPath\\FileConverter\\ExcelActivityTest.xaml", settings);
            System.Activities.Validation.ValidationResults results = System.Activities.Validation.ActivityValidationServices.Validate(workflow);



            ValidateWorlflow("C:\\UiPath\\FileConverter\\ExcelActivityTest.xaml");*/

            //TestRowNO();
            TestWorkflow();
            //TestFileConversion();
            //TestReadRange();
            //TestRange();
            //TestReadRangeBasic();
            //TestReadRangeBasicDOM();
        }

        private static void ValidateWorlflow(string xamlFile)

        {

            List<string> listError = new List<string>();

            DynamicActivity activity = null;



            try

            {

                // load the xaml 

                activity = System.Activities.XamlIntegration.ActivityXamlServices.Load(new StringReader(xamlFile)) as DynamicActivity;

            }

            catch (Exception ex)

            {

                listError.Add("In the workflow contains following errors and warnings :");

                listError.Add(ex.Message);

               

                // logger.Error("Unable to load workflow", ex);

                // throw new InvalidWorkflowException("Workflow is not valid", ex);

            }



            // validate the activity through ActivityValidationServices

            System.Activities.Validation.ValidationResults results = System.Activities.Validation.ActivityValidationServices.Validate(activity);

            // checking whether workflow has any errors or not 

            if (results.Errors.Count > 0)

            {

                listError.Add("In the workflow contains following errors and warnings :");

                foreach (System.Activities.Validation.ValidationError error in results.Errors)

                {

                    listError.Add(error.Message);



                }

                // adding worklfow warnings in the listError 

                foreach (System.Activities.Validation.ValidationError warning in results.Warnings)

                {

                    listError.Add(warning.Message);

                }

            }
        }

            static void TestFileConversion()
        {

            //string newFilePath = Utils.ConvertToXLSX("C:\\UiPath\\P0120180117_034752.xls", "Temp2", "C:\\UiPath");

           // Console.WriteLine(newFilePath);
           // Console.ReadLine();
        }

        static void TestReadRangeBasic()
        {
            ExcelRange range;
            try
            {
                //range = new ExcelRange("C3:F10");
                range = new ExcelRange("");
                Console.WriteLine("Start Row: " + range.StartRow + ". Start Col: " + range.StartColumn + ".End Row: " + range.EndRow + ".End Col: " + range.EndColumn);


                DataTable dt = Utils.ReadSAXRange(range, "C:\\Users\\bucur\\source\\repos\\ExcelSheetExtensionClasses\\TestExcelExtensions\\TestFiles\\LALALA.xlsx", "Sheet1", false);
                Console.WriteLine(DumpDataTable(dt));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }

            Console.ReadLine();
        }

        static void TestRange()
        {
            ExcelRange range;

            try
            {
                range = new ExcelRange("EE2:TR13");
                Console.WriteLine("Start Row: " + range.StartRow + ". Start Col: " + range.StartColumn + ".End Row: " + range.EndRow + ".End Col: " + range.EndColumn);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
            try
            {
                range = new ExcelRange("A12:B13");
                Console.WriteLine("Start Row: " + range.StartRow + ". Start Col: " + range.StartColumn + ".End Row: " + range.EndRow + ".End Col: " + range.EndColumn);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
            try
            {
                range = new ExcelRange("D1:B3");
                Console.WriteLine("Start Row: " + range.StartRow + ". Start Col: " + range.StartColumn + ".End Row: " + range.EndRow + ".End Col: " + range.EndColumn);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
           

            Console.ReadLine();
        }

        static public void TestWorkflow()
        {

            ActivityXamlServicesSettings settings = new ActivityXamlServicesSettings
            {
                CompileExpressions = true
            };
            Activity workflow = ActivityXamlServices.Load("ExcelActivityTest.xaml", settings);
            WorkflowInvoker.Invoke(workflow);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
        static public void TestRowNO()
        {
            //testing our custom activity
            Dictionary<string, object> wfParams = new Dictionary<string, object>()
            {
                {"FilePath","C:\\Work\\AMS_SC_Inputs_201801.xlsx"},
                {"SheetName","US"}
            };


            var Result = WorkflowInvoker.Invoke(new UiPathTeam.XLExcel.Activities.GetNumberOfRows(), wfParams);

            object dec;
            bool success = Result.TryGetValue("NumberOfRows", out dec);
            Console.WriteLine(dec.ToString());
            Console.ReadLine();
        }


        static public void TestReadRange()
        {

            string filePath = "C:\\Work\\AMS_SC_Inputs_201801.xlsx";
            string sheetName = "US";

            //open file
            using (SpreadsheetDocument myDoc = SpreadsheetDocument.Open(filePath, true))
            {

                WorkbookPart workbookPart = myDoc.WorkbookPart;
                //determine ID of the Sheet
                string relId = workbookPart.Workbook.Descendants<Sheet>().First(s => sheetName.Equals(s.Name)).Id;

                //open reader for Sheet
                WorksheetPart worksheetPart = workbookPart.GetPartById(relId) as WorksheetPart;
                OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);

                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Row))
                    {
                        reader.ReadFirstChild();

                        do
                        {
                           

                                if (reader.ElementType == typeof(Cell))
                                {
                                    Cell c = (Cell)reader.LoadCurrentElement();

                                    string cellValue;
                                    

                                    if (c.DataType != null && c.DataType == CellValues.SharedString)
                                    {
                                        SharedStringItem ssi = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(c.CellValue.InnerText));

                                        cellValue = ssi.Text.Text;
                                    }
                                    else
                                    {
                                        cellValue = c.CellValue.InnerText;
                                    }

                                    Console.Out.Write("{0}: {1} ", c.CellReference, cellValue);
                                }

                        } while (reader.ReadNextSibling());
                        Console.Out.WriteLine();
                    }
                }
            }
        }

        public static string DumpDataTable(DataTable table)
        {
            string data = string.Empty;
            StringBuilder sb = new StringBuilder();

            if (null != table && null != table.Rows)
            {
                foreach (DataRow dataRow in table.Rows)
                {
                    foreach (var item in dataRow.ItemArray)
                    {
                        sb.Append(item);
                        sb.Append(',');
                    }
                    sb.AppendLine();
                }

                data = sb.ToString();
            }
            return data;
        }

       

    }
}

