using System;
using System.Activities;

namespace UiPath.Database
{
    /// <summary>
    /// Information about a parameter that will bind to a query
    /// </summary>
    public class ParameterInfo
    {
        public object Value { get; set; }

        public Type Type { get; set; }

        public ArgumentDirection Direction { get; set; }
    }
}
