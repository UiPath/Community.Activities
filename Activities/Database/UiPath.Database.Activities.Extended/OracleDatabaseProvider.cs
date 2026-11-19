using System;
using System.Collections.Generic;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using UiPath.Database.BulkOps;
using UiPath.Database.Extensibility;

namespace UiPath.Database.Extended
{
    /// <summary>
    /// The Oracle provider. It lives in the extended package because <c>Oracle.ManagedDataAccess.Core</c> carries its own
    /// managed cryptography, which the base package must not ship.
    /// </summary>
    public sealed class OracleDatabaseProvider : IDatabaseProvider
    {
        private static readonly object InitializeLock = new object();
        private static bool _initialized;

        public string ProviderName => DatabaseConstants.OracleProvider;

        public DbProviderFactory Factory => OracleClientFactory.Instance;

        public bool SupportsBulk => true;

        public void Initialize()
        {
            lock (InitializeLock)
            {
                if (_initialized)
                {
                    return;
                }

                OracleConfiguration.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version8;
                _initialized = true;
            }
        }

        public bool Owns(DbConnection connection) => connection is OracleConnection;

        public IBulkOperations CreateBulkOperations() => new OracleBulkOperations();

        public bool TryUpdateParameterType(DbParameter parameter, Type clrType)
        {
            if (parameter is OracleParameter oracleParameter && clrType != null && OracleMappings.TryGetValue(clrType, out OracleDbType oracleType))
            {
                oracleParameter.OracleDbType = oracleType;
                return true;
            }

            return false;
        }

        public string GetCreateTempTableSql(string tempTableName, string columnList, string sourceTable) =>
            string.Format("CREATE GLOBAL TEMPORARY TABLE {1}  ON COMMIT PRESERVE ROWS AS SELECT {0} FROM {2}", columnList, tempTableName, sourceTable);

        /// <summary>
        /// The mapping of C# Type to OracleDbType or to DbType is not 1-1
        /// There are a lot of conversions behind the scene that are done
        /// Most of the conversions work ok with string type
        /// Here is some reference (it might not be up to date)
        /// https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
        /// https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/oracle-data-type-mappings
        /// For now leave only a few conversions
        /// </summary>
        private static readonly Dictionary<Type, OracleDbType> OracleMappings = new Dictionary<Type, OracleDbType>()
        {
            { typeof(OracleRefCursor), OracleDbType.RefCursor },
            { typeof(bool), OracleDbType.Boolean },
            { typeof(int), OracleDbType.Int32 },
            { typeof(uint), OracleDbType.Int32 },
            { typeof(short), OracleDbType.Int16 },
            { typeof(ushort), OracleDbType.Int16 },
            { typeof(long), OracleDbType.Int64 },
            { typeof(ulong), OracleDbType.Int64 },
            { typeof(byte), OracleDbType.Byte },
            { typeof(sbyte), OracleDbType.Byte },
            { typeof(float), OracleDbType.Single },
            { typeof(double), OracleDbType.Double },
            { typeof(decimal), OracleDbType.Decimal }
        };
    }
}
