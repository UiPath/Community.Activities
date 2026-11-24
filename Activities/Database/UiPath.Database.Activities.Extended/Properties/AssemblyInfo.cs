using UiPath.Database.Extended;
using UiPath.Database.Extensibility;

// The base package (UiPath.Database.Core.Activities) finds this provider when this assembly is installed
// (see DatabaseProviders).
[assembly: DatabaseProvider(typeof(OracleDatabaseProvider))]
