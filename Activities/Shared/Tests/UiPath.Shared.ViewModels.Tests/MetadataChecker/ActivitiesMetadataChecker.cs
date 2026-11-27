// Vendored from c:\Work\Activities3\Shared\Tests\UiPath.Shared.ViewModels.Tests\MetadataChecker\ on 2026-05-15.
// Adapted for Community.Activities: the upstream harness depends on `IActivityFactory`, `DesignPropertiesBaseViewModel`,
// `NotMappedPropertyAttribute`, `DesignProperty`, `DisplayPropertyValue`, `IDynamicDataSourceBuilder`, and
// `IWorkflowDesignApi` — types that ship with newer UiPath.Workflow / UiPath.Activities.Api versions than the ones
// Community.Activities currently pins. The four tests that need those types
// (`Check_ActivitiesMetadata_ViewModelsExist`, `Check_ActivitiesMetadata_ViewModelsMatchesActivity`,
// `Check_ActivitiesMetadata_FactoriesExist`) are omitted here.
//
// What this trimmed harness DOES validate:
//   • Activity types exist and inherit from `Activity`
//   • Every property marked `isRequired` in ActivitiesMetadata.json is also marked `isPrincipal`
//   • Every resource key referenced by the metadata exists in the pack's `Resources.resx`
//   • Activity tags refer to known triggers (or the configured Desktop trigger contract)
//   • Every icon key resolves to an embedded `.svg` resource
//   • Activities are not duplicated between `activities` and `additionalTypeCacheInfo.legacyDesignerActivities`
//
// Restore the omitted tests by re-vendoring the full upstream file once Community.Activities updates UiPath.Workflow
// to a version that exposes the missing types.
using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shouldly;
using UiPath.Platform.Triggers;
using Xunit;

namespace UiPath.Shared.ViewModels.Tests.MetadataChecker;

[Collection(MetadataCheckerConfig.FixtureCollectionName)]
public abstract class ActivitiesMetadataChecker<TFixture>
    where TFixture : IMetadataFixture
{
    private readonly TFixture _fixture;

    public ActivitiesMetadataChecker(TFixture fixture)
    {
        //Hold the fixture to avoid disposal until the test class is disposed
        _fixture = fixture;
    }

    [Theory]
    [ClassData(typeof(ActivitiesDataProvider))]
    public void Check_ActivitiesMetadata_ActivityExists(JToken activityMetadata)
    {
        var activityTypeName = activityMetadata["fullName"].Value<string>();

        Assert.NotNull(activityTypeName);
        Assert.NotEmpty(activityTypeName);

        Type activityType;
        try
        {
            activityType = MetadataCheckerConfig.ActivitiesAssembly.GetType(activityTypeName)
                ?? MetadataCheckerConfig.BaseActivitiesAssembly.GetType(activityTypeName);

            Assert.NotNull(activityType);
        }
        catch (Exception e)
        {
            throw new Exception($"{e.GetType().Name}: Activity '{activityTypeName}' could not be loaded. JSON Path: {activityMetadata.Path}", e);
        }

        Assert.True(activityType.IsSubclassOf(typeof(Activity)));
    }

    [Theory]
    [ClassData(typeof(RequiredPropertiesDataProvider))]
    public void Check_ActivitiesMetadata_AllRequiredPropertiesArePrincipal(JToken property)
    {
        // ensure that a property that is required, is always also principal
        var isPrincipal = property["isPrincipal"] as JValue;
        Assert.True(
            isPrincipal?.Value<bool>() is true,
            $"Property '{property["name"]}' is required but not principal. Activity: {property.Parent.Parent.Parent["fullName"]}"
        );
    }

    [Theory]
    [ClassData(typeof(DisplayNameKeysDataProvider))]
    [ClassData(typeof(DescriptionKeysDataProvider))]
    [ClassData(typeof(TooltipKeysDataProvider))]
    [ClassData(typeof(DisplayNameAliasKeysDataProvider))]
    [ClassData(typeof(CategoryKeysDataProvider))]
    [ClassData(typeof(OrderedCategoryDisplayNameKeysDataProvider))]
    public void Check_ActivitiesMetadata_ResourceStringExists(JToken resourceKey)
    {
        Assert.NotNull(resourceKey);

        var resourceKeyString = resourceKey.Value<string>();

        Assert.NotNull(resourceKeyString);
        Assert.NotEmpty(resourceKeyString);
        Assert.True(
            MetadataCheckerConfig.StringResources.ContainsKey(resourceKeyString),
            $"Resource '{resourceKeyString}' not found. JSON Path: {resourceKey.Path}");
    }

    [Theory]
    [ClassData(typeof(TagsDataProvider))]
    public void Check_ActivitiesMetadata_Tags(JToken tag)
    {
        var tagString = tag.Value<string>();
        Assert.NotNull(tagString);
        Assert.NotEmpty(tagString);

        // tag -> array -> property -> activity metadata
        var activityTypeName = tag.Parent.Parent.Parent["fullName"].Value<string>();

        Assert.NotNull(activityTypeName);
        Assert.NotEmpty(activityTypeName);

        if (tagString == MetadataCheckerConfig.DesktopTrigger)
        {
            AssertDesktopTriggersInheritance(activityTypeName);
            return;
        }

        // All trigger tags, future and past, should be known, and tested to respect the contract
        Assert.True(MetadataCheckerConfig.KnownTags.TryGetValue(tagString, out var expectedActivity), $"Unknown tag: {tag}");
        Assert.Equal(expectedActivity, activityTypeName);
    }

    private static void AssertDesktopTriggersInheritance(string activityTypeName)
    {
        var type = MetadataCheckerConfig.ActivitiesAssembly.GetType(activityTypeName);
        Assert.NotNull(type);
        while (type.BaseType is not null)
        {
            if (!type.BaseType.IsGenericType)
            {
                type = type.BaseType;
                continue;
            }

            var genericType = type.BaseType.GetGenericTypeDefinition();
            if (genericType == typeof(InterruptibleTriggerBase<>))
                return;

            type = type.BaseType;
        }

        throw new Exception($"Desktop trigger {activityTypeName} should inherit from {typeof(InterruptibleTriggerBase<>)}");
    }

#if WINDOWS
    [Fact]
    public void Check_ActivitiesMetadata_NoDuplicatesBetweenActivitiesAndLegacy()
    {
        var metadata = MetadataCheckerConfig.Metadata;

        var legacySection = metadata["additionalTypeCacheInfo"]?["legacyDesignerActivities"];
        if (legacySection == null)
            return;

        var activitiesFullNames = metadata["activities"]
            .Children()
            .Select(a => a["fullName"].Value<string>());

        var legacyFullNames = legacySection
            .Children()
            .Select(a => a["fullName"].Value<string>());

        activitiesFullNames.Concat(legacyFullNames).ShouldBeUnique();
    }
#endif

    [Theory]
    [ClassData(typeof(IconsDataProvider))]
    public void Check_ActivitiesMetadata_Icons(JToken icon)
    {
        Assert.Equal(JTokenType.String, icon.Type);

        var tokenValue = icon.Value<string>();
        Assert.NotNull(tokenValue);
        Assert.NotEmpty(tokenValue);
        Assert.EndsWith(".svg", tokenValue);

        Assert.Contains(tokenValue, MetadataCheckerConfig.IconResources);
    }
}

// Data providers for ClassData
public class ActivitiesDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata["activities"].Children().Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class RequiredPropertiesDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..properties[?(@.isRequired == true)]").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DisplayNameKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..displayNameKey").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DescriptionKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..descriptionKey").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TooltipKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..tooltipKey").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DisplayNameAliasKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..displayNameAliasKeys[*]").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CategoryKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..categoryKey").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class OrderedCategoryDisplayNameKeysDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..orderedCategoryDisplayNameKeys").Children().Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TagsDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        foreach (var item in metadata.SelectTokens("$..tags[*]").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class IconsDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var metadata = MetadataCheckerConfig.Metadata;
        if (metadata.SelectToken("assemblyIconKey")  is { } assemblyIcon)
            yield return new[] { assemblyIcon };
        if (metadata.SelectToken("defaultActivityIconKey") is { } defaultActivityIcon)
            yield return new[] { defaultActivityIcon };
        foreach (var item in metadata.SelectTokens("$..iconKey").Select(token => new[] { token }))
            yield return item;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
