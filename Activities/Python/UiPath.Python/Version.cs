using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UiPath.Python.Properties;

namespace UiPath.Python
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    internal sealed class VersionAttribute : Attribute
    {
        public int Major { get; set; }

        public int Minor { get; set; }

        public string AssemblyName { get; set; }

        public VersionAttribute(int major, int minor, string assemblyName)
        {
            Major = major;
            Minor = minor;
            AssemblyName = assemblyName;
        }
    }

    public enum Version
    {
        //Unknown = -1,

        Auto,

        [Version(2, 7, "Python.Runtime.27.dll")]
        Python_27,

        [Version(3, 3, "Python.Runtime.33.dll")]
        Python_33,

        [Version(3, 4, "Python.Runtime.34.dll")]
        Python_34,

        [Version(3, 5, "Python.Runtime.35.dll")]
        Python_35,

        [Version(3, 6, "Python.Runtime.36.dll")]
        Python_36,

        [Version(3, 7, "Python.Runtime.37.dll")]
        Python_37,

        [Version(3, 8, "Python.Runtime.38.dll")]
        Python_38,
    }

    /// <summary>
    /// Helper class for Python version operations
    /// </summary>
    internal static class VersionExtensions
    {
        internal static string GetAssemblyName(this Version version)
        {
            Debug.Assert(version.IsValid());
            Type t = typeof(Version);
            FieldInfo fi = t.GetField(version.ToString());
            VersionAttribute attr = (VersionAttribute)Attribute.GetCustomAttribute(t.GetField(version.ToString()), typeof(VersionAttribute));
            return attr.AssemblyName;
        }

        internal static Version GetVersionFromStr(this string fileVersion)
        {
            var regex = new Regex(@"(?<=Python )[0-9]+(\.[0-9]+)*");
            var nrVer = regex.Matches(fileVersion);
            if (nrVer.Count == 1)
            {
                var versionDetails = nrVer[0].Value.Split(new char[] { '.' });
                var version = GetPythonVersion(Int32.Parse(versionDetails[0]), Int32.Parse(versionDetails[1]));
                if (!version.IsValid())
                    throw new ArgumentException(string.Format(Resources.UnsupportedVersionException, nrVer[0].Value, string.Join(", ", Enum.GetNames(typeof(Version)).Where(x => x != Version.Auto.ToString()))));
                return version;
            }
            else 
                return Version.Auto;

        }
        internal static Version Get(this System.Version version)
        {
            return GetPythonVersion(version.Major, version.Minor);
        }

        internal static bool IsValid(this Version version)
        {
            return version > Version.Auto;
        }

        private static Version GetPythonVersion(int major, int minor)
        {
            Type t = typeof(Version);
            foreach (Version version in Enum.GetValues(t))
            {
                FieldInfo fi = t.GetField(version.ToString());
                VersionAttribute attr = (VersionAttribute)Attribute.GetCustomAttribute(t.GetField(version.ToString()), typeof(VersionAttribute));
                if (attr?.Major == major && attr?.Minor == minor)
                    return version;
            }
            return Version.Auto;
        }
    }
}
