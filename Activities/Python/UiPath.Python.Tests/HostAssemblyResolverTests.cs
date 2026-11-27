using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using UiPath.Python.Host;
using UiPath.Python.Impl;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // The host runs out of process, so its assembly resolver is exercised here in process (the
    // resolver source is linked into this project) and, end to end, by PythonScopePackagedHostTests.
    public class HostAssemblyResolverTests
    {
        private const string Category = "Python";

        private static readonly string PythonRuntimePath = typeof(global::Python.Runtime.PythonEngine).Assembly.Location;

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryRegister_NoArguments_Succeeds()
        {
            Assert.True(HostAssemblyResolver.TryRegister(Array.Empty<string>(), out var error));
            Assert.Null(error);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryRegister_MissingAssemblyFile_FailsWithItsNameAndPath()
        {
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Python.Runtime.dll");

            Assert.False(HostAssemblyResolver.TryRegister(new[] { $"Python.Runtime={missing}" }, out var error));
            Assert.Contains("Python.Runtime", error);
            Assert.Contains(missing, error);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryRegister_InvalidArgument_Fails()
        {
            Assert.False(HostAssemblyResolver.TryRegister(new[] { "not-an-assembly-argument" }, out var error));
            Assert.Contains("not-an-assembly-argument", error);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryRegisterOrReport_InvalidArgument_WritesTheErrorAndFails()
        {
            using var output = new StringWriter();

            Assert.False(HostAssemblyResolver.TryRegisterOrReport(new[] { "not-an-assembly-argument" }, output));
            Assert.Contains("not-an-assembly-argument", output.ToString());
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryRegisterOrReport_NoArguments_SucceedsWithoutOutput()
        {
            using var output = new StringWriter();

            Assert.True(HostAssemblyResolver.TryRegisterOrReport(Array.Empty<string>(), output));
            Assert.Equal(string.Empty, output.ToString());
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void Resolve_UnknownAssembly_ReturnsNull()
        {
            var assemblies = new Dictionary<string, string> { ["Python.Runtime"] = PythonRuntimePath };

            Assert.Null(HostAssemblyResolver.Resolve(AssemblyLoadContext.Default, new AssemblyName("Some.Other.Assembly"), assemblies));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void Resolve_KnownAssembly_LoadsItFromThePassedPath()
        {
            var assemblies = new Dictionary<string, string> { ["Python.Runtime"] = PythonRuntimePath };
            var context = new AssemblyLoadContext("resolver-test", isCollectible: true);
            try
            {
                var assembly = HostAssemblyResolver.Resolve(context, new AssemblyName("Python.Runtime"), assemblies);

                Assert.NotNull(assembly);
                Assert.Equal("Python.Runtime", assembly.GetName().Name);
            }
            finally
            {
                context.Unload();
            }
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void Resolve_PathHoldsADifferentAssembly_Throws()
        {
            var assemblies = new Dictionary<string, string> { ["System.Xaml"] = PythonRuntimePath };
            var context = new AssemblyLoadContext("resolver-test", isCollectible: true);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => HostAssemblyResolver.Resolve(context, new AssemblyName("System.Xaml"), assemblies));
                Assert.Contains("Python.Runtime", exception.Message);
                Assert.Contains("System.Xaml", exception.Message);
            }
            finally
            {
                context.Unload();
            }
        }

        // The client side of the same contract: the assemblies it hands to the host must have a file on disk.
        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void HostAssemblies_Add_RecordsTheAssemblyLocation()
        {
            var paths = new Dictionary<string, string>();

            HostAssemblies.Add(paths, typeof(global::Python.Runtime.PythonEngine).Assembly);

            Assert.Equal(PythonRuntimePath, paths["Python.Runtime"]);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void HostAssemblies_Add_AssemblyWithoutFile_Throws()
        {
            // Loaded from a stream, so it has no Location.
            var context = new AssemblyLoadContext("resolver-test", isCollectible: true);
            try
            {
                using var stream = File.OpenRead(PythonRuntimePath);
                var inMemory = context.LoadFromStream(stream);

                var exception = Assert.Throws<InvalidOperationException>(() => HostAssemblies.Add(new Dictionary<string, string>(), inMemory));
                Assert.Contains("Python.Runtime", exception.Message);
            }
            finally
            {
                context.Unload();
            }
        }
    }
}
