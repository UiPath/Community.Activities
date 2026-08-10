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
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this check box is selected, the subfolders are also included in the enumeration of the files on the FTP server.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// Configure what type of objects to filter
        /// </summary>
        public DesignProperty<FtpFilterObjectType> Filter { get; set; } = new DesignProperty<FtpFilterObjectType>();

        /// <summary>
        /// A collection of files that have been found on the FTP server.
        /// </summary>
        public DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>> Files { get; set; } = new DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>>();

        private static DataSource<FtpFilterObjectType> _filterObjectDataSource;

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            RemotePath.DisplayName = Resources.Activity_EnumerateObjects_Property_RemotePath_Name;
            RemotePath.Tooltip = Resources.Activity_EnumerateObjects_Property_RemotePath_Description;
            RemotePath.EditPlaceholder = Resources.Activity_EnumerateObjects_Property_RemotePath_Placeholder;
            RemotePath.IsRequired = true;
            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = orderIndex++;
            RemotePath.Category = Resources.Input;

            Recursive.DisplayName = Resources.Activity_EnumerateObjects_Property_Recursive_Name;
            Recursive.Tooltip = Resources.Activity_EnumerateObjects_Property_Recursive_Description;
            Recursive.IsPrincipal = false;
            Recursive.OrderIndex = orderIndex++;
            Recursive.Category = Resources.Options;
            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Filter.DisplayName = Resources.Activity_EnumerateObjects_Property_Filter_Name;
            Filter.Tooltip = Resources.Activity_EnumerateObjects_Property_Filter_Description;
            Filter.EditPlaceholder = Resources.Activity_EnumerateObjects_Property_Filter_Placeholder;
            Filter.IsPrincipal = false;
            Filter.OrderIndex = orderIndex++;
            Filter.Category = Resources.Options;
            Filter.DataSource = _filterObjectDataSource;
            Filter.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };

            ConfigureContinueOnError(ref orderIndex);

            // the output closes the property list, after the Options section
            Files.DisplayName = Resources.Activity_EnumerateObjects_Property_Files_Name;
            Files.Tooltip = Resources.Activity_EnumerateObjects_Property_Files_Description;
            Files.IsPrincipal = false;
            Files.OrderIndex = orderIndex;
            Files.Category = Resources.Output;
        }

        private static void InitializeFilterObjectsDataSource()
        {
            if (_filterObjectDataSource is not null)
            {
                return;
            }

            var objectTypes = Enum.GetValues<FtpFilterObjectType>()
                .Where(s => s != FtpFilterObjectType.None)
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
                .WithData(objectTypes)
                .Build();
        }

        private static List<FtpFilterObjectType> Decompose(FtpFilterObjectType value)
        {
            var bits = EnumExtensions<FtpFilterObjectType>.Decompose(value);
            bits.Remove(FtpFilterObjectType.None);
            return bits;
        }

        // gets the value of a localized enum and appends Obsolete if necessary
        private static string GetFtpFilterObjectTypeLabel(FtpFilterObjectType value)
        {
            List<string> labels = new();
            foreach (var flag in Decompose(value))
            {
                labels.Add(GetFilterObjectTypeFlagLabel(flag));
            }

            return string.Join(" | ", labels);
        }

        /// <summary>
        /// Returns the localized name of an enum flag (i.e. a single enum value, not a combination).
        ///  Localized names of obsolete values are marked appropriately.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>The localized name</returns>
        private static string GetFilterObjectTypeFlagLabel(FtpFilterObjectType flag)
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
