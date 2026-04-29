using System;

namespace UiPath.Java.Activities.API.Models
{
    /// <summary>
    /// Options for configuring a Java scope.
    /// </summary>
    public class JavaScopeOptions
    {
        /// <summary>
        /// Path to the Java installation directory (the folder containing the <c>bin</c> subfolder).
        /// When <c>null</c> or empty, the Java executable on the system PATH is used.
        /// </summary>
        public string JavaPath { get; set; }

        /// <summary>
        /// Maximum time to wait for the Java service to start.
        /// Default is 15 seconds.
        /// </summary>
        public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(15);
    }
}
