using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Security;
using System.Reflection;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;

namespace UiPathTeam.XLExcel
{
    public static class ExtensionClasses
    {

        public static void AddColumns(this DataTable DT, int Count)
        {
            for (int i = 0; i < Count; i++)
                DT.Columns.Add();
        }
        public static void AddColumns(this DataTable DT, object[] Columns)
        {
            foreach (object header in Columns)
            {
                DT.Columns.Add(header != null ? header.ToString() : "");
            }
        }

        public static void AddRows(this DataTable DT, List<object[]> Rows)
        {
            foreach (object[] row in Rows)
            {
                DT.Rows.Add(row);
            }
        }
        public static void AddRows(this DataTable DT, List<List<object>> Rows)
        {
            foreach (List<object> row in Rows)
            {
                DT.Rows.Add(row.ToArray());
            }
        }
        public static void AddRows(this DataTable DT, LinkedList<object[]> Rows, int MaxColumnNumber)
        {
            while(Rows.Count> 0)
            {
                object[] currentRow = Rows.First.Value;
                if (MaxColumnNumber > currentRow.Length)
                {
                    List<object> currentRowAsList = (currentRow.ToList());
                    currentRowAsList.AddRange(
                        Enumerable.Repeat<object>(null, MaxColumnNumber - currentRow.Length));

                    currentRow = currentRowAsList.ToArray();
                }
                
                DT.Rows.Add(currentRow);
                Rows.RemoveFirst();
            }
        }

        public static int GetCellColumn(this Cell Cell)
        {
            int startRow=0, startCol=0;

            //get the coordinates from the Cell Reference
            ExcelRange.ExcelColumnNameToCoordinates(Cell.CellReference, ref startRow, ref startCol);
            return startCol;
           

        }

        public static string Dump(this DataTable Table)
        {
            string data = string.Empty;
            StringBuilder sb = new StringBuilder();

            if (null != Table && null != Table.Rows)
            {
                for (int i=0;i<Table.Rows.Count;i++)
                {
                    DataRow dataRow = Table.Rows[i];
                    /*foreach (var item in dataRow.ItemArray)
                    {
                        sb.Append(item);
                        sb.Append(',');
                    }*/
                    sb.Append(String.Join(",", dataRow.ItemArray));
                    if(i+1 <Table.Rows.Count)sb.AppendLine();
                }
                data = sb.ToString();
            }
            return data;
        }

        public static string DumpColumns(this DataTable Table)
        {
          
            string Columns = String.Join(";", Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray());
            return Columns;
        }
    }
}
