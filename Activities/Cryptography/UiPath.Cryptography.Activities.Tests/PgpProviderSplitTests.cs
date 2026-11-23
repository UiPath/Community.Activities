using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UiPath.Cryptography.Pgp;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// The core package contains no OpenPGP; the extended package supplies it and is found at run time. These tests cover
    /// the discovery, the message for a missing provider and the manifest copy that the extended package ships.
    /// </summary>
    public class PgpProviderSplitTests
    {
        private static readonly Assembly Extended = typeof(PgpProvider).Assembly;
        private static readonly Assembly CoreActivities = typeof(EncryptText).Assembly;

        [Fact]
        public void Discover_NothingInstalled_ReturnsNull()
        {
            var provider = PgpProviderLocator.Discover(
                Enumerable.Empty<Assembly>(),
                PgpProviderLocator.WellKnownAssemblies,
                name => throw new FileNotFoundException(name));

            Assert.Null(provider);
        }

        [Fact]
        public void Discover_ProviderInLoadedAssembly_IsFound()
        {
            var provider = PgpProviderLocator.Discover(new[] { Extended }, Array.Empty<string>(), name => throw new FileNotFoundException(name));

            Assert.IsType<PgpProvider>(provider);
        }

        [Fact]
        public void Discover_ProviderNotLoadedYet_IsLoadedByTheWellKnownName()
        {
            string requested = null;
            var provider = PgpProviderLocator.Discover(
                Enumerable.Empty<Assembly>(),
                PgpProviderLocator.WellKnownAssemblies,
                name =>
                {
                    requested = name;
                    return Extended;
                });

            Assert.Equal("UiPath.Cryptography.Activities.Extended", requested);
            Assert.IsType<PgpProvider>(provider);
            Assert.Equal(Extended.GetName().Name, requested);
        }

        [Fact]
        public void MissingProviderMessage_NamesThePackageToInstall()
        {
            string message = UiPath.Cryptography.Properties.Resources.PgpProviderNotInstalled;

            Assert.Contains("UiPath.Cryptography.Activities", message);
            Assert.Contains("UiPath.Cryptography.Core.Activities", message);
        }

        [Fact]
        public void CorePackageAssemblies_DoNotReferencePgpCoreOrBouncyCastle()
        {
            foreach (Assembly core in new[] { typeof(CryptographyHelper).Assembly, CoreActivities })
            {
                string[] names = core.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
                Assert.DoesNotContain("PgpCore", names);
                Assert.DoesNotContain("BouncyCastle.Cryptography", names);
            }
        }

        [Fact]
        public void Discover_InstalledButNotLoadable_ReportsTheCause()
        {
            var errors = new System.Collections.Generic.List<Exception>();
            var cause = new BadImageFormatException("version mismatch");

            var provider = PgpProviderLocator.Discover(
                Enumerable.Empty<Assembly>(),
                PgpProviderLocator.WellKnownAssemblies,
                name => throw cause,
                errors);

            Assert.Null(provider);
            Assert.Same(cause, Assert.Single(errors));
        }

        [Fact]
        public void Discover_NotInstalled_ReportsNoCause()
        {
            var errors = new System.Collections.Generic.List<Exception>();

            PgpProviderLocator.Discover(Enumerable.Empty<Assembly>(), PgpProviderLocator.WellKnownAssemblies, name => throw new FileNotFoundException(name), errors);

            Assert.Empty(errors);
        }

        [Fact]
        public void Discover_AssemblyFromAnotherLoadContext_DoesNotPreventTheByNameLoad()
        {
            // The extended assembly loaded by another load context has another type identity; it must not count as loaded here.
            string path = Extended.Location;
            var other = new System.Runtime.Loader.AssemblyLoadContext("other", isCollectible: true);
            try
            {
                Assembly foreign = other.LoadFromAssemblyPath(path);
                string requested = null;

                // The foreign copy shares Core's IPgpProvider type here (the other context falls back to the default one), so
                // it is usable; either way a provider is found and discovery does not report the package as missing.
                var provider = PgpProviderLocator.Discover(
                    new[] { foreign },
                    PgpProviderLocator.WellKnownAssemblies,
                    name =>
                    {
                        requested = name;
                        return Extended;
                    });

                Assert.NotNull(provider);
                Assert.True(requested == null || requested == "UiPath.Cryptography.Activities.Extended");
            }
            finally
            {
                other.Unload();
            }
        }

        // The packaging projects are sources: they exist in a repository checkout (local runs and the build stage), not in
        // the test stage of the pipeline, which only downloads the test output. Returns null there and the checks are skipped.
        private static string CryptographyFolder()
        {
            for (string dir = AppContext.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir))
            {
                foreach (string candidate in new[] { dir, Path.Combine(dir, "Cryptography"), Path.Combine(dir, "Activities", "Cryptography") })
                {
                    if (File.Exists(Path.Combine(candidate, "UiPath.Cryptography.Core.Activities.Packaging", "UiPath.Cryptography.Core.Activities.Packaging.csproj")))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        // The scan judges what the Core package declares and ships, so the check reads the packaging project that produces
        // the nuspec (all of Core, including the coded workflow API assembly, goes through it).
        [Fact]
        public void CorePackagingProject_DoesNotDeclarePgpCoreBouncyCastleOrTheExtension()
        {
            if (CryptographyFolder() == null)
            {
                return;
            }

            string csproj = File.ReadAllText(Path.Combine(CryptographyFolder(), "UiPath.Cryptography.Core.Activities.Packaging", "UiPath.Cryptography.Core.Activities.Packaging.csproj"));
            // Only references count: the project's pack guard names PgpCore and BouncyCastle in its own text.
            Assert.DoesNotMatch(@"<(PackageReference|ProjectReference)[^>]*(PgpCore|BouncyCastle|Cryptography\.Activities\.Extended)", csproj);

            foreach (string project in Directory.GetFiles(CryptographyFolder(), "*.csproj", SearchOption.AllDirectories))
            {
                if (project.Contains(".Extended") || project.Contains("Tests") || project.Contains("UiPath.Cryptography.Activities.Packaging")
                    || project.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                string text = File.ReadAllText(project);
                Assert.DoesNotContain("PackageReference Include=\"PgpCore\"", text);
                Assert.DoesNotContain("PackageReference Include=\"BouncyCastle.Cryptography\"", text);
            }
        }

        [Fact]
        public void ExtendedPackagingProject_PinsTheCoreVersionAndPacksItsDocs()
        {
            if (CryptographyFolder() == null)
            {
                return;
            }

            string folder = Path.Combine(CryptographyFolder(), "UiPath.Cryptography.Activities.Packaging");
            string csproj = File.ReadAllText(Path.Combine(folder, "UiPath.Cryptography.Activities.Packaging.csproj"));
            Assert.Contains("[$(PackageVersion)]", csproj);
            Assert.Contains(@"PackagePath=""content\docs""", csproj);
            Assert.True(File.Exists(Path.Combine(folder, "docs", "overview.md")));
        }

        [Fact]
        public void ExtendedAssembly_HasNoActivities()
        {
            Assert.DoesNotContain(Extended.GetTypes(), type => typeof(System.Activities.Activity).IsAssignableFrom(type));
        }

        // The extended package lists the same activities as the core package (Studio Web only lists and shows the
        // activities of packages that have a manifest). These tests keep the two manifests from drifting apart.
        private static JsonDocument ReadManifest(Assembly assembly)
        {
            string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("ActivitiesMetadata.json", StringComparison.Ordinal));
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                return JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            }
        }

        [Fact]
        public void ExtendedManifest_IsACopyOfTheCoreManifest_WithItsOwnAssemblyPath()
        {
            using (JsonDocument core = ReadManifest(CoreActivities))
            using (JsonDocument extended = ReadManifest(Extended))
            {
                Assert.Equal("UiPath.Cryptography.Activities.dll", core.RootElement.GetProperty("AssemblyRelativePath").GetString());
                Assert.Equal("UiPath.Cryptography.Activities.Extended.dll", extended.RootElement.GetProperty("AssemblyRelativePath").GetString());

                string Normalize(JsonDocument doc) => string.Join(
                    "\n",
                    doc.RootElement.EnumerateObject()
                        .Where(property => property.Name != "AssemblyRelativePath")
                        .Select(property => property.Name + "=" + property.Value.GetRawText()));

                Assert.Equal(Normalize(core), Normalize(extended));
            }
        }

        [Fact]
        public void ExtendedAssembly_EmbedsTheIconsTheManifestNames()
        {
            using (JsonDocument extended = ReadManifest(Extended))
            {
                string[] resources = Extended.GetManifestResourceNames();
                foreach (JsonElement activity in extended.RootElement.GetProperty("Activities").EnumerateArray())
                {
                    string icon = activity.GetProperty("IconKey").GetString();
                    Assert.Contains(resources, resource => resource.EndsWith(icon, StringComparison.Ordinal));
                }
            }
        }

        [Fact]
        public void ExtendedManifest_NamesTypesAndTextsThatExistInCore()
        {
            using (JsonDocument extended = ReadManifest(Extended))
            {
                var texts = new System.Resources.ResourceManager(
                    extended.RootElement.GetProperty("ResourceManagerName").GetString(), CoreActivities);

                foreach (JsonElement activity in extended.RootElement.GetProperty("Activities").EnumerateArray())
                {
                    Assert.NotNull(CoreActivities.GetType(activity.GetProperty("FullName").GetString()));
                    Assert.NotNull(CoreActivities.GetType(activity.GetProperty("ViewModelType").GetString()));
                    Assert.False(string.IsNullOrEmpty(texts.GetString(activity.GetProperty("DisplayNameKey").GetString())));
                }
            }
        }
    }
}
