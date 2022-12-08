using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using UiPath.Data.ConnectionUI.Dialog.Dialogs;

namespace UiPath.Database.Activities.Design
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ConnectionDialog : WorkflowElementDialog
    {
        private const string DefaultSqlClient = "System.Data.SqlClient";
        private const string MicrosoftSqlClient = "Microsoft.Data.SqlClient";
        private const string OracleClient = "Oracle.ManagedDataAccess.Client";

        public static List<string> ProviderNames { get; set; }

        public ConnectionDialog(ModelItem modelItem)
        {
            ProviderNames = new List<string>();
            List<string> providers = new List<string>();
#if NETFRAMEWORK
            providers.Add(OracleClient);
            providers.Add(MicrosoftSqlClient);
#endif
#if NETCOREAPP
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("System.Data.OleDb", System.Data.OleDb.OleDbFactory.Instance);
            DbProviderFactories.RegisterFactory("System.Data.Odbc", System.Data.Odbc.OdbcFactory.Instance);
            DbProviderFactories.RegisterFactory("Oracle.ManagedDataAccess.Client", Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance);
#endif
            var installedProviders = DbProviderFactories.GetFactoryClasses();
            foreach (DataRow installedProvider in installedProviders.Rows)
            {
                if((installedProvider["InvariantName"] as string).ToLower() != DefaultSqlClient)
                    ProviderNames.Add(installedProvider["InvariantName"] as string);
            }
            foreach (var provider in providers)
                if (!ProviderNames.Contains(provider))
                    ProviderNames.Add(provider);
            InitializeComponent();
            ModelItem = modelItem;
            Context = modelItem.GetEditingContext();
        }

        private void NewConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataConnectionSourceDialog dataConnectionDialog = new DataConnectionSourceDialog();
                if (dataConnectionDialog.ShowOkCancel())
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