using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace UiPath.FTP.Extensibility
{
    /// <summary>
    /// Marks an <see cref="ISftpCryptoExtension"/> of an assembly so that SFTP connections find it automatically
    /// (see <see cref="FtpConfiguration.DiscoverCryptoExtensions"/>), for example the extension of an extension package:
    /// <c>[assembly: SftpCryptoExtension(typeof(MyExtension))]</c>.
    /// </summary>
    /// <remarks>
    /// The type must be a public, non-abstract class with a public parameterless constructor. A new instance is created
    /// for every connection.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SftpCryptoExtensionAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="SftpCryptoExtensionAttribute"/> class.</summary>
        /// <param name="extensionType">The type that implements <see cref="ISftpCryptoExtension"/>.</param>
        public SftpCryptoExtensionAttribute(Type extensionType)
        {
            ExtensionType = extensionType;
        }

        /// <summary>Gets the type that implements <see cref="ISftpCryptoExtension"/>.</summary>
        public Type ExtensionType { get; }
    }

    /// <summary>
    /// Finds the extensions that installed packages provide. Done for every connection and never remembered: Studio and
    /// the robot load assemblies lazily, so an assembly that is not loaded now may be loaded by the next connection.
    /// </summary>
    internal static class SftpCryptoExtensionDiscovery
    {
        /// <summary>
        /// Assemblies loaded by name when they are not loaded yet. Loading only succeeds when the assembly is installed;
        /// a missing assembly simply means there is no extension.
        /// </summary>
        internal static readonly string[] WellKnownAssemblies = { "UiPath.FTP.Activities.Extended" };

        private static readonly string OwnAssemblyName = typeof(SftpCryptoExtensionAttribute).Assembly.GetName().Name;

        /// <summary>
        /// Returns the explicit extensions followed by the discovered ones, without a second instance of the same type.
        /// </summary>
        /// <param name="explicitExtensions">The extensions of <see cref="FtpConfiguration.CryptoExtensions"/>.</param>
        /// <param name="discover">Whether to look for extensions of installed packages.</param>
        public static IReadOnlyList<ISftpCryptoExtension> Combine(IEnumerable<ISftpCryptoExtension> explicitExtensions, bool discover)
        {
            var result = new List<ISftpCryptoExtension>(explicitExtensions ?? Enumerable.Empty<ISftpCryptoExtension>());
            if (!discover)
            {
                return result;
            }

            // The filter looks at the result while it grows, so two discovered extensions of the same type are added once.
            foreach (ISftpCryptoExtension found in Discover().Where(f => !result.Any(existing => existing != null && existing.GetType() == f.GetType())))
            {
                result.Add(found);
            }

            return result;
        }

        public static IReadOnlyList<ISftpCryptoExtension> Discover() =>
            Discover(LoadedAssemblies(), WellKnownAssemblies, name => Assembly.Load(new AssemblyName(name)));

        /// <summary>
        /// The assemblies of the load context this library was loaded into. Not the whole AppDomain: with several load
        /// contexts each one has its own copy of this library, and an extension of another context declares an attribute
        /// of a different type, so it is of no use here (and it must not stop the extension from being loaded into this
        /// context by name).
        /// </summary>
        internal static IEnumerable<Assembly> LoadedAssemblies()
        {
            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(SftpCryptoExtensionAttribute).Assembly);
            IEnumerable<Assembly> assemblies = context?.Assemblies ?? AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Where(assembly => !assembly.IsDynamic).ToList();
        }

        /// <summary>The testable core of the discovery.</summary>
        /// <param name="loadedAssemblies">The assemblies to look at.</param>
        /// <param name="wellKnownAssemblies">Names of assemblies to load when they are not loaded yet.</param>
        /// <param name="load">Loads an assembly by name; may throw when it is not installed.</param>
        internal static IReadOnlyList<ISftpCryptoExtension> Discover(
            IEnumerable<Assembly> loadedAssemblies,
            IEnumerable<string> wellKnownAssemblies,
            Func<string, Assembly> load)
        {
            var found = new List<ISftpCryptoExtension>();
            var seenTypes = new HashSet<Type>();

            var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly assembly in loadedAssemblies)
            {
                loadedNames.Add(assembly.GetName().Name);
                if (References(assembly))
                {
                    AddExtensions(assembly, found, seenTypes);
                }
            }

            // A well-known assembly that is installed but not loaded yet (assemblies load lazily) is loaded by name.
            foreach (string name in wellKnownAssemblies)
            {
                if (loadedNames.Contains(name))
                {
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = load(name);
                }
                catch (Exception)
                {
                    // Not installed (or not loadable): there is no extension.
                    continue;
                }

                AddExtensions(assembly, found, seenTypes);
            }

            return found;
        }

        // Only assemblies that reference this one can declare an extension; this avoids reading the attributes of every
        // framework assembly.
        private static bool References(Assembly assembly)
        {
            if (assembly == typeof(SftpCryptoExtensionAttribute).Assembly)
            {
                return false;
            }

            try
            {
                return assembly.GetReferencedAssemblies().Any(reference => reference.Name == OwnAssemblyName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void AddExtensions(Assembly assembly, List<ISftpCryptoExtension> found, HashSet<Type> seenTypes)
        {
            IEnumerable<SftpCryptoExtensionAttribute> attributes;
            try
            {
                attributes = assembly.GetCustomAttributes<SftpCryptoExtensionAttribute>().ToList();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("SFTP crypto extensions of {0} were not read: {1}", assembly.GetName().Name, ex.Message);
                return;
            }

            foreach (Type type in attributes.Select(attribute => attribute.ExtensionType))
            {
                if (type == null || !seenTypes.Add(type))
                {
                    continue;
                }

                if (!typeof(ISftpCryptoExtension).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface
                    || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Trace.TraceWarning(
                        "{0} is declared as an SFTP crypto extension by {1} but is not a public, non-abstract class implementing {2} with a parameterless constructor; it is ignored.",
                        type.FullName,
                        assembly.GetName().Name,
                        nameof(ISftpCryptoExtension));
                    continue;
                }

                try
                {
                    found.Add((ISftpCryptoExtension)Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("The SFTP crypto extension {0} could not be created and is ignored: {1}", type.FullName, ex.Message);
                }
            }
        }
    }
}
