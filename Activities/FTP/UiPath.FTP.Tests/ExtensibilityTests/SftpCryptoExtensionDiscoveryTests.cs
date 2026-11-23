using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Renci.SshNet;
using UiPath.FTP.Extensibility;
using UiPath.FTP.Tests;
using Xunit;

// Extensions of this test assembly, as an extension package declares them. The discovery finds them in every test
// that creates an SFTP session; they only add one harmless key exchange name.
[assembly: SftpCryptoExtension(typeof(DiscoveryTestExtension))]
[assembly: SftpCryptoExtension(typeof(NotAnExtension))]
[assembly: SftpCryptoExtension(typeof(AbstractTestExtension))]
[assembly: SftpCryptoExtension(typeof(NoParameterlessConstructorExtension))]

namespace UiPath.FTP.Tests
{
    public sealed class DiscoveryTestExtension : ISftpCryptoExtension
    {
        public const string KeyExchangeName = "test-discovery-kex";

        public string Name => "discovery test extension";

        public void Register(ISftpCryptoRegistry registry) =>
            registry.AddKeyExchange(KeyExchangeName, HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => throw new NotSupportedException());
    }

    public sealed class NotAnExtension
    {
    }

    public abstract class AbstractTestExtension : ISftpCryptoExtension
    {
        public string Name => "abstract";

        public abstract void Register(ISftpCryptoRegistry registry);
    }

    public sealed class NoParameterlessConstructorExtension : ISftpCryptoExtension
    {
        public NoParameterlessConstructorExtension(int unused)
        {
        }

        public string Name => "no parameterless constructor";

        public void Register(ISftpCryptoRegistry registry)
        {
        }
    }

    public class SftpCryptoExtensionDiscoveryTests
    {
        private static readonly string[] SomeExtensionAssembly = { "Some.Extension.Assembly" };
        private static readonly string[] LateExtensionAssembly = { "Late.Extension" };
        private static readonly string[] EcdhNames = { "ecdh-sha2-nistp256", "ecdh-sha2-nistp384", "ecdh-sha2-nistp521" };

        private static readonly Assembly TestAssembly = typeof(DiscoveryTestExtension).Assembly;

        private static Assembly NeverLoad(string name) => throw new InvalidOperationException("nothing should be loaded: " + name);

        [Fact]
        public void Discover_FindsTheDeclaredExtension()
        {
            var found = SftpCryptoExtensionDiscovery.Discover(new[] { TestAssembly }, Array.Empty<string>(), NeverLoad);

            ISftpCryptoExtension extension = Assert.Single(found);
            Assert.IsType<DiscoveryTestExtension>(extension);
        }

        [Fact]
        public void Discover_IgnoresDeclarationsThatAreNotUsableExtensions()
        {
            var found = SftpCryptoExtensionDiscovery.Discover(new[] { TestAssembly }, Array.Empty<string>(), NeverLoad);

            Assert.DoesNotContain(found, e => e.GetType() == typeof(NoParameterlessConstructorExtension));
            Assert.DoesNotContain(found, e => e is AbstractTestExtension);
            Assert.Single(found);
        }

        [Fact]
        public void Discover_IgnoresAssembliesThatDoNotReferenceTheLibrary()
        {
            var found = SftpCryptoExtensionDiscovery.Discover(new[] { typeof(string).Assembly, typeof(Uri).Assembly }, Array.Empty<string>(), NeverLoad);

            Assert.Empty(found);
        }

        [Fact]
        public void Discover_CreatesNewInstancesForEachCall()
        {
            var first = SftpCryptoExtensionDiscovery.Discover(new[] { TestAssembly }, Array.Empty<string>(), NeverLoad);
            var second = SftpCryptoExtensionDiscovery.Discover(new[] { TestAssembly }, Array.Empty<string>(), NeverLoad);

            Assert.NotSame(first[0], second[0]);
        }

        [Fact]
        public void Discover_LoadsAWellKnownAssemblyThatIsNotLoadedYet()
        {
            string requested = null;

            var found = SftpCryptoExtensionDiscovery.Discover(Array.Empty<Assembly>(), SomeExtensionAssembly, name =>
            {
                requested = name;
                return TestAssembly;
            });

            Assert.Equal("Some.Extension.Assembly", requested);
            Assert.IsType<DiscoveryTestExtension>(Assert.Single(found));
        }

        [Fact]
        public void Discover_DoesNotLoadAWellKnownAssemblyThatIsAlreadyLoaded()
        {
            var found = SftpCryptoExtensionDiscovery.Discover(new[] { TestAssembly }, new[] { TestAssembly.GetName().Name }, NeverLoad);

            Assert.IsType<DiscoveryTestExtension>(Assert.Single(found));
        }

        [Fact]
        public void Discover_ALoadFailureIsToleratedAndNotRemembered()
        {
            bool installed = false;
            Func<string, Assembly> load = name => installed ? TestAssembly : throw new System.IO.FileNotFoundException(name);

            var whileMissing = SftpCryptoExtensionDiscovery.Discover(Array.Empty<Assembly>(), LateExtensionAssembly, load);
            installed = true;
            var afterLoading = SftpCryptoExtensionDiscovery.Discover(Array.Empty<Assembly>(), LateExtensionAssembly, load);

            Assert.Empty(whileMissing);
            Assert.IsType<DiscoveryTestExtension>(Assert.Single(afterLoading));
        }

        [Fact]
        public void Combine_PutsExplicitExtensionsFirstAndAddsNoSecondInstanceOfTheSameType()
        {
            var explicitExtension = new DiscoveryTestExtension();

            var combined = SftpCryptoExtensionDiscovery.Combine(new ISftpCryptoExtension[] { explicitExtension }, discover: true);

            Assert.Same(explicitExtension, combined[0]);
            Assert.Single(combined.OfType<DiscoveryTestExtension>());
        }

        [Fact]
        public void Combine_WithDiscoveryOffReturnsOnlyTheExplicitExtensions()
        {
            Assert.Empty(SftpCryptoExtensionDiscovery.Combine(null, discover: false));
            Assert.Empty(SftpCryptoExtensionDiscovery.Combine(Array.Empty<ISftpCryptoExtension>(), discover: false));
        }

        [Fact]
        public void Combine_WithDiscoveryOnFindsTheExtensionOfTheLoadedTestAssembly()
        {
            var combined = SftpCryptoExtensionDiscovery.Combine(null, discover: true);

            Assert.Single(combined.OfType<DiscoveryTestExtension>());
        }

        private static ConnectionInfo ConnectionInfoOf(SftpSession session)
        {
            FieldInfo field = typeof(SftpSession).GetField("_sftpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            return ((SftpClient)field.GetValue(session)).ConnectionInfo;
        }

        [Fact]
        public void SftpSession_UsesTheDiscoveredExtensionsByDefault()
        {
            var configuration = new FtpConfiguration("localhost") { Username = "user", Password = "password" };

            ConnectionInfo info = ConnectionInfoOf(new SftpSession(configuration));

            Assert.Contains(DiscoveryTestExtension.KeyExchangeName, info.KeyExchangeAlgorithms.Keys);
        }

        [Fact]
        public void SftpSession_DoesNotDiscoverWhenDiscoveryIsOff()
        {
            var configuration = new FtpConfiguration("localhost") { Username = "user", Password = "password", DiscoverCryptoExtensions = false };

            ConnectionInfo info = ConnectionInfoOf(new SftpSession(configuration));

            Assert.DoesNotContain(DiscoveryTestExtension.KeyExchangeName, info.KeyExchangeAlgorithms.Keys);
            Assert.Equal(EcdhNames, info.KeyExchangeAlgorithms.Keys.ToArray());
        }
    }
}
