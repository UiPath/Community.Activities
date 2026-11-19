using System;
using System.Activities.Presentation;
using System.Windows;
using System.Windows.Controls;
using UiPath.Database;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for OracleConnectionUIControl.xaml
    /// </summary>
    public partial class OracleConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;
        string _host, _port, _service, _sid;

        bool _useSID;

        private string DataSource
        {
            get
            {
                return _connectionProperties[DatabaseConstants.Data_Source] as string;
            }
            set
            {
                if (value != null && value.Trim().Length > 0)
                {
                    _connectionProperties[DatabaseConstants.Data_Source] = value.Trim();
                }
                else
                {
                    _connectionProperties.Reset(DatabaseConstants.Data_Source);
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

        public string SID
        {
            get
            {
                return _sid;
            }
            set
            {
                _sid = value; SetDataSource();
            }
        }

        public bool UseSID
        {
            get 
            { 
                return _useSID;
            }
            set 
            {
                _useSID = value; SetDataSource();
            }
        }

        public string UserName
        {
            get
            {
                return _connectionProperties[DatabaseConstants.User_ID] as string;
            }
            set
            {
                _connectionProperties[DatabaseConstants.User_ID] = value.Trim();
            }
        }

        public string Password
        {
            get
            {
                return _connectionProperties[DatabaseConstants.Password] as string;
            }
            set
            {
                _connectionProperties[DatabaseConstants.Password] = value.Trim();
            }
        }
        
        public bool SavePassword
        {
            get
            {
                return (bool)_connectionProperties[DatabaseConstants.Persist_Security_Info];
            }
            set
            {
                _connectionProperties[DatabaseConstants.Persist_Security_Info] = value;

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
            if (_useSID)
                DataSource = $"(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = {_host})(PORT = {_port})))(CONNECT_DATA = (SERVER = DEDICATED)(SID = {_sid})))";
            else
                DataSource = $"(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = {_host})(PORT = {_port})))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = {_service})))";
        }
    }
}
