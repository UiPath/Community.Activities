
namespace UiPath.Database
{
    public static class DatabaseConstants
    {
        #region MainProviders

        //Oracle Data Provider for .NET Assemblies
        public const string OracleProvider = "Oracle.ManagedDataAccess.Client";

        //Microsoft SqlClient Data Provider for SQL Server
        public const string SqlServerProvider = "Microsoft.Data.SqlClient";

        //Data provider for OLE DB data sources
        public const string OleDbProvider = "System.Data.OleDb";

        //Data provider for ODBC data sources
        public const string OdbcProvider = "System.Data.Odbc";

        #endregion

        #region OleDbProviders

        //Microsoft OLE DB Driver for SQL Server
        public const string OleDbSql = "SQLOLEDB";

        //Microsft OLE DB provider to connect to Access (.mdb), Excel (.xls) and CSV and text files
        public const string OleDbJet = "Microsoft.Jet.OLEDB.4.0";

        //Microsoft OLE DB Provider for Access
        public const string OleDbAce = "Microsoft.ACE.OLEDB.12.0";

        //Microsoft OLE DB Provider for Oracle
        public const string OleDbOra = "MSDAORA";

        #endregion

        #region ConnectionProperties

        public const string SqlServerNativeClient = "SQLNCLI";

        public const string Provider = "Provider";

        public const string Password = "Password";

        public const string User_ID = "User ID";

        public const string Data_Source = "Data Source";

        public const string Persist_Security_Info = "Persist Security Info";

        public const string AttachDbFilename = "AttachDbFilename";

        public const string Integrated_Security = "Integrated Security";

        public const string UID = "UID";

        public const string ServerName = "ServerName";

        public const string InstanceName = "InstanceName";

        public const string User_Instance = "User Instance";

        public const string Trusted_Connection = "Trusted_Connection";

        public const string Pooling = "Pooling";

        #endregion

        #region Miscellaneous 

        //Document-Based Question
        public const string DBQ = "DBQ";

        //Pattern to match oracle connections
        public const string OraclePattern = "oracle";

        public const string OracleOdbcDriverPattern = "SQORA";

        public const string DB2OdbcDriverPattern = "DB2";

        #endregion
    }
}
