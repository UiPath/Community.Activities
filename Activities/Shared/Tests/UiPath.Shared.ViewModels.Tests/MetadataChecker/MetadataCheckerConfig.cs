// Vendored from c:\Work\Activities3\Shared\Tests\UiPath.Shared.ViewModels.Tests\MetadataChecker\ on 2026-05-15.
// Community.Activities adaptations vs upstream:
//   • ActivitiesMetadata.json files in this repo use PascalCase keys (`FullName`, `Activities`, etc.). The harness walks
//     the JSON with camelCase JPath queries, so we normalize the JObject tree to camelCase right after parsing.
//   • Older metadata files don't carry a `resourceManagerName` field; consumers can pass it through the new
//     `resourceManagerName` ctor parameter and we fall back to that when the JSON is silent.
//   • ReadStringResources guards against a null ResourceSet so a misconfigured resource manager surfaces as zero
//     entries (which will cause individual resource-key tests to fail loudly) rather than a fixture-load NRE.
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace UiPath.Shared.ViewModels.Tests.MetadataChecker
{
    public class MetadataCheckerConfig
    {
        public const string FixtureCollectionName = "ActivitiesMetadataCollection";

        private const string MetadataFileName = "ActivitiesMetadata.json";

        //To be provided by the user
        public static Assembly BaseActivitiesAssembly { get; set; }
        public static Assembly ActivitiesAssembly { get; set; }
        public static Assembly ViewModelsAssembly { get; set; }

        public static string DesktopTrigger { get; set; }

        public static Dictionary<string, string> KnownTags { get; set; }

        public static HashSet<string> ExcludedActivities { get; set; } = new HashSet<string>();

        //Computed properties from input
        public static JToken Metadata { get; set; }

        public static Dictionary<string, string> StringResources { get; set; } = new Dictionary<string, string>();

        public static HashSet<string> IconResources { get; set; } = new HashSet<string>();

        //Every project should instantiate its own config, with proper params when constructing the fixture
        public MetadataCheckerConfig(
            Assembly baseActivitiesAssembly,
            Assembly activitiesAssembly,
            Assembly viewModelsAssembly,
            string desktopTrigger = null,
            Dictionary <string, string> knownTags = null,
            HashSet<string> excludedActivities = null,
            string resourceManagerName = null)
        {
            BaseActivitiesAssembly = baseActivitiesAssembly;
            ActivitiesAssembly = activitiesAssembly;
            ViewModelsAssembly = viewModelsAssembly;
            DesktopTrigger = desktopTrigger;
            KnownTags = knownTags ?? new Dictionary<string, string>();
            ExcludedActivities = excludedActivities ?? new HashSet<string>();

            var resourceName = ViewModelsAssembly.GetManifestResourceNames().First(n => n.EndsWith(MetadataFileName));
            using var stream = ViewModelsAssembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);

            string jsonFile = reader.ReadToEnd();

            Metadata = JToken.Parse(jsonFile, new JsonLoadSettings()
            {
                CommentHandling = CommentHandling.Load,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                LineInfoHandling = LineInfoHandling.Load
            });

            // Community.Activities metadata files use PascalCase; upstream harness expects camelCase. Normalize.
            Metadata = NormalizeKeysToCamelCase(Metadata);

            ReadStringResources(resourceManagerName);
            ReadIconResources();
        }

        private static JToken NormalizeKeysToCamelCase(JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    var newObj = new JObject();
                    foreach (var prop in obj.Properties())
                    {
                        var name = prop.Name;
                        var newName = name.Length > 0 ? char.ToLowerInvariant(name[0]) + name.Substring(1) : name;
                        newObj[newName] = NormalizeKeysToCamelCase(prop.Value);
                    }
                    return newObj;
                case JArray arr:
                    var newArr = new JArray();
                    foreach (var item in arr)
                        newArr.Add(NormalizeKeysToCamelCase(item));
                    return newArr;
                default:
                    return token;
            }
        }

        private static void ReadStringResources(string fallbackResourceManagerName)
        {
            var resManager = GetResourceManagerFromMetadataFromFile(fallbackResourceManagerName);
            using var resourceSet = resManager?.GetResourceSet(global::System.Globalization.CultureInfo.InvariantCulture, true, false);
            if (resourceSet == null)
                return; // no resource manager configured; resource-key tests will fail individually with a clear message.
            foreach (DictionaryEntry entry in resourceSet)
            {
                StringResources[entry.Key.ToString()] = entry.Value?.ToString() ?? string.Empty;
            }
        }

        private static void ReadIconResources()
        {
            IconResources = ViewModelsAssembly.GetManifestResourceNames()
                .Where(embeddedResource => embeddedResource.ToLower().EndsWith(".svg"))
                .Select(icon => string.Join(".", icon.Split(".").TakeLast(2).ToArray()))
                .ToHashSet();
        }

        private static ResourceManager GetResourceManagerFromMetadataFromFile(string fallbackResourceManagerName)
        {
            var resManagerName = Metadata["resourceManagerName"]?.Value<string>() ?? fallbackResourceManagerName;
            var resourceManagerAssembly = GetResourceManagerAssembly(resManagerName);
            var res = resManagerName == null || resourceManagerAssembly == null
                ? null
                : new ResourceManager(resManagerName, resourceManagerAssembly);
            return res;
        }

        private static Assembly GetResourceManagerAssembly(string resManagerName)
        {
            if (resManagerName == null)
                return null;

            Assembly resourceManagerAssembly = null;
            var assemblies = new Assembly[] {
                BaseActivitiesAssembly,
                ActivitiesAssembly,
                ViewModelsAssembly
            };
            foreach (var contextAssembly in assemblies)
            {
                if (contextAssembly.GetManifestResourceNames().Contains($"{resManagerName}.resources"))
                {
                    resourceManagerAssembly = contextAssembly;
                    break;
                }
            }

            return resourceManagerAssembly;
        }
    }
}
