using Oracle.ManagedDataAccess.Client;
namespace UiPath.Data.ConnectionUI.Dialog
{

	public class OracleConnectionProperties : AdoDotNetConnectionProperties
	{
		private OracleConnectionStringBuilder _connStringBuilder;

		public override bool IsComplete
		{
			get
			{
				if (!(_connStringBuilder["Data Source"] is string) ||
					(_connStringBuilder["Data Source"] as string).Length == 0)
				{
					return false;
				}
				if (!(bool)_connStringBuilder["Integrated Security"] &&
					(!(_connStringBuilder["User ID"] is string) ||
					(_connStringBuilder["User ID"] as string).Length == 0))
				{
					return false;
				}
				return true;
			}
		}

		public OracleConnectionProperties()
			: base("Oracle.ManagedDataAccess.Client")
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
			bool savedPooling = (bool)_connStringBuilder["Pooling"];
			bool wasDefault = !_connStringBuilder.ShouldSerialize("Pooling");
			_connStringBuilder["Pooling"] = false;
			string dataSource = _connStringBuilder["Data Source"] as string;
			string password = _connStringBuilder["Password"] as string;
			string userId = _connStringBuilder["User Id"] as string;
			string testString = $"User Id={userId};Password={password};Data Source={dataSource};POOLING={savedPooling}";
			_connStringBuilder["Pooling"] = savedPooling;
			if (wasDefault)
			{
				_connStringBuilder.Remove("Pooling");
			}
			return testString;
		}

		private void LocalReset()
		{
			// We always start with unicode turned on
			//this["Unicode"] = true;
		}
	}
}
