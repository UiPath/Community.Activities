using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Security;
using System.Reflection;
using System.Activities;


using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;

namespace UiPathTeam.XLExcel
{
    public static partial class Utils
    {


        public static XLExcelContextInfo GetXLExcelContextInfo(NativeActivityContext context)
        {
            var property = context.DataContext.GetProperties()[XLExcelContextInfo.XLExcelContextInfoTag];
            var ctx = property.GetValue(context.DataContext) as XLExcelContextInfo;

            return ctx;
        }
        public static XLExcelContextInfo GetXLExcelContextInfo(CodeActivityContext context)
        {
            var property = context.DataContext.GetProperties()[XLExcelContextInfo.XLExcelContextInfoTag];
            var ctx = property.GetValue(context.DataContext) as XLExcelContextInfo;

            return ctx;
        }

        public static DataTable ReadSAXRange(
          ExcelRange Range,
          string FilePath,
          string SheetName,
          bool addHeaders)
        {
            DataTable result = new DataTable();

            LinkedList<object[]> rows = new LinkedList<object[]>();
            int previousRow = Range.StartRow - 1, maxColNo;

            //if we know exactly the range we will read (we know the start and end columns) 
            //we do not have to save the rows in the linked list and we can add them directly to the output
            bool isLimitedRange = (Range.EndRow != -1 && Range.EndColumn != -1) ? true : false;

            //open file
            using (SpreadsheetDocument myDoc = SpreadsheetDocument.Open(FilePath, true))
            {
                WorkbookPart workbookPart = myDoc.WorkbookPart;
                //determine ID of the Sheet
                string relId = workbookPart.Workbook.Descendants<Sheet>().First(s => SheetName.Equals(s.Name)).Id;
                //open reader for Sheet
                WorksheetPart worksheetPart = workbookPart.GetPartById(relId) as WorksheetPart;
                OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                //get shared string array
                SharedStringItem[] sharedStringItemsArray = GetSharedStringItemsArray(workbookPart);

                //keep track of current expected row
                int prevRow = Range.StartRow - 1;

                while (reader.Read())
                {
                    //we found a row
                    if (reader.ElementType == typeof(Row))
                    {

                        //get current row number
                        OpenXmlAttribute attr = reader.Attributes.FirstOrDefault(a => a.LocalName == "r");

                        if (attr != null && attr.Value != null)
                        {
                            string rowNum = attr.Value;
                            int currentRow = int.Parse(rowNum);

                            ///make sure row index is correct
                            if (currentRow < 1 || currentRow > 1048576) throw new Exception("Cannot process rows whose index number is below 1 or above 1048576");

                            //add any missing rows to the table
                            FillMissingRows(prevRow, currentRow, Range, rows, isLimitedRange, result, addHeaders);

                            if (currentRow > Range.EndRow && Range.EndRow != -1)
                            {
                                //we're out of the current range, we simply break
                                break;
                            }
                            else if (currentRow >= Range.StartRow && (currentRow <= Range.EndRow || Range.EndRow == -1))
                            {
                                //add the current row to the table
                                List<object> row = GetSAXRowArray(reader, workbookPart, Range, sharedStringItemsArray);
                                //if we know the exact range; we add the rows directly to the DataTable; otherwise we store them
                                if (!isLimitedRange) rows.AddLast(row.ToArray());
                                else AddFullRowToDT(row.ToArray(), addHeaders, result);
                                prevRow = currentRow;
                            }
                        }
                    }
                }

                //add any missing rows to the table
                FillMissingRows(prevRow, Range.EndRow + 1, Range, rows, isLimitedRange, result, addHeaders);

                if (!isLimitedRange)
                {
                    //if we did not have the exact range, we add the rows to the DataTable when we reach the end of the document
                    maxColNo = rows.Max(x => x.Length);
                    Utils.AddColumnsToDT(Range, addHeaders, result, rows, maxColNo);
                    //POPULATING THE RESULT
                    result.AddRows(rows, maxColNo);
                }
            }
            return result;
        }


        private static void AddFullRowToDT(object[] Row, bool addHeaders, DataTable DT)
        {
            //if we are supposed to add headers but no columns added yet
            if (addHeaders && DT.Columns.Count == 0)
            {
                DT.AddColumns(Row);
                return;
            }
            else if (DT.Columns.Count == 0)
            {
                DT.AddColumns(Row.Length);
            }

            //add the row as normal
            DT.Rows.Add(Row);
        }

        //adding the columns to the table
        private static void AddColumnsToDT(ExcelRange Range, bool addHeaders, DataTable Result, LinkedList<object[]> Rows, int MaxColNo)
        {
            //adding column to the table
            if (addHeaders)
            {
                //get the header row
                object[] headers = Rows.First();
                Result.AddColumns(headers);

                //add more columns in case the header is smaller than the maximum sized row
                Result.AddColumns(MaxColNo - headers.Length);
                Rows.RemoveFirst();
            }
            else
            {

                //simply adding the correct number of empty column rows
                if (Range.EndRow == -1)
                {
                    Result.AddColumns(MaxColNo);
                }
                else
                {
                    Result.AddColumns(Range.ColumnCount);
                }
            }
        }

        private static void FillMissingRows(int PreviousRow, int CurrentRow, ExcelRange Range, LinkedList<object[]> Rows, bool isLimitedRange = false, DataTable DT = null, bool addHeaders = false)
        {
            //add any missing rows to the table
            while (PreviousRow < CurrentRow - 1)
            {
                PreviousRow++;
                if (isLimitedRange)
                {
                    AddFullRowToDT(Enumerable.Repeat<object>(null, Range.ColumnCount).ToArray(), addHeaders, DT);
                }
                else
                {
                    Rows.AddLast(Enumerable.Repeat<object>(null, (Range.EndColumn != -1 ? Range.ColumnCount : 1)).ToArray());
                }

            }
        }

        //gets the cells on a row as a List of objects
        private static List<object> GetSAXRowArray(OpenXmlReader Reader, WorkbookPart WorkbookPart, ExcelRange Range, SharedStringItem[] SharedStringArray)
        {
            List<object> results = new List<object>();
            Reader.ReadFirstChild();

            int previousCellNo = Range.StartColumn - 1;
            do
            {
                if (Reader.ElementType == typeof(Cell))
                {
                    Cell c = (Cell)Reader.LoadCurrentElement();

                    //get current column
                    int currentCol = c.GetCellColumn();
                    if (currentCol > Range.EndColumn && Range.EndColumn != -1)
                    {
                        //we're out of the current range, we simply break
                        break;
                    }
                    else if (currentCol >= Range.StartColumn && (currentCol <= Range.EndColumn || Range.EndColumn == -1))
                    {
                        GetCellValue(SharedStringArray, results, previousCellNo, c, currentCol);
                        previousCellNo = currentCol;
                    }
                }
            } while (Reader.ReadNextSibling());

            //fill in the last gaps
            if (Range.EndRow != -1) FillCellGaps(ref previousCellNo, Range.EndColumn, results);
            //return resulted Rows
            return results;
        }

        private static void GetCellValue(SharedStringItem[] SharedStringArray, List<object> Results, int PreviousCellNumber, Cell Cell, int CurrentCol)
        {

            //fill any gaps between the previous cell and the current one
            FillCellGaps(ref PreviousCellNumber, CurrentCol - 1, Results);

            //get the cell value
            string cellValue;
            if (Cell.DataType != null && Cell.DataType == CellValues.SharedString && SharedStringArray != null)
            {
                SharedStringItem ssi = SharedStringArray[int.Parse(Cell.CellValue.InnerText)];
                cellValue = ssi.Text.Text;
            }
            else if (Cell.CellValue != null) cellValue = Cell.CellValue.InnerText;
            else cellValue = null;

            //check if the value can be parsed to remove extra decimals
            double parsedValue;
            if (Double.TryParse(cellValue, out parsedValue))
            {
                cellValue = parsedValue.ToString();
            }

            //add received cell Value
            Results.Add(cellValue);
        }

        private static void FillCellGaps(ref int PreviousCellNo, int CurrentCol, List<object> Results)
        {
            //fill in gaps
            while (PreviousCellNo < CurrentCol)
            {
                Results.Add(null);
                PreviousCellNo++;
            }
        }


        private static SharedStringItem[] GetSharedStringItemsArray(WorkbookPart WorkbookPart)
        {
            SharedStringItem[] sharedStringItemsArray;

            try
            {
                sharedStringItemsArray = WorkbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray<SharedStringItem>();
            }
            catch (Exception)
            {
                sharedStringItemsArray = null;
            }

            return sharedStringItemsArray;
        }

    }





}
