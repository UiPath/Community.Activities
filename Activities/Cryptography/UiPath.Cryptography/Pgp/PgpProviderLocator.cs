using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

            provider = Discover(
                AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic),
                WellKnownAssemblies,
                name => Assembly.Load(new AssemblyName(name)));
            if (provider == null)
            {
                throw new InvalidOperationException(Resources.PgpProviderNotInstalled);
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
        {
            var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly assembly in loadedAssemblies)
            {
                loadedNames.Add(assembly.GetName().Name);
                if (References(assembly))
                {
                    IPgpProvider provider = CreateProvider(assembly);
                    if (provider != null)
                    {
                        return provider;
                    }
                }
            }

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
                    // Not installed (or not loadable): there is no provider.
                    continue;
                }

                IPgpProvider provider = CreateProvider(assembly);
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

        private static IPgpProvider CreateProvider(Assembly assembly)
        {
            PgpProviderAttribute attribute;
            try
            {
                attribute = assembly.GetCustomAttribute<PgpProviderAttribute>();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("The PGP provider of {0} was not read: {1}", assembly.GetName().Name, ex.Message);
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
                return null;
            }
        }
    }
}
