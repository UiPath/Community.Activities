using System.Windows;
using UiPath.Data.ConnectionUI.Dialog;
using UiPath.Database.Extended.Design;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// The connection dialog offers Oracle when this assembly is installed (see ConnectionDialogProviders).
[assembly: ConnectionDialogProvider(typeof(OracleConnectionDialogProvider))]
