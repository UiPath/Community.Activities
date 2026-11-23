using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Pgp
{
    /// <summary>
    /// Finds the OpenPGP provider that an installed package supplies. A provider that was found is kept; a miss is never
    /// remembered, because Studio and the robot load assemblies lazily and the provider assembly may be loaded by the next
    /// call.
    /// </summary>
    internal static class PgpProviderLocator
    {
        /// <summary>
        /// Assemblies loaded by name when they are not loaded yet. Loading only succeeds when the assembly is installed.
        /// </summary>
        internal static readonly string[] WellKnownAssemblies = { "UiPath.Cryptography.Activities.Extended" };

        private static readonly string OwnAssemblyName = typeof(PgpProviderAttribute).Assembly.GetName().Name;

        private static volatile IPgpProvider _found;

        /// <summary>Returns the provider, or throws an <see cref="InvalidOperationException"/> that names the package to install.</summary>
        internal static IPgpProvider GetProvider()
        {
            IPgpProvider provider = _found;
            if (provider != null)
            {
                return provider;
            }

            var errors = new List<Exception>();
            provider = Discover(
                AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic),
                WellKnownAssemblies,
                name => Assembly.Load(new AssemblyName(name)),
                errors);
            if (provider == null)
            {
                // A package that is not installed has no error to report; an installed one that cannot be loaded or
                // created keeps its cause as the inner exception, so a broken install is distinguishable from a missing one.
                throw new InvalidOperationException(Resources.PgpProviderNotInstalled, errors.FirstOrDefault());
            }

            _found = provider;
            return provider;
        }

        /// <summary>The testable core of the discovery.</summary>
        /// <param name="loadedAssemblies">The assemblies to look at.</param>
        /// <param name="wellKnownAssemblies">Names of assemblies to load when they are not loaded yet.</param>
        /// <param name="load">Loads an assembly by name; may throw when it is not installed.</param>
        internal static IPgpProvider Discover(
            IEnumerable<Assembly> loadedAssemblies,
            IEnumerable<string> wellKnownAssemblies,
            Func<string, Assembly> load)
            => Discover(loadedAssemblies, wellKnownAssemblies, load, new List<Exception>());

        /// <param name="errors">Receives the reasons an installed provider could not be used (not the plain "not installed").</param>
        internal static IPgpProvider Discover(
            IEnumerable<Assembly> loadedAssemblies,
            IEnumerable<string> wellKnownAssemblies,
            Func<string, Assembly> load,
            ICollection<Exception> errors)
        {
            // A provider from another load context is only usable when it shares this assembly's IPgpProvider type;
            // CreateProvider checks that. Such an assembly must not count as loaded for the by-name load into this context.
            IPgpProvider found = FindInLoaded(loadedAssemblies, errors, out HashSet<string> loadedNames);
            return found ?? LoadByName(wellKnownAssemblies, loadedNames, load, errors);
        }

        private static IPgpProvider FindInLoaded(IEnumerable<Assembly> loadedAssemblies, ICollection<Exception> errors, out HashSet<string> loadedNames)
        {
            AssemblyLoadContext ownContext = AssemblyLoadContext.GetLoadContext(typeof(PgpProviderAttribute).Assembly);
            loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly assembly in loadedAssemblies)
            {
                if (AssemblyLoadContext.GetLoadContext(assembly) == ownContext)
                {
                    loadedNames.Add(assembly.GetName().Name);
                }

                IPgpProvider provider = References(assembly) ? CreateProvider(assembly, errors) : null;
                if (provider != null)
                {
                    return provider;
                }
            }

            return null;
        }

        private static IPgpProvider LoadByName(IEnumerable<string> names, ISet<string> loadedNames, Func<string, Assembly> load, ICollection<Exception> errors)
        {
            foreach (string name in names.Where(name => !loadedNames.Contains(name)))
            {
                Assembly assembly;
                try
                {
                    assembly = load(name);
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    // Not installed: there is no provider.
                    continue;
                }
                catch (Exception ex)
                {
                    // Installed but not loadable (for example a version mismatch).
                    errors.Add(ex);
                    continue;
                }

                IPgpProvider provider = CreateProvider(assembly, errors);
                if (provider != null)
                {
                    return provider;
                }
            }

            return null;
        }

        // Only assemblies that reference this one can declare a provider; this avoids reading the attributes of every
        // framework assembly.
        private static bool References(Assembly assembly)
        {
            if (assembly == typeof(PgpProviderAttribute).Assembly)
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

        private static IPgpProvider CreateProvider(Assembly assembly, ICollection<Exception> errors)
        {
            PgpProviderAttribute attribute;
            try
            {
                attribute = assembly.GetCustomAttribute<PgpProviderAttribute>();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("The PGP provider of {0} was not read: {1}", assembly.GetName().Name, ex.Message);
                errors.Add(ex);
                return null;
            }

            Type type = attribute?.ProviderType;
            if (type == null)
            {
                return null;
            }

            if (!typeof(IPgpProvider).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface
                || type.GetConstructor(Type.EmptyTypes) == null)
            {
                Trace.TraceWarning(
                    "{0} is declared as the PGP provider of {1} but is not a public, non-abstract class implementing {2} with a parameterless constructor; it is ignored.",
                    type.FullName,
                    assembly.GetName().Name,
                    nameof(IPgpProvider));
                return null;
            }

            try
            {
                return (IPgpProvider)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("The PGP provider {0} could not be created and is ignored: {1}", type.FullName, ex.Message);
                errors.Add(ex);
                return null;
            }
        }
    }
}
