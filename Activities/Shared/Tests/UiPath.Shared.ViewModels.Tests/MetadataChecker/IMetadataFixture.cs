// Vendored from c:\Work\Activities3\Shared\Tests\UiPath.Shared.ViewModels.Tests\MetadataChecker\ on 2026-05-15. Keep in sync with upstream.
namespace UiPath.Shared.ViewModels.Tests.MetadataChecker
{
    //Collection definition should be defined in the test project
    //Something similar to this
    //[CollectionDefinition(MetadataCheckerConfig.FixtureCollectionName)]
    //public class ActivitiesMetadataCollection
    //: ICollectionFixture<MetadataFixture>
    //{ }

    public interface IMetadataFixture
    {
        public MetadataCheckerConfig Config { get; }
    }
}
