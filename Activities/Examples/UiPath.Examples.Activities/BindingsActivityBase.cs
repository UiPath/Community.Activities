using System.Activities;

namespace UiPath.Examples.Activities
{
    /// <summary>
    /// Bindings are used by activities to expose resources needed for execution.
    /// These resources can be defined at design time (saved in the workflow) or overriden before runtime (in Assistant or Orchestrator).
    /// </summary>
    public abstract class BindingsActivityBase<T> : CodeActivity<T>
    {
        /// <summary>
        /// The Type of the resource (ex: Connection, Asset, Queue, etc)
        /// Based on the type a specific overriding experience will be provided in Assistant and Orchestrator
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The resources are seen as a dictionary of key-value pairs. 
        /// The key is used to identify the resource at runtime.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Used to generate the key for the runtime bindings dictionary.
        /// By convention, the key is composed of the Type and the Key properties.
        /// </summary>
        /// <returns>The resource identifier required to retrieve the overriden value</returns>
        internal string GetRuntimeBindingsKey()
        {
            return $"{Type}.{Key}";
        }
    }
}
