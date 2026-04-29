using System;
using UiPath.Python;

namespace UiPath.Python.Activities.API.Models
{
    /// <summary>
    /// Options for configuring a Python scope.
    /// </summary>
    public class PythonScopeOptions
    {
        /// <summary>
        /// Path to the Python installation directory.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Path to additional Python libraries.
        /// </summary>
        public string LibraryPath { get; set; }

        /// <summary>
        /// The Python version to use.
        /// Default is <see cref="Version.Auto"/>.
        /// </summary>
        public Version Version { get; set; } = Version.Auto;

        /// <summary>
        /// The working folder for the Python process.
        /// </summary>
        public string WorkingFolder { get; set; }

        /// <summary>
        /// Maximum time to wait for Python operations to complete.
        /// When <c>null</c>, defaults to 1 hour.
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }
    }
}
