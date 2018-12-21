using DocAcquire.Activities.Properties;
using System;
using System.ComponentModel;

namespace DocAcquire.Activities
{

    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        public LocalizedDisplayNameAttribute(string displayName) : base(displayName) { }

        public override string DisplayName
        {
            get
            {
                return Resources.ResourceManager.GetString(DisplayNameValue) ?? base.DisplayName;
            }
        }
    }
}
