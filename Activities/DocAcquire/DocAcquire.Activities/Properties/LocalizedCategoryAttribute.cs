using DocAcquire.Activities.Properties;
using System;
using System.ComponentModel;

namespace DocAcquire.Activities
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        public LocalizedCategoryAttribute(string category) : base(category) { }

        protected override string GetLocalizedString(string value)
        {
            return Resources.ResourceManager.GetString(value) ?? base.GetLocalizedString(value);
        }
    }
}
