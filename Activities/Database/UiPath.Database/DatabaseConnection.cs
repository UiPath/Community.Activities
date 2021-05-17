using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UiPath.Database.BulkOps;
using UiPath.Database.Properties;
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
        private const string OraclePattern = "oracle";
        private const string SQLProvider = "system.data.sqlclient";
        private const string OracleProvider = "oracle.manageddataaccess.client";

        public DatabaseConnection Initialize(DbConnection connection)
        {
            _connection = connection;
            OpenConnection();
            return this;
        }

        public DatabaseConnection Initialize(string connectionString, string providerName)
        {
            _providerName = providerName;
            _connection = DbProviderFactories.GetFactory(providerName).CreateConnection();
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
            DbDataAdapter dbDA = DbProviderFactories.GetFactory(_providerName).CreateDataAdapter();
            DbCommandBuilder cmdb = DbProviderFactories.GetFactory(_providerName).CreateCommandBuilder();
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

        public long BulkInsertDataTable(string tableName, DataTable dataTable, string connection, IExecutorRuntime executorRuntime = null)
        {
            DbDataAdapter dbDA = DbProviderFactories.GetFactory(_providerName).CreateDataAdapter();
            DbCommandBuilder cmdb = DbProviderFactories.GetFactory(_providerName).CreateCommandBuilder();
            cmdb.DataAdapter = dbDA;
            dbDA.ContinueUpdateOnError = false;
            IBulkOperations bulkOps = BulkOperationsFactory.Create(_providerName);
            DbCommand commandRowCount = _connection.CreateCommand();
            DbCommand commandTableStructure = _connection.CreateCommand();
            DoBulkInsert(_providerName, tableName, dataTable, connection, executorRuntime, dbDA, bulkOps, commandRowCount, commandTableStructure, out long affectedRecords);

            return affectedRecords;
        }

        private string CreateTempTableForUpdate(DataTable dataTable, string tableName, DbCommand cmd)
        {
            var tempTableName = string.Format("a{0}",DateTime.Now.Ticks);
            if (_providerName.ToLower() == SQLProvider)
            {
                tempTableName = string.Format("##{0}", tempTableName);
                cmd.CommandText = string.Format("SELECT TOP 1 {0} INTO {1} FROM {2}", string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray()), tempTableName, tableName);
            }
            if (_providerName.ToLower() == OracleProvider)
                cmd.CommandText = string.Format("CREATE GLOBAL TEMPORARY TABLE {1}  ON COMMIT PRESERVE ROWS AS SELECT {0} FROM {2}", string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray()), tempTableName, tableName);
            cmd.ExecuteNonQuery();
            cmd.CommandText = string.Format("TRUNCATE TABLE {0}", tempTableName);
            cmd.ExecuteNonQuery();
            return tempTableName;

        }
        public long BulkUpdateDataTable(bool bulkBatch, string tableName, DataTable dataTable, string[] columnNames, string connection, IExecutorRuntime executorRuntime = null)
        {
            long nrRows = 0;
            var sqlCommand = _connection.CreateCommand();
            sqlCommand.Connection = _connection;
            sqlCommand.Transaction = _transaction;
            sqlCommand.CommandType = CommandType.Text;
            var tblName = string.Empty;
            var dbSchema = _connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);

            if (bulkBatch && (_providerName.ToLower() == OracleProvider || _providerName.ToLower() == SQLProvider))
            {
                try
                {
                    tblName = CreateTempTableForUpdate(dataTable, tableName, sqlCommand);
                    BulkInsertDataTable(tblName, dataTable, connection, executorRuntime);

                    sqlCommand.CommandText = string.Format("MERGE INTO {0} t USING (SELECT * FROM {1})s on ({3}) WHEN MATCHED THEN UPDATE SET {2}", tableName, tblName,
                        string.Join(",", dataTable.Columns.Cast<DataColumn>()
                            .Where(x => !columnNames.Contains(x.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                            .Select(x => string.Format("t.{0}=s.{0}", EscapeDbObject(x.ColumnName))).ToArray()),
                        string.Join(" and ", dataTable.Columns.Cast<DataColumn>()
                            .Where(x => columnNames.Contains(x.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                            .Select(x => string.Format("t.{0}=s.{0}", EscapeDbObject(x.ColumnName))).ToArray())
                        );
                    if (_providerName.ToLower() == SQLProvider)
                        sqlCommand.CommandText += ";";
                    nrRows = sqlCommand.ExecuteNonQuery();
                }catch(Exception)
                {
                    throw;
                }finally
                {
                    if (!string.IsNullOrEmpty(tblName))
                    {
                        sqlCommand.CommandText = string.Format("TRUNCATE TABLE {0}", tblName);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.CommandText = string.Format("DROP TABLE {0}", tblName);
                        sqlCommand.ExecuteNonQuery();
                    }
                    
                }
                return nrRows;
            }
            else
            {
                string markerFormat = (string)dbSchema.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat];
                string markerPattern = (string)dbSchema.Rows[0][DbMetaDataColumnNames.ParameterMarkerPattern];
                if (markerFormat == "{0}" && markerPattern.StartsWith("@"))
                    markerFormat = "@" + markerFormat;

                List<DbParameter> updatePar = new List<DbParameter>();
                List<DbParameter> wherePar = new List<DbParameter>();
                var result = SetupBulkUpdateCommand(tableName, dataTable, columnNames, markerFormat, _connection, _transaction, sqlCommand, updatePar, wherePar);
                

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
        }
        public Tuple<string, string> SetupBulkUpdateCommand(string tableName, DataTable dataTable, string[] columnNames, string markerFormat, DbConnection dbConnection, DbTransaction dbTransaction, DbCommand updateCommand, List<DbParameter> updatePar, List<DbParameter> wherePar)
        {
            var whereClause = string.Empty;
            var updateClause = string.Empty;

            int index = 1;

            foreach (DataColumn column in dataTable.Columns)
            {
                DbParameter p = updateCommand.CreateParameter();
                p.ParameterName = string.Format("p{0}", index++);
                p.SourceColumn = column.ColumnName;

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

        public virtual DbParameter BuildParameter(DbCommand updateCommand, string name, DataColumn column)
        {
            var p = updateCommand.CreateParameter();
            p.ParameterName = name;
            p.SourceColumn = column.ColumnName;
            return p;
        }


        public void DoBulkInsert(string providerName, string tableName, DataTable dataTable, string connection, IExecutorRuntime executorRuntime, DbDataAdapter dbDA, IBulkOperations bulkOps, DbCommand commandRowCount, DbCommand commandTableStructure, out long affectedRecords)
        {
            if (providerName.ToLower() == SQLProvider)
            {
                bulkOps.Connection = connection;
                bulkOps.TableName = tableName;
                var structureQuery = string.Format("select TOP(1) * from {0}", tableName);

                ValidateDatabaseTableStructure(tableName, commandTableStructure, dataTable, dbDA, structureQuery);
                var countQuery = string.Format("SELECT COUNT(*) FROM {0}", tableName);
                var countStart = CountRowsInTable(commandRowCount, countQuery);
                bulkOps.WriteToServer(dataTable);
                var countEnd = CountRowsInTable(commandRowCount, countQuery);
                affectedRecords = countEnd - countStart;
            }
            else if (providerName.ToLower() == OracleProvider)
            {
                bulkOps.Connection = connection;
                bulkOps.TableName = tableName;
                var structureQuery = string.Format("select * from {0} where rownum = 1", tableName);
                ValidateDatabaseTableStructure(tableName, commandTableStructure, dataTable, dbDA, structureQuery);
                var countQuery = string.Format("SELECT COUNT(*) FROM {0}", tableName);
                var countStart = CountRowsInTable(commandRowCount, countQuery);
                List<string> oracleList = FindAssembliesInGAC("oracle.manageddataaccess*");
                BulkInsertOracleManaged(bulkOps, tableName, oracleList, dataTable, executorRuntime);
                var countEnd = CountRowsInTable(commandRowCount, countQuery);
                affectedRecords = countEnd - countStart;
            }
            else
            {
                //if no bulk insert possible, fallback to insert data table with warning message

                if (executorRuntime != null)
                {
                    LogWarningMessage(executorRuntime);
                }
                affectedRecords = InsertDataTable(tableName, dataTable);
            }
        }

        private void BulkInsertOracleManaged(IBulkOperations bulkOps, string tableName, List<string> oracleList, DataTable dataTable, IExecutorRuntime executorRuntime)
        {
            bool bulkCopyIsPresent = false;
            if (oracleList.Count > 0)
            {
                //try all drivers with latest version first
                foreach (var item in oracleList.OrderByDescending(x => x))
                {
                    var oracle = Assembly.LoadFile(item);
                    var bulkCopyType = oracle.GetType("Oracle.ManagedDataAccess.Client.OracleBulkCopy");

                    if (bulkCopyType != null)
                    {
                        bulkOps.BulkCopyType = bulkCopyType;
                        bulkOps.WriteToServer(dataTable);
                        bulkCopyIsPresent = true;
                    }
                    else continue;
                }
            }
            if (!bulkCopyIsPresent)
            {
                //if no bulk insert possible, fallback to insert data table
                if (executorRuntime != null)
                {
                    LogWarningMessage(executorRuntime);
                }
                InsertDataTable(tableName, dataTable);
            }
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

        private static List<string> FindAssembliesInGAC(string name)
        {
            List<string> gacFolders = new List<string>() {
                    "GAC", "GAC_32", "GAC_64", "GAC_MSIL",
                    "NativeImages_v2.0.50727_32",
                    "NativeImages_v2.0.50727_64",
                    "NativeImages_v4.0.30319_32",
                    "NativeImages_v4.0.30319_64"
                };
            List<string> oracleList = new List<string>();
            foreach (string folder in gacFolders)
            {
                string path = Path.Combine(
                   Environment.ExpandEnvironmentVariables(@"%systemroot%\assembly"),
                   folder);

                if (Directory.Exists(path))
                {
                    string[] assemblyFolders = Directory.GetDirectories(path);
                    foreach (string assemblyFolder in assemblyFolders)
                    {
                        string[] files = Directory.GetFiles(assemblyFolder, name, SearchOption.AllDirectories);
                        oracleList.AddRange(files);
                    }
                }

                path = Path.Combine(
                   Environment.ExpandEnvironmentVariables(@"%systemroot%\Microsoft.NET\assembly"),
                   folder);

                if (Directory.Exists(path))
                {
                    string[] assemblyFolders = Directory.GetDirectories(path);
                    foreach (string assemblyFolder in assemblyFolders)
                    {
                        string[] files = Directory.GetFiles(assemblyFolder, "oracle.manageddataaccess*", SearchOption.AllDirectories);
                        oracleList.AddRange(files);
                    }
                }
            }

            return oracleList;
        }

        private long CountRowsInTable(DbCommand commandRowCount, string countQuery)
        {
            // Perform an initial count on the destination table.

            commandRowCount.Connection = _connection;
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

        private void ValidateDatabaseTableStructure(string tableName, DbCommand dbCommand, DataTable dataTable, DbDataAdapter dbDA, string command)
        {
            dbDA.SelectCommand = dbCommand;
            dbDA.SelectCommand.Transaction = _transaction;
            dbDA.SelectCommand.CommandType = CommandType.Text;
            dbDA.SelectCommand.CommandText = command;

            var ds = new DataSet();
            dbDA.Fill(ds);
            ValidateTableStructure(tableName, dataTable, ds);
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
            if ((_connection.GetType() == typeof(OdbcConnection) && ((OdbcConnection)_connection).Driver.StartsWith(OracleOdbcDriverPattern))
               || _connection.ToString().ToLower().Contains(OraclePattern))
                return 1000000;
            if (_connection.GetType() == typeof(OdbcConnection) && ((OdbcConnection)_connection).Driver.StartsWith(SqlOdbcDriverPattern))
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
                    columns.Append(string.Format("{0}{1}",EscapeDbObject(column.ColumnName),","));
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