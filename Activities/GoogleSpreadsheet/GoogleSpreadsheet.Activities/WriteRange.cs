using System;
using System.Activities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using System.ComponentModel;
using Google.Apis.Sheets.v4.Data;
using System.Data;

namespace GoogleSpreadsheet.Activities
{
    public class WriteRange : GoogleInteropActivity
    {
        #region Properties

        [Category("Input")]
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }

        [Category("Input")]
        public InArgument<string> SheetName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> StartingCell { get; set; }
        
        [Category("Options")]
        public bool IncludeHeaders { get; set; }

        #endregion

        #region GoogleInteropActivity

        protected override Task ExecuteAsync(AsyncCodeActivityContext context, SheetsService sheetService)
        {
            var includeHeaders = IncludeHeaders;
            var sheet = SheetName.Get(context);
            var startingCell = StartingCell.Get(context);
            var dataTable = DataTable.Get(context);

            string cellToPassToService;

            if (string.IsNullOrWhiteSpace(sheet))
            {
                cellToPassToService = startingCell;
            }
            else
            {
                cellToPassToService = string.Format("{0}!{1}", sheet, startingCell);
            }

            return Task.Factory.StartNew<object>(() =>
            {
                ValueRange requestBody = CreateRequestBodyWithValues(dataTable, includeHeaders);
                
                SpreadsheetsResource.ValuesResource.UpdateRequest request =
                   sheetService.Spreadsheets.Values.Update(requestBody, SpreadsheetId, cellToPassToService);

                //request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                var response = request.Execute();
                
                return response;
            });
        }

        private ValueRange CreateRequestBodyWithValues(DataTable dataTable, bool includeHeaders = false)
        {
            var result = new ValueRange()
            {
                Values = new List<IList<object>>()
            };

            if (includeHeaders)
            {
                IList<object> columns = new List<object>();
                foreach(var col in dataTable.Columns)
                {
                    columns.Add(col.ToString());
                }

                result.Values.Add(columns);
            }

            foreach(DataRow row in dataTable.Rows)
            {
                result.Values.Add(row.ItemArray);
            }

            return result;
        }
        
        #endregion
    }
}
