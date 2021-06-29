//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Data.ConnectionUI
{
	public class OdbcConnectionProperties : AdoDotNetConnectionProperties
	{
		private static List<string> _sqlNativeClientDrivers = null;

		public OdbcConnectionProperties()
			: base("System.Data.Odbc")
		{
		}

		public override bool IsComplete
		{
			get
			{
				if ((!(ConnectionStringBuilder["DSN"] is string) ||
					(ConnectionStringBuilder["DSN"] as string).Length == 0) &&
					(!(ConnectionStringBuilder["DRIVER"] is string) ||
					(ConnectionStringBuilder["DRIVER"] as string).Length == 0))
				{
					return false;
				}
				return true;
			}
		}

		public static List<string> SqlNativeClientDrivers
		{
			get
			{
				if (_sqlNativeClientDrivers == null)
				{
					_sqlNativeClientDrivers = new List<string>();

					List<string> driverDescList = ManagedSQLGetInstalledDrivers();
					Debug.Assert(driverDescList != null, "driver list is null");
					foreach (string driverDesc in driverDescList)
					{
						if (driverDesc.Contains("Native") && driverDesc.Contains("Client"))
						{
							StringBuilder driverBuf = new StringBuilder(1024);
							int len = NativeMethods.SQLGetPrivateProfileString(driverDesc, "Driver", "", driverBuf, driverBuf.Capacity, "ODBCINST.INI");
							if (len > 0 && driverBuf.Length > 0)
							{
								string driver = driverBuf.ToString();
								int start = driver.LastIndexOf('\\');
								if (start > 0)
								{
									_sqlNativeClientDrivers.Add(driver.Substring(start + 1).ToUpperInvariant());
								}
							}
						}
					}

					_sqlNativeClientDrivers.Sort();
				}

				Debug.Assert(_sqlNativeClientDrivers != null, "Native Client list is null");
				return _sqlNativeClientDrivers;
			}
		}

		private static List<string> ManagedSQLGetInstalledDrivers()
		{
			char[] lpszBuf = new char[1024];
			int pcbBufOut = 0;
			bool succeed = true;
			List<string> driverList = new List<string>();

			try
			{
				succeed = NativeMethods.SQLGetInstalledDrivers(lpszBuf, lpszBuf.Length, ref pcbBufOut);

				while (succeed && pcbBufOut > 0 &&
					pcbBufOut == (lpszBuf.Length - 1) &&
					lpszBuf.Length < Math.Pow(2, 30) /* sanity limit */ )
				{
					// The managed buffer needs to be bigger
					lpszBuf = new char[lpszBuf.Length * 2];

					succeed = NativeMethods.SQLGetInstalledDrivers(lpszBuf, lpszBuf.Length, ref pcbBufOut);
				}
			}
			catch (Exception e)
			{
				Debug.Fail(e.ToString());
				succeed = false;
			}

			if (succeed)
			{
				for (int start = 0, end = Array.IndexOf(lpszBuf, '\0', start, (pcbBufOut - 1));
					start < (pcbBufOut - 1);
					start = end + 1, end = Array.IndexOf(lpszBuf, '\0', start, (pcbBufOut - 1) - end))
				{
					driverList.Add(new string(lpszBuf, start, end - start));
				}
			}

			return driverList;
		}
	}
}
