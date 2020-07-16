using System;
using System.Activities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using System.ComponentModel;
using Google.Apis.Sheets.v4.Data;
using System.Data;
using System.Text.RegularExpressions;

namespace GoogleSpreadsheet.Activities
{
    public class ReadRange : GoogleInteropActivity<DataTable>
    {
        #region Properties

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Range { get; set; }

        [Category("Input")]
        public InArgument<string> Sheet { get; set; }

        [Category("Options")]
        public bool IncludeHeaders { get; set; }

        #endregion

        public static Regex regexRange = new Regex(@"([A-Z]+)\d+", RegexOptions.Compiled);

        #region GoogleInteropActivity

        protected override Task<DataTable> ExecuteAsync(AsyncCodeActivityContext context, SheetsService sheetService)
        {
            var range = Range.Get(context);
            var output = new DataTable();
            var includeHeaders = IncludeHeaders;
            var sheet = Sheet.Get(context);
            string rangeToPassToService;

            if (string.IsNullOrWhiteSpace(sheet))
            {
                rangeToPassToService = range;
            }
            else
            {
                rangeToPassToService = string.Format("{0}!{1}", sheet, range);
            }

            return Task.Factory.StartNew<DataTable>(() =>
            {
                SpreadsheetsResource.ValuesResource.GetRequest request =
                   sheetService.Spreadsheets.Values.Get(SpreadsheetId, rangeToPassToService);

                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;

                if (includeHeaders)
                {
                    foreach (var col in values[0])
                    {
                        output.Columns.Add(col.ToString());
                    }
                }
                else
                {
                    for (int i = 0; i < GetNumberOfColumnsFromRange(range); i++)
                    {
                        output.Columns.Add(string.Format("Column{0}", i));
                    }
                }

                AddRows(values, output, includeHeaders);

                return output;
            });
        }

        private void AddRows(IList<IList<object>> values, DataTable output, bool includeHeaders)
        {
            int startIndex = includeHeaders ? 1 : 0;

            for (int i = startIndex; i < values.Count; i++)
            {
                output.Rows.Add(((List<object>)values[i]).ToArray());
            }
        }

        #endregion

        #region Helpers

        public static int GetNumberOfColumnsFromRange(string range)
        {
            string[] rangeParts = GetRangeParts(range);

            var firstLetter = (int)rangeParts[0][0];
            var secondLetter = (int)rangeParts[1][0];

            /* if (secondLetter < firstLetter)
             {
                 throw new Exception("Invalid range specified.");
             }*/

            

            Match match = regexRange.Match(rangeParts[0]);
            string firstColName, secondColName;
            int firstColNumber = 0, secondColNumber = 0;
            if (match.Success)
            {
                firstColName = match.Groups[1].Value;
                firstColNumber = LetterToColumn(firstColName);
            }
            else
            {
                throw new ArgumentException("Invalid range specified: expected upper case letter(s) followed by digits for the first part of the range");
            }
            Match match2 = regexRange.Match(rangeParts[1]);
            if (match2.Success)
            {
                secondColName = match2.Groups[1].Value;
                secondColNumber = LetterToColumn(secondColName);
            }
            else
            {
                throw new ArgumentException("Invalid range specified: expected upper case letter(s) followed by digits for the second part of the range");
            }

            return secondColNumber - firstColNumber + 1;

            //return secondLetter - firstLetter + 1;
        }


        public static int LetterToColumn(String letter)
        {

            var column = 0;
            var length = letter.Length;
            for (var i = 0; i < length; i++)
            {
                column += (letter.Substring(i, 1)[0] - 64) * (int)(Math.Pow(26, length - i - 1));
            }

            return column;
        }


        private static string[] GetRangeParts(string range)
        {
            var rangeParts = range.Split(':');

            if (rangeParts.Length != 2 ||
                !char.IsLetter(rangeParts[0][0]) ||
                !char.IsLetter(rangeParts[1][0]))
            {
                throw new Exception("Invalid range specified.");
            }

            return rangeParts;
        }

        public static int GetNumberOfRowsFromRange(string range, bool includeHeaders = false)
        {
            string[] rangeParts = GetRangeParts(range);
            int firstNumber;
            int secondNumber;

            try
            {
                firstNumber = int.Parse(rangeParts[0].Substring(1));
                secondNumber = int.Parse(rangeParts[1].Substring(1));
            }
            catch (Exception e)
            {
                throw new Exception("Invalid range specified. " + e.Message);
            }

            if (secondNumber < firstNumber)
            {
                throw new Exception("Invalid range specified.");
            }

            if (includeHeaders)
            {
                return secondNumber - firstNumber;
            }
            else
            {
                return secondNumber - firstNumber + 1;
            }
        }

        #endregion
    }
}
