//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Data.ConnectionUI
{
	internal sealed class HelpUtils
	{
		private const int KeyValueNameLength = 1024; // 1024 should be enough for registry key value name. 

		private HelpUtils()
		{
		}

		public static bool IsContextHelpMessage(ref Message m)
		{
			return (m.Msg == NativeMethods.WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == NativeMethods.SC_CONTEXTHELP);
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
						StringBuilder builder = new StringBuilder(KeyValueNameLength);
						uint size = KeyValueNameLength;

						try
						{
							lResult = NativeMethods.RegEnumValue(nameKey, index, builder, ref  size, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
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

		public static void TranslateContextHelpMessage(Form f, ref Message m)
		{
			Debug.Assert(f != null);

			Control activeControl = GetActiveControl(f);
			if (activeControl != null)
			{
				// Turn this message into a WM_HELP message
				m.HWnd = activeControl.Handle;
				m.Msg = NativeMethods.WM_HELP;
				m.WParam = IntPtr.Zero;
                NativeMethods.HELPINFO helpInfo = new NativeMethods.HELPINFO
                {
                    iContextType = NativeMethods.HELPINFO_WINDOW,
                    iCtrlId = f.Handle.ToInt32(),
                    hItemHandle = activeControl.Handle,
                    dwContextId = 0
                };
                helpInfo.MousePos.x = NativeMethods.LOWORD((int)m.LParam);
				helpInfo.MousePos.y = NativeMethods.HIWORD((int)m.LParam);
				m.LParam = Marshal.AllocHGlobal(Marshal.SizeOf(helpInfo));
				Marshal.StructureToPtr(helpInfo, m.LParam, false);
			}
		}

		public static Control GetActiveControl(Form f)
		{
			Control activeControl = f;
			ContainerControl containerControl = null;
			while ((containerControl = activeControl as ContainerControl) != null &&
				containerControl.ActiveControl != null)
			{
				activeControl = containerControl.ActiveControl;
			}
			return activeControl;
		}
	}
}
