using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.XLExcel
{
    public class XLExcelContextInfo
    {
        public string Path;
        public string SheetName;

        public static string XLExcelContextInfoTag { get { return "XLExcelContextInfoInfoTag"; } }

        public void WriteProperties()
        {
            Console.WriteLine("path: " + Path);
            Console.WriteLine("sheetName: " + SheetName);
        }


    }

}
