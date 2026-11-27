using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using Xunit;

namespace UiPath.FTP.Extended.Tests
{
    /// <summary>
    /// The extended package lists the same activities as the core package (Studio Web only lists and shows the activities
    /// of packages that have a manifest), without a copy of their code or texts. These tests keep the two manifests
    /// from drifting apart and check that everything the extended manifest names can be found.
    /// </summary>
    public class ManifestTests
    {
        private static readonly Assembly Core = typeof(UiPath.FTP.Activities.WithFtpSession).Assembly;
        private static readonly Assembly Extended = typeof(ExtendedSftpCryptoExtension).Assembly;

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

        private static string[] Aliases(JsonElement activity) =>
            activity.TryGetProperty("DisplayNameAliasKeys", out JsonElement keys) ? keys.EnumerateArray().Select(k => k.GetString()).ToArray() : Array.Empty<string>();

        [Fact]
        public void ExtendedManifest_ListsTheSameActivitiesAsCore()
        {
            using (JsonDocument core = ReadManifest(Core))
            using (JsonDocument extended = ReadManifest(Extended))
            {
                var coreActivities = core.RootElement.GetProperty("Activities").EnumerateArray().ToArray();
                var extendedActivities = extended.RootElement.GetProperty("Activities").EnumerateArray().ToArray();

                Assert.Equal(coreActivities.Length, extendedActivities.Length);
                Assert.NotEmpty(extendedActivities);
                for (int i = 0; i < coreActivities.Length; i++)
                {
                    foreach (string property in new[] { "FullName", "ShortName", "DisplayNameKey", "DescriptionKey", "IconKey", "ViewModelType" })
                    {
                        Assert.Equal(Text(coreActivities[i], property), Text(extendedActivities[i], property));
                    }

                    Assert.Equal(Aliases(coreActivities[i]), Aliases(extendedActivities[i]));
                }

                // The texts come from Core's resources, found by the same resource manager name.
                Assert.Equal(Text(core.RootElement, "ResourceManagerName"), Text(extended.RootElement, "ResourceManagerName"));
                Assert.Equal(Text(core.RootElement, "DefaultActivityColor"), Text(extended.RootElement, "DefaultActivityColor"));
                Assert.Equal(Text(core.RootElement, "DefaultActivityNameBackgroundColor"), Text(extended.RootElement, "DefaultActivityNameBackgroundColor"));
            }
        }

        [Fact]
        public void ExtendedManifest_NamesItsOwnAssembly()
        {
            using (JsonDocument extended = ReadManifest(Extended))
            {
                Assert.Equal(Extended.GetName().Name + ".dll", Text(extended.RootElement, "AssemblyRelativePath"));
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
                    Assert.NotNull(Core.GetType(Text(activity, "ViewModelType")));

                    foreach (string key in new[] { Text(activity, "DisplayNameKey"), Text(activity, "DescriptionKey") }.Concat(Aliases(activity)))
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
            Assert.DoesNotContain(Extended.GetTypes(), t => t.Namespace == "UiPath.FTP.Activities");
        }
    }
}
