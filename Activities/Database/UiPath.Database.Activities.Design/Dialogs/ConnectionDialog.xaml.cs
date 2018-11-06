using Microsoft.Data.ConnectionUI;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Windows;

namespace UiPath.Database.Activities.Design
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ConnectionDialog : WorkflowElementDialog
    {
        public static List<string> ProviderNames { get; set; }

        public ConnectionDialog(ModelItem modelItem)
        {
            ProviderNames = new List<string>();
            var installedProviders = DbProviderFactories.GetFactoryClasses();
            foreach (DataRow installedProvider in installedProviders.Rows)
            {
                ProviderNames.Add(installedProvider["InvariantName"] as string);
            }
            InitializeComponent();
            this.ModelItem = modelItem;
            this.Context = modelItem.GetEditingContext();
        }

        private void NewConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataConnectionDialog dataConnectionDialog = new DataConnectionDialog();
                DataConnectionConfiguration dataConnectionSetting = new DataConnectionConfiguration(null);
                dataConnectionSetting.LoadConfiguration(dataConnectionDialog);

                if (DataConnectionDialog.Show(dataConnectionDialog) == System.Windows.Forms.DialogResult.OK)
                {
                    string connString = dataConnectionDialog.ConnectionString;
                    string provName = dataConnectionDialog.SelectedDataProvider.Name;

                    ModelItem.Properties["ConnectionString"].SetValue(new InArgument<string>(connString));
                    ModelItem.Properties["ProviderName"].SetValue(new InArgument<string>(provName));

                    if (ModelItem.Properties["ExistingDbConnection"] != null)
                    {
                        ModelItem.Properties["ExistingDbConnection"].SetValue(null);
                    }
                }
            }
            catch { }
        }

        private void ComboboxControl_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var selectedValue = (e.OriginalSource as System.Windows.Controls.ComboBox).SelectedValue;
            if (selectedValue != null)
            {
                ModelItem.Properties["ProviderName"].SetValue(new InArgument<string>(selectedValue.ToString()));
            }
        }
    }
}