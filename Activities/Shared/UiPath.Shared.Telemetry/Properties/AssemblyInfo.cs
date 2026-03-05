using System.Runtime.CompilerServices;

// Allow internal types to be accessed by activity assemblies for telemetry
[assembly: InternalsVisibleTo("UiPath.Database.Activities")]
[assembly: InternalsVisibleTo("UiPath.Java.Activities")]
[assembly: InternalsVisibleTo("UiPath.FTP.Activities")]
[assembly: InternalsVisibleTo("UiPath.Cryptography.Activities")]
[assembly: InternalsVisibleTo("UiPath.Python.Activities")]
[assembly: InternalsVisibleTo("UiPath.Credentials.Activities")]