using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;


namespace UiPath.Data.ConnectionUI.Dialog
{
	internal sealed class HelpUtils
	{
		private HelpUtils()
		{
		}

		/// <summary>
		/// This function checks if the OS is 64 bits.
		/// </summary>
		public static bool IsWow64()
		{
			bool isWow64 = false;
			if (Environment.OSVersion.Version.Major >= 5)
			{
				Process curProcess = Process.GetCurrentProcess();
				try
				{
					NativeMethods.IsWow64Process(curProcess.Handle, out isWow64);
				}
				catch (Exception e)
				{
					isWow64 = false;
					Debug.Fail("Failed in calling IsWow64Process: " + e.Message);
				}
			}

			return isWow64;
		}

		/// <summary>
		/// Get ValueNames from registry for WoW64 machine. Corresponding to Microsoft.Win32.RegistryKey.GetValueNames().  
		/// </summary>
		/// <param name="registryKey">Registry key string value</param>
		/// <param name="ulOptions">Access key value options</param>
		/// <returns></returns>
		public static string[] GetValueNamesWow64(string registryKey, int ulOptions)
		{
			UIntPtr hKey = UIntPtr.Zero;
			UIntPtr nameKey = UIntPtr.Zero;
			int lResult = 0;
			string[] valueNames = null;

			try
			{
				lResult = NativeMethods.RegOpenKeyEx(NativeMethods.HKEY_LOCAL_MACHINE, registryKey, 0, ulOptions, out nameKey);
			}
			catch
			{
				// Ignore native exceptions. 
			}
			if (lResult == 0 && Equals(nameKey, UIntPtr.Zero) == false)
			{
				uint numValues = 0;
				try
				{
					lResult = NativeMethods.RegQueryInfoKey(nameKey, null, IntPtr.Zero, IntPtr.Zero, out uint numSubKeys, IntPtr.Zero, IntPtr.Zero, out numValues, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
				catch
				{
					// Ignore native exceptions. 
				}

				if (lResult == 0)
				{
					valueNames = new string[numValues];

					for (uint index = 0; index < numValues; index++)
					{
						StringBuilder builder = new StringBuilder();
						uint size = (uint)short.MaxValue;

						try
						{
							lResult = NativeMethods.RegEnumValue(nameKey, index, builder, ref size, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
						}
						catch
						{
							// Ignore native exceptions. 
						}

						if (lResult == 0)
						{
							valueNames[index] = builder.ToString();
						}
					}
				}
			}
			if (valueNames != null)
			{
				return valueNames;
			}
			else
			{
				return new string[0];
			}
		}
	}
}
