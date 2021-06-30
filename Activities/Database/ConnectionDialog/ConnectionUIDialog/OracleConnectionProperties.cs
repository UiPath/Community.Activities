//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Data.ConnectionUI
{
	public class OracleConnectionProperties : AdoDotNetConnectionProperties
	{
		public OracleConnectionProperties()
			: base("System.Data.OracleClient")
		{
			LocalReset();
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}

		public override bool IsComplete
		{
			get
			{
				if (!(ConnectionStringBuilder["Data Source"] is string) ||
					(ConnectionStringBuilder["Data Source"] as string).Length == 0)
				{
					return false;
				}
				if (!(bool)ConnectionStringBuilder["Integrated Security"] &&
					(!(ConnectionStringBuilder["User ID"] is string) ||
					(ConnectionStringBuilder["User ID"] as string).Length == 0))
				{
					return false;
				}
				return true;
			}
		}

		protected override string ToTestString()
		{
			bool savedPooling = (bool)ConnectionStringBuilder["Pooling"];
			bool wasDefault = !ConnectionStringBuilder.ShouldSerialize("Pooling");
			ConnectionStringBuilder["Pooling"] = false;
			string testString = ConnectionStringBuilder.ConnectionString;
			ConnectionStringBuilder["Pooling"] = savedPooling;
			if (wasDefault)
			{
				ConnectionStringBuilder.Remove("Pooling");
			}
			return testString;
		}

		private void LocalReset()
		{
			// We always start with unicode turned on
			this["Unicode"] = true;
		}

	}
}
