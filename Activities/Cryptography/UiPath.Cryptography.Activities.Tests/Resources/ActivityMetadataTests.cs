using System.Collections.Generic;
using UiPath.Shared.ViewModels.Tests.MetadataChecker;
using Xunit;

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities.Tests.Resources;

[CollectionDefinition(MetadataCheckerConfig.FixtureCollectionName)]
public class ActivitiesMetadataCollection : ICollectionFixture<MetadataFixture> { }

public class MetadataFixture : IMetadataFixture
{
    public MetadataCheckerConfig Config { get; }

    public MetadataFixture()
    {
        // Cryptography has no triggers and no excluded activities.
        var knownTags = new Dictionary<string, string>();
        var excludedActivities = new HashSet<string>();

        Config = new MetadataCheckerConfig(
            baseActivitiesAssembly: typeof(UiPath.Cryptography.CryptographyHelper).Assembly,
            activitiesAssembly: typeof(EncryptFile).Assembly,
            // Cryptography keeps its ViewModels in the same assembly as the activities (under NetCore/ViewModels/).
            viewModelsAssembly: typeof(EncryptFile).Assembly,
            desktopTrigger: null,
            knownTags: knownTags,
            excludedActivities: excludedActivities,
            // Cryptography's ActivitiesMetadata.json doesn't carry resourceManagerName; supply it from the Designer.cs naming.
            resourceManagerName: "UiPath.Cryptography.Activities.Properties.UiPath.Cryptography.Activities");
    }
}

public class ActivityMetadataTests : ActivitiesMetadataChecker<MetadataFixture>
{
    public ActivityMetadataTests(MetadataFixture fixture) : base(fixture) { }
}
