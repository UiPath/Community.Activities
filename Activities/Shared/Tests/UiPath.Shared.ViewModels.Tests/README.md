# UiPath.Shared.ViewModels.Tests

Shared `MetadataChecker` harness for Community.Activities packs. Asserts that each pack's `ActivitiesMetadata.json`, activity classes, ViewModel classes, resource strings, and SVG icons are consistent and resolvable at runtime.

**Upstream source.** Vendored from `c:\Work\Activities3\Shared\Tests\UiPath.Shared.ViewModels.Tests\` on 2026-05-15. Keep the `MetadataChecker/*.cs` files in sync with upstream; do not local-fork.

## Consuming this Shared Project from a pack's test csproj

1. Add the import:

   ```xml
   <Import Project="..\..\Shared\Tests\UiPath.Shared.ViewModels.Tests\UiPath.Shared.ViewModels.Tests.projitems" Label="Shared" />
   ```

2. Add `Newtonsoft.Json` as a `<PackageReference>` if the pack doesn't already pull it transitively. `xunit`, `Moq`, and `Shouldly` are auto-included for test projects via `Directory.build.targets`.

3. Create a fixture + a collection definition + a one-line test class deriving from `ActivitiesMetadataChecker<TFixture>`:

   ```csharp
   [CollectionDefinition(MetadataCheckerConfig.FixtureCollectionName)]
   public class ActivitiesMetadataCollection : ICollectionFixture<MetadataFixture> { }

   public class MetadataFixture : IMetadataFixture
   {
       public MetadataCheckerConfig Config { get; }
       public MetadataFixture()
       {
           Config = new MetadataCheckerConfig(
               baseActivitiesAssembly: typeof(YourHelperType).Assembly,
               activitiesAssembly: typeof(YourActivity).Assembly,
               viewModelsAssembly: typeof(YourViewModel).Assembly);
       }
   }

   public class ActivityMetadataTests : ActivitiesMetadataChecker<MetadataFixture>
   {
       public ActivityMetadataTests(MetadataFixture fixture) : base(fixture) { }
   }
   ```

## Canonical example

See `Activities/Cryptography/UiPath.Cryptography.Activities.Tests/Resources/ActivityMetadataTests.cs` — first consumer in this repo.
