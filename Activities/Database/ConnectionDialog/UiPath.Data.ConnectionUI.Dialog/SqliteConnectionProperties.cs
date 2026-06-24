using Microsoft.Data.Sqlite;
using UiPath.Database;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class SqliteConnectionProperties : AdoDotNetConnectionProperties
    {
        private readonly SqliteConnectionStringBuilder _connStringBuilder;

        public override bool IsComplete
        {
            get
            {
                return _connStringBuilder.DataSource is string dataSource
                    && dataSource.Length > 0;
            }
        }

        public SqliteConnectionProperties()
            : base(DatabaseConstants.SQLiteProvider)
        {
            _connStringBuilder = (SqliteConnectionStringBuilder)ConnectionStringBuilder;
        }

        protected override string ToTestString()
        {
            return _connStringBuilder.ConnectionString;
        }
    }
}
