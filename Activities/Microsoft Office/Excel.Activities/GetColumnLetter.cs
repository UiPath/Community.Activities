using System;
using System.Activities;
using System.ComponentModel;
using System.Data;

namespace Excel.Activities
{
    public class GetColumnLetter : CodeActivity
    {
        #region Properties

        [Category("Input"), Description("Name of Column (exact match)")]
        [RequiredArgument]
        public InArgument<string> ColumnName { get; set; }

        [Category("Input"), Description("DataTable to obtain Column Letter from")]
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }

        [Category("Output"), Description("Column Letter in Excel (e.g. A, BZ, etc.)")]
        public OutArgument<string> ColumnLetter { get; set; }

        #endregion

        #region CodeActivity

        protected override void Execute(CodeActivityContext context)
        {
            string cn = ColumnName.Get(context);
            DataTable dt = DataTable.Get(context);

            if (!dt.Columns.Contains(cn))
            {
                throw new ArgumentException("Column '" + cn + "' was not found");
            }

            //add 1 to the column index since we always start with column index 0
            int ci = dt.Columns.IndexOf(cn) + 1;
            ColumnLetter.Set(context, CalculateColumnLetter(ci));
        }

        #endregion

        #region HelperMethods

        public static string CalculateColumnLetter(int columnIndex)
        {
            int temp = 0;
            string columnLetter = String.Empty;
            while (columnIndex > 0)
            {
                temp = (columnIndex - 1) % 26;
                columnLetter = (char)(65 + temp) + columnLetter;
                columnIndex = (int)((columnIndex - temp) / 26);
            }

            return columnLetter;
        }

        #endregion

    }
}