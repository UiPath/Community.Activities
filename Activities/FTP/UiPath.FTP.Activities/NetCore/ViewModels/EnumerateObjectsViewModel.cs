using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Linq;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class EnumerateObjectsViewModel : BaseFtpViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public EnumerateObjectsViewModel(IDesignServices services) : base(services)
        {
            InitializeFilterObjectsDataSource();
        }

        /// <summary>
        /// The path of the directory on the FTP server whose files are to be enumerated.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// If this check box is selected, the subfolders are also included in the enumeration of the files on the FTP server.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; }

        /// <summary>
        /// Configure what type of objects to filter 
        /// </summary>
        public DesignProperty<FtpFilterObjectType> Filter { get; set; }

        /// <summary>
        /// A collection of files that have been found on the FTP server.
        /// </summary>
        public DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>> Files { get; set; }

        private static DataSource<FtpFilterObjectType> _filterObjectDataSource;

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.OrderIndex = propertyOrderIndex++;
            Files.OrderIndex = propertyOrderIndex++;
            Recursive.OrderIndex = propertyOrderIndex++;
            Filter.OrderIndex = propertyOrderIndex;

            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            Filter.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };

            Filter.DataSource = _filterObjectDataSource;
        }

        private static void InitializeFilterObjectsDataSource()
        {
            if (_filterObjectDataSource is not null)
            {
                return;
            }

            var protocols = Enum.GetValues<FtpFilterObjectType>()
                .Where(s => s != FtpFilterObjectType.None && s != FtpFilterObjectType.All)
                .OrderBy(s => s)
                .ToList();

            _filterObjectDataSource = DataSourceBuilder<FtpFilterObjectType>
                .WithId(s => Enum.GetName(s))
                .WithLabel(s => GetFtpFilterObjectTypeLabel(s))
                .WithMultipleSelection<FtpFilterObjectType>(
                    selectionToValue: s =>
                    {
                        if (s.Count == 0)
                        {
                            return FtpFilterObjectType.None;
                        }

                        return s.Aggregate((a, b) => a | b);
                    },
                    valueToSelection: s => Decompose(s).ToArray()
                    )
                .WithData(protocols)
                .Build();
        }

        private static List<FtpFilterObjectType> Decompose(FtpFilterObjectType value)
        {
            var bits = EnumExtensions<FtpFilterObjectType>.Decompose(value);
            return bits;
        }

        // gets the value of a localized enum and appends Obsolete if necessary
        private static string GetFtpFilterObjectTypeLabel(FtpFilterObjectType value)
        {
            List<string> labels = new();
            foreach (var flag in Decompose(value))
            {
                labels.Add(GetFilteObjectTypeFlagLabel(flag));
            }

            return string.Join(" | ", labels);
        }

        /// <summary>
        /// Returns the localized name of an enum flag (i.e. a single enum value, not a combination).
        ///  Localized names of obsolete values are marked appropriately.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>The localized name</returns>
        private static string GetFilteObjectTypeFlagLabel(FtpFilterObjectType flag)
        {
            var localizedName = LocalizedEnum.GetLocalizedValue(typeof(FtpFilterObjectType), flag).Name;

            // get the attributes of the enum value
            var field = typeof(FtpFilterObjectType).GetField(flag.ToString());
            if (field.GetCustomAttributesData().Any(a => a.AttributeType == typeof(ObsoleteAttribute)))
            {
                return string.Format(Resources.ObsoleteEnumValue, localizedName);
            }

            return localizedName;
        }
    }
}
