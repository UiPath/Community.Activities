using System.Data;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities.NetCore
{
    public static class CommandTypeExtensionMethods
    {
        public static string GetLabel(this CommandType commandType)
        {
            switch (commandType)
            {
                case System.Data.CommandType.Text:
                    return Resources.CommandType_Text_FriendlyName;
                case System.Data.CommandType.TableDirect:
                    return Resources.CommandType_TableDirect_FriendlyName;
                case System.Data.CommandType.StoredProcedure:
                    return Resources.CommandType_StoredProcedure_FriendlyName;
                default:
                    return string.Empty;
            }
        }
    }
}
