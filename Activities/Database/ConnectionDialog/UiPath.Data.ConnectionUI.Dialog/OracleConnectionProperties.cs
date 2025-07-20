using Oracle.ManagedDataAccess.Client;
using UiPath.Database;

namespace UiPath.Data.ConnectionUI.Dialog
{
	public class OracleConnectionProperties : AdoDotNetConnectionProperties
	{
		private OracleConnectionStringBuilder _connStringBuilder;

		public override bool IsComplete
		{
			get
			{
				if (!(_connStringBuilder[DatabaseConstants.Data_Source] is string) ||
					(_connStringBuilder[DatabaseConstants.Data_Source] as string).Length == 0)
				{
					return false;
				}
				if ((!(_connStringBuilder[DatabaseConstants.User_ID] is string) ||
					(_connStringBuilder[DatabaseConstants.User_ID] as string).Length == 0))
				{
					return false;
				}
				return true;
			}
		}

		public OracleConnectionProperties()
			: base(DatabaseConstants.OracleProvider)
		{
			LocalReset();
			_connStringBuilder = ConnectionStringBuilder as OracleConnectionStringBuilder;
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}

		protected override string ToTestString()
		{
			bool savedPooling = (bool)_connStringBuilder[DatabaseConstants.Pooling];
			bool wasDefault = !_connStringBuilder.ShouldSerialize(DatabaseConstants.Pooling);
			_connStringBuilder[DatabaseConstants.Pooling] = false;
			string dataSource = _connStringBuilder[DatabaseConstants.Data_Source] as string;
			string password = _connStringBuilder[DatabaseConstants.Password] as string;
			string userId = _connStringBuilder[DatabaseConstants.User_ID] as string;
			string testString = $"{DatabaseConstants.User_ID}={userId};{DatabaseConstants.Password}={password};{DatabaseConstants.Data_Source}={dataSource};{DatabaseConstants.Pooling}={savedPooling}";
			_connStringBuilder[DatabaseConstants.Pooling] = savedPooling;
			if (wasDefault)
			{
				_connStringBuilder.Remove(DatabaseConstants.Pooling);
			}
			return testString;
		}

        public override string ToFullString()
        {
			return base.ToFullString().Replace("\"", "");
        }

        private void LocalReset()
		{
			// We always start with unicode turned on
			//this["Unicode"] = true;
		}
	}
}
