using System;
using System.Activities.Presentation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for OracleConnectionUIControl.xaml
    /// </summary>
    public partial class OracleConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;
        string _host, _port, _service;

        private string DataSource
        {
            get
            {
                return _connectionProperties["Data Source"] as string;
            }
            set
            {
                if (value != null && value.Trim().Length > 0)
                {
                    _connectionProperties["Data Source"] = value.Trim();
                }
                else
                {
                    _connectionProperties.Reset("Data Source");
                }
            }
        }

        #region Public Properties
        public string Server
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value; SetDataSource();
            }
        }
        
        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value; 
                SetDataSource();
            }
        }
        
        public string Service
        {
            get
            {
                return _service;
            }
            set
            {
                _service = value; 
                SetDataSource();
            }
        }

        public string UserName
        {
            get
            {
                return _connectionProperties["User ID"] as string;
            }
            set
            {
                _connectionProperties["User ID"] = value.Trim();
            }
        }

        public string Password
        {
            get
            {
                return _connectionProperties["Password"] as string;
            }
            set
            {
                _connectionProperties["Password"] = value.Trim();
            }
        }
        
        public bool SavePassword
        {
            get
            {
                return (bool)_connectionProperties["Persist Security Info"];
            }
            set
            {
                _connectionProperties["Persist Security Info"] = value;

            }
        }
        #endregion

        public OracleConnectionUIControl()
        {
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if(connectionProperties == null)

            {
                throw new ArgumentNullException("connectionProperties");
            }

            if (!(connectionProperties is OracleConnectionProperties) &&
                !(connectionProperties is OleDBOracleConnectionProperties))
            {
                throw new ArgumentException(Properties.Resources.OracleConnectionUIControl_InvalidConnectionProperties);
            }

            _connectionProperties = connectionProperties as OracleConnectionProperties;
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = ((PasswordBox)sender).Password;
        }

        private void SetDataSource()
        {
            DataSource=  $"(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = {_host})(PORT = {_port})))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = {_service})))";
        }
    }
}
