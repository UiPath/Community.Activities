﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UiPath.FTP.Activities.Properties;

namespace UiPath.FTP.Activities
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        public LocalizedCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return Resources.ResourceManager.GetString(value) ?? base.GetLocalizedString(value);
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        public LocalizedDisplayNameAttribute(string displayName)
            : base(displayName)
        {

        }

        public override string DisplayName
        {
            get
            {
                return Resources.ResourceManager.GetString(DisplayNameValue) ?? base.DisplayName;
            }
        }
    }

    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public LocalizedDescriptionAttribute(string displayName)
            : base(displayName)
        {

        }

        public override string Description
        {
            get
            {
                return Resources.ResourceManager.GetString(DescriptionValue) ?? base.Description;
            }
        }
    }
}
