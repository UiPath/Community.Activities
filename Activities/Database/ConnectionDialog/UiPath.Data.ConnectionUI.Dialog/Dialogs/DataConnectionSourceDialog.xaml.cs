using System;
using System.Activities.Presentation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Res = UiPath.Data.ConnectionUI.Dialog.Properties;

namespace UiPath.Data.ConnectionUI.Dialog.Dialogs
{
    /// <summary>
    /// Interaction logic for DataConnectionSourceDialog.xaml
    /// </summary>
    public partial class DataConnectionSourceDialog : WorkflowElementDialog
    {
        private IDictionary<DataSource, IDictionary<DataProvider, IDataConnectionProperties>> _connectionPropertiesTable = new Dictionary<DataSource, IDictionary<DataProvider, IDataConnectionProperties>>();
        private DataSource _unspecifiedDataSource = DataSource.CreateUnspecified();
        private IDictionary<string, DataSource> _dataSources;
        private DataSource selectedDatasource;
        private DataProvider selectedProvider;

        #region Public Properties

        public string ConnectionString
        {
            get
            {
                string s = null;
                if (ConnectionProperties != null)
                {
                    try
                    {
                        s = ConnectionProperties.ToFullString();
                    }
                    catch { }
                }
                return (s != null) ? s : string.Empty;
            }
        }
        
        public DataProvider SelectedDataProvider
        {
            get
            {
                return selectedProvider;
            }
        }

        public DataSource SelectedDataSource
        {
            get { return selectedDatasource; }
        }

        public IDictionary<string, DataSource> DataSources
        {
            get { return _dataSources; }
        }

        #endregion

        internal IDataConnectionProperties ConnectionProperties
        {
            get
            {
                if (selectedProvider == null)
                {
                    return null;
                }
                if (!_connectionPropertiesTable.ContainsKey(SelectedDataSource))
                {
                    _connectionPropertiesTable[SelectedDataSource] = new Dictionary<DataProvider, IDataConnectionProperties>();
                }
                if (!_connectionPropertiesTable[SelectedDataSource].ContainsKey(selectedProvider))
                {
                    IDataConnectionProperties properties = null;
                    if (SelectedDataSource == _unspecifiedDataSource)
                    {
                        properties = selectedProvider.CreateConnectionProperties();
                    }
                    else
                    {
                        properties = selectedProvider.CreateConnectionProperties(SelectedDataSource);
                    }
                    if (properties == null)
                    {
                        properties = new BasicConnectionProperties();
                    }
                    properties.PropertyChanged += new EventHandler(ConfigureAcceptButton);
                    _connectionPropertiesTable[SelectedDataSource][selectedProvider] = properties;
                }
                return _connectionPropertiesTable[SelectedDataSource][selectedProvider];
            }
        }

        public DataConnectionSourceDialog()
        {
            InitializeComponent();
            DataConnectionConfiguration config = new DataConnectionConfiguration();
            _dataSources = config.DataSources; 
        }

        private void DataSourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDatasource = ((KeyValuePair<string, DataSource>)e.AddedItems[0]).Value;
            providerCombo.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            providerCombo.SelectedValue = selectedDatasource.DefaultProvider;
            contentControl.Content = selectedProvider?.CreateConnectionUIControl(selectedDatasource, ConnectionProperties);
            descriptionLabel.Text = selectedProvider?.GetDescription(selectedDatasource);
        }

        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                selectedProvider = (DataProvider)e.AddedItems[0];
            else
                selectedProvider = null;
            descriptionLabel.Text = selectedProvider?.GetDescription(selectedDatasource);
            contentControl.Content = selectedProvider?.CreateConnectionUIControl(selectedDatasource, ConnectionProperties);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                errorLabel.Text = null;
                errorLabel.Refresh();
                okLabel.Text = null; 
                okLabel.Refresh();
                ConnectionProperties.Test();
                okLabel.Text = Res.Resources.DataConnectionDialog_TestConnectionSucceeded;
            }
            catch(Exception ex)
            {
                errorLabel.Text = ex.Message;
            }
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionAdvancedDialog dataAdvancedDialog = new DataConnectionAdvancedDialog(ConnectionProperties);
            if (dataAdvancedDialog.ShowOkCancel())
                VisualTreeHelpers.RefreshBindings(this);
        }

        private void ConfigureAcceptButton(object sender, EventArgs e)
        {
            try
            {
                this.EnableOk((ConnectionProperties != null) ? ConnectionProperties.IsComplete : false);
            }
            catch
            {
                this.EnableOk(true);
            }
        }

        private class BasicConnectionProperties : IDataConnectionProperties
        {
            private string _s;

            #region Public Properties
            [Browsable(false)]
            public bool IsExtensible
            {
                get
                {
                    return false;
                }
            }

            [Browsable(false)]
            public bool IsComplete
            {
                get
                {
                    return true;
                }
            }

            public string ConnectionString
            {
                get
                {
                    return ToFullString();
                }
                set
                {
                    Parse(value);
                }
            }
            #endregion

            public event EventHandler PropertyChanged;

            public BasicConnectionProperties()
            {
            }

            public void Reset()
            {
                _s = string.Empty;
            }

            public void Parse(string s)
            {
                _s = s;
                PropertyChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(string propertyName)
            {
                throw new NotImplementedException();
            }

            public bool Contains(string propertyName)
            {
                return (propertyName == "ConnectionString");
            }

            public object this[string propertyName]
            {
                get
                {
                    if (propertyName == "ConnectionString")
                    {
                        return ConnectionString;
                    }
                    else
                    {
                        return null;
                    }
                }
                set
                {
                    if (propertyName == "ConnectionString")
                    {
                        ConnectionString = value as string;
                    }
                }
            }

            public void Remove(string propertyName)
            {
                throw new NotImplementedException();
            }

            public void Reset(string propertyName)
            {
                Debug.Assert(propertyName == "ConnectionString");
                _s = string.Empty;
            }

            public void Test()
            {
            }

            public string ToFullString()
            {
                return _s;
            }

            public string ToDisplayString()
            {
                return _s;
            }
        }
    }
}