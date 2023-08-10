using Oracle.ManagedDataAccess.Client;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UiPath.Database.BulkOps;
using UiPath.Database.Properties;
using UiPath.Data.ConnectionUI.Dialog.Workaround;
using UiPath.Robot.Activities.Api;

namespace UiPath.Database
{
    public class DatabaseConnection : IDisposable
    {
        private DbConnection _connection;
        private DbCommand _command;
        private DbTransaction _transaction;
        private string _providerName;
        private const string SqlOdbcDriverPattern = "SQLSRV";
        private const string OracleOdbcDriverPattern = "SQORA";
        private const string DB2OdbcDriverPattern = "DB2";
        private const string OraclePattern = "oracle";
        private const string OracleProvider = "oracle.manageddataaccess.client";
        private const string SqlProvider = "microsoft.data.sqlclient";
        private bool _isWindows = true;

        public ConnectionState? State => _connection?.State;

        public DatabaseConnection()
        {
#if NETCOREAPP
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
            DbWorkarounds.SNILoadWorkaround(_isWindows);
        }

        public DatabaseConnection Initialize(DbConnection connection)
        {
            _connection = connection;
            OpenConnection();
            return this;
        }

        public DatabaseConnection Initialize(string connectionString, string providerName)
        {
            _providerName = providerName;

#if NETCOREAPP
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);

            //OLEDB driver is Windows propietary - there is no support for other OS
            if(_isWindows)
                DbProviderFactories.RegisterFactory("System.Data.OleDb", System.Data.OleDb.OleDbFactory.Instance);

            DbProviderFactories.RegisterFactory("System.Data.Odbc", System.Data.Odbc.OdbcFactory.Instance);
            DbProviderFactories.RegisterFactory("Oracle.ManagedDataAccess.Client", Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance);
#endif
            switch (providerName.ToLower())
            {
                case SqlProvider:
                    _connection = new SqlConnection();
                    break;
                case OracleProvider:
                    _connection = new OracleConnection();
                    break;
                default:
                    _connection = DbProviderFactories.GetFactory(providerName).CreateConnection();
                    break;
            }
            _connection.ConnectionString = connectionString;
            OpenConnection();
            return this;
        }

        public virtual void BeginTransaction()
        {
            _transaction = _connection.BeginTransaction();
        }

        public virtual DataTable ExecuteQuery(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            OpenConnection();
            SetupCommand(sql, parameters, commandTimeout, commandType);
            _command.Transaction = _transaction;
            DataTable dt = new DataTable();
            dt.Load(_command.ExecuteReader());
            foreach (var param in _command.Parameters)
            {
                var dbParam = param as DbParameter;
                parameters[dbParam.ParameterName] = new Tuple<object, ArgumentDirection>(dbParam.Value, WokflowParameterDirectionToDbParameter(dbParam.Direction));
            }
            return dt;
        }

        public virtual int Execute(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            OpenConnection();
            SetupCommand(sql, parameters, commandTimeout, commandType);
            _command.Transaction = _transaction;
            var result = _command.ExecuteNonQuery();
            foreach (var param in _command.Parameters)
            {
                var dbParam = param as DbParameter;
                parameters[dbParam.ParameterName] = new Tuple<object, ArgumentDirection>(dbParam.Value, WokflowParameterDirectionToDbParameter(dbParam.Direction));
            }
            return result;
        }

        public virtual int InsertDataTable(string tableName, DataTable dataTable, bool removeBrackets = false)
        {
            DbDataAdapter dbDA = GetCurrentFactory().CreateDataAdapter();
            DbCommandBuilder cmdb = GetCurrentFactory().CreateCommandBuilder();
            cmdb.DataAdapter = dbDA;
            dbDA.ContinueUpdateOnError = false;

            dbDA.SelectCommand = _connection.CreateCommand();
            dbDA.SelectCommand.Transaction = _transaction;
            dbDA.SelectCommand.CommandType = CommandType.Text;
            try
            {
                dbDA.SelectCommand.CommandText = string.Format("select {0} from {1}", GetColumnNames(dataTable, removeBrackets), tableName);
                dbDA.InsertCommand = cmdb.GetInsertCommand();
            }
            catch (Exception)
            {
                //try again with/without brackets
                dbDA.SelectCommand.CommandText = string.Format("select {0} from {1}", GetColumnNames(dataTable, !removeBrackets), tableName);
                dbDA.InsertCommand = cmdb.GetInsertCommand();
            }

            dbDA.InsertCommand.Connection = _connection;
            dbDA.InsertCommand.Transaction = _transaction;

            foreach (DataRow row in dataTable.Rows)
            {
                if (row.RowState == DataRowState.Unchanged)
                    row.SetAdded();
            }

            return dbDA.Update(dataTable);
        }

        public virtual bool SupportsBulk()
        {
            if (_connection is OracleConnection || _connection is SqlConnection)
                return true;
            return false;
        }

        public long BulkInsertDataTable(string tableName, DataTable dataTable, IExecutorRuntime executorRuntime = null)
        {
            if (SupportsBulk())
                return DoBulkInsert(tableName, dataTable);
            else
            {
                //if no bulk insert possible, fallback to insert data table with warning message

                if (executorRuntime != null)
                {
                    LogWarningMessage(executorRuntime);
                }
                return InsertDataTable(tableName, dataTable);
            }
        }

        private long DoBulkInsert(string tableName, DataTable dataTable)
        {
            IBulkOperations bulkOps = BulkOperationsFactory.Create(_connection);
            bulkOps.Connection = _connection;
            bulkOps.TableName = tableName;
            ValidateDatabaseTableStructure(tableName, dataTable);
            var countStart = CountRowsInTable(tableName);
            bulkOps.WriteToServer(dataTable);
            var countEnd = CountRowsInTable(tableName);
            return countEnd - countStart;
        }

        private long CountRowsInTable(string tableName)
        {
            var commandRowCount = _connection.CreateCommand();
            // Perform an initial count on the destination table.
            var countQuery = string.Format("SELECT COUNT(*) FROM {0}", tableName);
            commandRowCount.Transaction = _transaction;
            commandRowCount.CommandType = CommandType.Text;
            commandRowCount.CommandText = countQuery;

            return Convert.ToInt32(commandRowCount.ExecuteScalar());
        }

        private static void ValidateTableStructure(string tableName, DataTable dataTable, DataSet ds)
        {
            if (ds != null && ds.Tables != null && ds.Tables?.Count > 0)
            {
                var tableCols = dataTable.Columns.Count;
                var datasetCols = ds.Tables[0].Columns.Count;

                if (tableCols != datasetCols)
                {
                    throw new InvalidOperationException(Resources.BulkInsert_NumberOfColumnsMustMatch);
                }

                for (int i = 0; i < datasetCols; i++)
                {
                    if (ds.Tables[0].Columns[i].DataType != dataTable.Columns[i].DataType)
                    {
                        throw new InvalidOperationException(Resources.BulkInsert_ColumnsDataTypesMustMatch);
                    }
                    if (ds.Tables[0].Columns[i].ColumnName != dataTable.Columns[i].ColumnName)
                    {
                        throw new InvalidOperationException(string.Format(Resources.BulkInsert_ColumnsNamesMustMatch, dataTable.Columns[i].ColumnName, tableName));
                    }
                }
            }
        }
        private DbProviderFactory GetCurrentFactory()
        {
            if (DbProviderFactories.GetFactory(_connection) == null)
                return DbProviderFactories.GetFactory(_providerName);
            return DbProviderFactories.GetFactory(_connection);
        }

        private void ValidateDatabaseTableStructure(string tableName, DataTable dataTable)
        {
            if (_connection == null)
                return;
            DbDataAdapter dbDA = GetCurrentFactory().CreateDataAdapter();
            dbDA.SelectCommand = _connection.CreateCommand();
            dbDA.SelectCommand.Transaction = _transaction;
            dbDA.SelectCommand.CommandType = CommandType.Text;
            dbDA.SelectCommand.CommandText = string.Format("select * from {0}", tableName);

            var ds = new DataSet();
            dbDA.FillSchema(ds, SchemaType.Source);
            ValidateTableStructure(tableName, dataTable, ds);
        }

        private string CreateTempTableForUpdate(DataTable dataTable, string tableName, DbCommand cmd)
        {
            var tempTableName = string.Format("a{0}", DateTime.Now.Ticks);
            if (_connection is SqlConnection)
            {
                tempTableName = string.Format("##{0}", tempTableName);
                cmd.CommandText = string.Format("SELECT TOP 1 {0} INTO {1} FROM {2}", string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray()), tempTableName, tableName);
            }
            if (_connection is OracleConnection)
                cmd.CommandText = string.Format("CREATE GLOBAL TEMPORARY TABLE {1}  ON COMMIT PRESERVE ROWS AS SELECT {0} FROM {2}", string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray()), tempTableName, tableName);
            cmd.ExecuteNonQuery();
            cmd.CommandText = string.Format("TRUNCATE TABLE {0}", tempTableName);
            cmd.ExecuteNonQuery();
            return tempTableName;
        }

        public long BulkUpdateDataTable(bool bulkBatch, string tableName, DataTable dataTable, string[] columnNames, IExecutorRuntime executorRuntime = null)
        {
            if (bulkBatch && SupportsBulk())
                return DoBulkUpdate(tableName, dataTable, columnNames, executorRuntime);
            else
                return DoBatchUpdate(tableName, dataTable, columnNames);
        }

        private int DoBatchUpdate(string tableName, DataTable dataTable, string[] columnNames)
        {
            if (_connection == null)
                return -1;
            var sqlCommand = _connection?.CreateCommand();
            if (sqlCommand != null)
            {
                sqlCommand.Connection = _connection;
                sqlCommand.Transaction = _transaction;
                sqlCommand.CommandType = CommandType.Text;
            }
            var dbSchema = _connection?.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
            string markerFormat = (string)dbSchema.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat];
            string markerPattern = (string)dbSchema.Rows[0][DbMetaDataColumnNames.ParameterMarkerPattern];
            if (markerFormat == "{0}" && markerPattern.StartsWith("@"))
                markerFormat = "@" + markerFormat;

            List<DbParameter> updatePar = new List<DbParameter>();
            List<DbParameter> wherePar = new List<DbParameter>();
            var result = SetupBulkUpdateCommand(dataTable, columnNames, markerFormat, sqlCommand, updatePar, wherePar);

            sqlCommand.Parameters.AddRange(updatePar.ToArray());
            sqlCommand.Parameters.AddRange(wherePar.ToArray());

            var updateClause = result.Item1;
            var whereClause = result.Item2;

            sqlCommand.CommandText = string.Format("UPDATE {0} SET {1} WHERE {2}", tableName, updateClause, whereClause);

            int rows = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DbParameter param in sqlCommand.Parameters)
                    param.Value = row[param.SourceColumn];
                rows += sqlCommand.ExecuteNonQuery();
            }
            return rows;
        }

        private int DoBulkUpdate(string tableName, DataTable dataTable, string[] columnNames, IExecutorRuntime executorRuntime)
        {
            if (_connection == null)
                return -1;
            var tblName = string.Empty;
            var sqlCommand = _connection?.CreateCommand();
            if (sqlCommand != null)
            {
                sqlCommand.Connection = _connection;
                sqlCommand.Transaction = _transaction;
                sqlCommand.CommandType = CommandType.Text;
            }
            try
            {
                tblName = CreateTempTableForUpdate(dataTable, tableName, sqlCommand);
                BulkInsertDataTable(tblName, dataTable, executorRuntime);

                sqlCommand.CommandText = string.Format("MERGE INTO {0} t USING (SELECT * FROM {1})s on ({3}) WHEN MATCHED THEN UPDATE SET {2}", tableName, tblName,
                    string.Join(",", dataTable.Columns.Cast<DataColumn>()
                        .Where(x => !columnNames.Contains(x.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                        .Select(x => string.Format("t.{0}=s.{0}", EscapeDbObject(x.ColumnName))).ToArray()),
                    string.Join(" and ", dataTable.Columns.Cast<DataColumn>()
                        .Where(x => columnNames.Contains(x.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                        .Select(x => string.Format("t.{0}=s.{0}", EscapeDbObject(x.ColumnName))).ToArray())
                    );
                if (_connection is SqlConnection)
                    sqlCommand.CommandText += ";";
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tblName))
                {
                    sqlCommand.CommandText = string.Format("TRUNCATE TABLE {0}", tblName);
                    sqlCommand.ExecuteNonQuery();
                    sqlCommand.CommandText = string.Format("DROP TABLE {0}", tblName);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private Tuple<string, string> SetupBulkUpdateCommand(DataTable dataTable, string[] columnNames, string markerFormat, DbCommand updateCommand, List<DbParameter> updatePar, List<DbParameter> wherePar)
        {
            var whereClause = string.Empty;
            var updateClause = string.Empty;

            int index = 1;

            foreach (DataColumn column in dataTable.Columns)
            {
                DbParameter p = BuildParameter(updateCommand, string.Format("p{0}", index++), column);

                string paramName = string.Format(markerFormat, p.ParameterName);
                if (columnNames.Contains(column.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                {
                    whereClause = string.Format("{0} {1}={2} AND ", whereClause, EscapeDbObject(column.ColumnName), paramName);
                    wherePar.Add(p);
                }
                else
                {
                    updateClause = string.Format("{0} {1}={2},", updateClause, EscapeDbObject(column.ColumnName), paramName);
                    updatePar.Add(p);
                }
            }

            updateClause = updateClause.Remove(updateClause.Length - 1, 1);
            whereClause = whereClause.Remove(whereClause.Length - 5, 5);

            return new Tuple<string, string>(updateClause, whereClause);
        }

        private DbParameter BuildParameter(DbCommand updateCommand, string name, DataColumn column)
        {
            var p = updateCommand.CreateParameter();
            p.ParameterName = name;
            p.SourceColumn = column.ColumnName;
            return p;
        }

        private static void LogWarningMessage(IExecutorRuntime executorRuntime)
        {
            var message = new LogMessage
            {
                Message = Resources.BulkInsert_DriverDoesNotSupportBulkInsert,
                EventType = TraceEventType.Warning
            };
            executorRuntime.LogMessage(message);
        }

        public virtual void Commit()
        {
            _transaction?.Commit();
        }

        public virtual void Rollback()
        {
            _transaction?.Rollback();
        }

        public virtual void Dispose()
        {
            _command?.Dispose();
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        private void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        private void SetupCommand(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(Resources.ConnectionNotInitialized);
            }

            _command = _command ?? _connection.CreateCommand();

            var ceilVal = (int)Math.Ceiling((double)commandTimeout / 1000);

            if (ceilVal != 0)
            {
                _command.CommandTimeout = ceilVal;
            }

            _command.CommandType = commandType;
            _command.CommandText = sql;
            _command.Parameters.Clear();
            if (parameters == null)
            {
                return;
            }
            foreach (var param in parameters)
            {
                DbParameter dbParameter = _command.CreateParameter();
                dbParameter.ParameterName = param.Key;
                dbParameter.Direction = WokflowDbParameterToParameterDirection(param.Value.Item2);
                if (dbParameter.Direction.HasFlag(ParameterDirection.InputOutput) || dbParameter.Direction.HasFlag(ParameterDirection.Output))
                {
                    dbParameter.Size = GetParameterSize(dbParameter);
                }

                dbParameter.Value = param.Value.Item1 ?? DBNull.Value;
                _command.Parameters.Add(dbParameter);
            }
        }

        private int GetParameterSize(DbParameter dbParameter)
        {
            if ((_connection.GetType() == typeof(OdbcConnection) && (((OdbcConnection)_connection).Driver.StartsWith(OracleOdbcDriverPattern) || ((OdbcConnection)_connection).Driver.StartsWith(DB2OdbcDriverPattern)))
               || _connection.ToString().ToLower().Contains(OraclePattern))
                return 1000000;
            if (_connection.GetType() == typeof(OdbcConnection))
                return 4000;
            return -1;
        }

        private string GetColumnNames(DataTable table, bool removeBrackets = false)
        {
            if (table.Columns.Count < 1 || table.Rows.Count < 1)
            {
                return string.Empty;
            }

            var columns = new StringBuilder();
            foreach (DataColumn column in table.Columns)
            {
                if (removeBrackets)
                {
                    columns.Append(column.ColumnName + ",");
                }
                else
                {
                    columns.Append(string.Format("{0}{1}", EscapeDbObject(column.ColumnName), ","));
                }
            }
            columns = columns.Remove(columns.Length - 1, 1);

            return columns.ToString();
        }

        private string EscapeDbObject(string dbObject)
        {
            return "\"" + dbObject + "\"";
        }

        private static ParameterDirection WokflowDbParameterToParameterDirection(ArgumentDirection argumentDirection)
        {
            switch (argumentDirection)
            {
                case ArgumentDirection.In:
                    return ParameterDirection.Input;

                case ArgumentDirection.Out:
                    return ParameterDirection.Output;

                default:
                    return ParameterDirection.InputOutput;
            }
        }

        private static ArgumentDirection WokflowParameterDirectionToDbParameter(ParameterDirection parameterDirection)
        {
            switch (parameterDirection)
            {
                case ParameterDirection.Input:
                    return ArgumentDirection.In;

                case ParameterDirection.Output:
                    return ArgumentDirection.Out;

                case ParameterDirection.InputOutput:
                    return ArgumentDirection.InOut;

                default:
                    throw new ArgumentException(Resources.ParameterDirectionArgumentException);
            }
        }
    }
}