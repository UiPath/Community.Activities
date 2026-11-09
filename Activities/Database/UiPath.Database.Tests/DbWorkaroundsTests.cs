using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace UiPath.Database.Tests
{
    public class DbWorkaroundsTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "dbworkarounds-" + Guid.NewGuid().ToString("N"));

        public DbWorkaroundsTests() => Directory.CreateDirectory(_root);

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch (IOException) { }
            GC.SuppressFinalize(this);
        }

        private static string AddPackage(string root, string package, string version)
        {
            var path = Path.Combine(root, package, version);
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void FindPackageVersionPath_VulnerableAndPatchedSqliteInstalled_ChoosesPatched()
        {
            var oldRoot = Path.Combine(_root, "old");
            var newRoot = Path.Combine(_root, "new");
            AddPackage(oldRoot, DbWorkarounds.Sqlite.Package, "2.1.6");
            var patched = AddPackage(newRoot, DbWorkarounds.Sqlite.Package, "2.1.14");

            var result = DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { oldRoot, newRoot });

            result.ShouldBe(patched);
        }

        [Fact]
        public void FindPackageVersionPath_OnlyVulnerableSqliteInstalled_Throws()
        {
            var oldRoot = Path.Combine(_root, "old");
            AddPackage(oldRoot, DbWorkarounds.Sqlite.Package, "2.1.6");

            Should.Throw<DirectoryNotFoundException>(() =>
                DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { oldRoot }));
        }

        [Fact]
        public void FindPackageVersionPath_VersionExactlyAtFloor_IsAccepted()
        {
            var root = Path.Combine(_root, "r");
            var atFloor = AddPackage(root, DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion);

            DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { root })
                .ShouldBe(atFloor);
        }

        [Fact]
        public void FindPackageVersionPath_SeveralPatchedVersions_PicksLowestAtOrAboveFloor()
        {
            var root = Path.Combine(_root, "r");
            AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.6");
            var lowestPatched = AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.14");
            AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.20");

            DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { root })
                .ShouldBe(lowestPatched);
        }

        [Fact]
        public void FindPackageVersionPath_StableAndPrereleaseInstalled_PrefersStable()
        {
            var root = Path.Combine(_root, "r");
            var stable = AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.14");
            AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.15-beta");

            DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { root })
                .ShouldBe(stable);
        }

        [Fact]
        public void FindPackageVersionPath_OnlyPrereleaseAtOrAboveFloor_FallsBackToPrerelease()
        {
            var root = Path.Combine(_root, "r");
            AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.6");
            var pre = AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.15-beta");

            DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { root })
                .ShouldBe(pre);
        }

        [Fact]
        public void FindPackageVersionPath_MissingRootsAreSkipped()
        {
            var missing = Path.Combine(_root, "does-not-exist");
            var root = Path.Combine(_root, "r");
            var patched = AddPackage(root, DbWorkarounds.Sqlite.Package, "2.1.14");

            DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { missing, root })
                .ShouldBe(patched);
        }

        [Theory]
        [InlineData("Database.UiPath.Database.csproj")]
        [InlineData("Database.UiPath.Database.Activities.Packaging.csproj")]
        [InlineData("Database.UiPath.Data.ConnectionUI.Dialog.csproj")]
        public void ProjectsReferencingSqlite_AlsoReferenceNativePackage(string copiedProject)
        {
            // The csproj files are copied next to the test assembly (see UiPath.Database.Tests.csproj).
            var project = XDocument.Load(Path.Combine(AppContext.BaseDirectory, copiedProject));
            var references = project.Descendants("PackageReference")
                .Select(e => (string)e.Attribute("Include") ?? (string)e.Attribute("Update"))
                .ToList();

            references.ShouldContain("Microsoft.Data.Sqlite");
            references.ShouldContain("SQLitePCLRaw.lib.e_sqlite3");
        }

        [Fact]
        public void SqliteMinVersion_IsNotVulnerable()
        {
            Version.Parse(DbWorkarounds.Sqlite.MinVersion).ShouldBeGreaterThanOrEqualTo(new Version(2, 1, 12));
        }

        [Fact]
        public void SqliteNativePackagePin_IsAtLeastMinVersion()
        {
            // Directory.build.targets is copied next to the test assembly (see UiPath.Database.Tests.csproj).
            var targets = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Database.Directory.build.targets"));
            var pinned = targets.Descendants("PackageReference")
                .Where(e => (string)e.Attribute("Update") == "SQLitePCLRaw.lib.e_sqlite3")
                .Select(e => (string)e.Attribute("Version"))
                .Single();

            Version.Parse(pinned).ShouldBeGreaterThanOrEqualTo(Version.Parse(DbWorkarounds.Sqlite.MinVersion));
        }
    }
}
