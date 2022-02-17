using System;
using System.Activities.DesignViewModels;
using UiPath.Shared;

namespace UiPath.Cryptography.Activities.NetCore
{
    /// <summary>
    /// Helper for accessing localized resources from UiPath.Cryptography.Properties in ViewModels
    /// </summary>
    internal static class DataSourceHelper
    {
        public static DataSource<T> ForEnum<T>(params T[] data) where T : Enum
        {
            return DataSourceBuilder<T>
                .WithId(e => Enum.GetName(typeof(T), e))
                .WithLabel(e => LocalizedEnum.GetLocalizedValue(typeof(T), e).Name)
                .WithData(data)
                .Build();
        }
    }
}
