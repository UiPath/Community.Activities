using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using Xunit;

namespace UiPath.Database.Extended.Tests
{
    /// <summary>
    /// The extended package lists the same activities as the core package (Studio Web only lists and shows the activities
    /// of packages that have a manifest), without a copy of their code or texts. These tests keep the two manifests
    /// from drifting apart and check that everything the extended manifest names can be found.
    /// </summary>
    public class ManifestTests
    {
        private static readonly Assembly Core = typeof(UiPath.Database.Activities.DatabaseConnect).Assembly;
        private static readonly Assembly Extended = typeof(UiPath.Database.Extended.OracleDatabaseProvider).Assembly;

        private static JsonDocument ReadManifest(Assembly assembly)
        {
            string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("ActivitiesMetadata.json", StringComparison.Ordinal));
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                return JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            }
        }

        private static string Text(JsonElement element, string property) =>
            element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        [Fact]
        public void ExtendedManifest_IsTheCoreManifestWithItsOwnAssemblyName()
        {
            using (JsonDocument core = ReadManifest(Core))
            using (JsonDocument extended = ReadManifest(Extended))
            {
                Assert.Equal(Extended.GetName().Name + ".dll", Text(extended.RootElement, "AssemblyRelativePath"));

                // Everything but the assembly name is the same, so a change of Core's manifest fails here until the copy
                // is updated.
                string Normalized(JsonDocument document) =>
                    JsonSerializer.Serialize(document.RootElement.EnumerateObject()
                        .Where(property => property.Name != "AssemblyRelativePath")
                        .ToDictionary(property => property.Name, property => property.Value));

                Assert.Equal(Normalized(core), Normalized(extended));
                Assert.NotEmpty(extended.RootElement.GetProperty("Activities").EnumerateArray());
            }
        }

        [Fact]
        public void ExtendedAssembly_EmbedsTheIconsOfEveryActivity()
        {
            string[] resources = Extended.GetManifestResourceNames();
            using (JsonDocument extended = ReadManifest(Extended))
            {
                foreach (JsonElement activity in extended.RootElement.GetProperty("Activities").EnumerateArray())
                {
                    string icon = Text(activity, "IconKey");
                    Assert.Contains(resources, r => r.EndsWith("." + icon, StringComparison.Ordinal));
                }
            }
        }

        [Fact]
        public void TheTypesAndTextsTheExtendedManifestNamesExistInCore()
        {
            using (JsonDocument extended = ReadManifest(Extended))
            {
                var resources = new ResourceManager(Text(extended.RootElement, "ResourceManagerName"), Core);

                foreach (JsonElement activity in extended.RootElement.GetProperty("Activities").EnumerateArray())
                {
                    Assert.NotNull(Core.GetType(Text(activity, "FullName")));

                    foreach (string key in new[] { Text(activity, "DisplayNameKey"), Text(activity, "DescriptionKey") })
                    {
                        string value = resources.GetString(key);
                        Assert.False(string.IsNullOrEmpty(value), key + " has no text in the core resources.");
                        Assert.NotEqual(key, value);
                    }
                }
            }
        }

        [Fact]
        public void TheExtendedAssembly_DoesNotContainTheActivities()
        {
            // The code stays in the core package; the extended package must not ship a second copy of it.
            Assert.DoesNotContain(Extended.GetTypes(), t => t.Namespace == "UiPath.Database.Activities");
        }
    }
}
