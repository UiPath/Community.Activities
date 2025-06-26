using Microsoft.Win32;
using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UiPathTeam.XLExcel.Activities.Design
{
    // Interaction logic for XLExcelApplicationScopeDesigner.xaml
    public partial class XLExcelApplicationScopeDesigner
    {

        public XLExcelApplicationScopeDesigner()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog _openFileDialog = new OpenFileDialog();
            _openFileDialog.Title = "Open XLSX File";
            _openFileDialog.Filter = "Excel Files|*.xlsx;*.xlsm";
            _openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

            if (_openFileDialog.ShowDialog() == true)
            {
                ModelProperty property = this.ModelItem.Properties["FilePath"];
                //property
                property.SetValue(new InArgument<string>(TrimFilePath(_openFileDialog.FileName, Directory.GetCurrentDirectory())));


            }
        }

        //if the file is inside our current directory, we remove the absolute path
        public static string TrimFilePath(string initialPath, string absolutePath)
        {
            if (initialPath.StartsWith(absolutePath))
            {
                return initialPath.Remove(0, absolutePath.Length).TrimStart('\\');
            }

            return initialPath;
        }

    }
}
