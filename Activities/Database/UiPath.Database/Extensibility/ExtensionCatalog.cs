using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace UiPath.Database.Extensibility
{
    /// <summary>
    /// Finds the extensions that installed packages bring. An extension is a public class with a parameterless
    /// constructor that an assembly declares with an assembly-level attribute. The catalog never remembers a miss:
    /// Studio and the robot load assemblies lazily, so an assembly that is not loaded now may be loaded by the next
    /// lookup. Found extensions are kept, one instance per type.
    /// </summary>
    /// <typeparam name="TExtension">The interface the extensions implement.</typeparam>
    /// <typeparam name="TAttribute">The assembly attribute that declares an extension.</typeparam>
    public sealed class ExtensionCatalog<TExtension, TAttribute>
        where TExtension : class
        where TAttribute : Attribute
    {
        private readonly object _sync = new object();
        private readonly Dictionary<Type, TExtension> _known = new Dictionary<Type, TExtension>();
        private readonly Func<TAttribute, Type> _typeOf;
        private readonly string[] _wellKnownAssemblies;
        private readonly string _ownerAssemblyName;
        private readonly Func<IEnumerable<Assembly>> _loadedAssemblies;
        private readonly Func<string, Assembly> _load;

        /// <param name="typeOf">Reads the extension type from the attribute.</param>
        /// <param name="wellKnownAssemblies">
        /// Names of assemblies to load when they are not loaded yet. Loading only succeeds when the assembly is
        /// installed; a missing assembly simply means there is no extension.
        /// </param>
        /// <param name="ownerAssembly">
        /// The assembly that defines the attribute. Only assemblies that reference it can declare an extension, which
        /// avoids reading the attributes of every framework assembly.
        /// </param>
        public ExtensionCatalog(Func<TAttribute, Type> typeOf, IEnumerable<string> wellKnownAssemblies, Assembly ownerAssembly)
            : this(
                typeOf,
                wellKnownAssemblies,
                ownerAssembly,
                () => AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic),
                name => Assembly.Load(new AssemblyName(name)))
        {
        }

        /// <summary>For tests: the assemblies to look at and the way to load one by name are supplied.</summary>
        internal ExtensionCatalog(
            Func<TAttribute, Type> typeOf,
            IEnumerable<string> wellKnownAssemblies,
            Assembly ownerAssembly,
            Func<IEnumerable<Assembly>> loadedAssemblies,
            Func<string, Assembly> load)
        {
            _typeOf = typeOf;
            _wellKnownAssemblies = wellKnownAssemblies.ToArray();
            OwnerAssembly = ownerAssembly;
            _ownerAssemblyName = ownerAssembly.GetName().Name;
            _loadedAssemblies = loadedAssemblies;
            _load = load;
        }

        private Assembly OwnerAssembly { get; }

        /// <summary>Returns every extension that is installed now.</summary>
        public IReadOnlyList<TExtension> GetAll()
        {
            Refresh();
            return Snapshot();
        }

        /// <summary>
        /// Returns the first extension that matches, looking again at the loaded assemblies when none of the known ones
        /// does. Returns null when there is none.
        /// </summary>
        public TExtension Find(Func<TExtension, bool> match)
        {
            TExtension found = Match(match);
            if (found != null)
            {
                return found;
            }

            Refresh();
            return Match(match);
        }

        private List<TExtension> Snapshot()
        {
            lock (_sync)
            {
                return _known.Values.ToList();
            }
        }

        private TExtension Match(Func<TExtension, bool> match)
        {
            foreach (TExtension extension in Snapshot())
            {
                try
                {
                    if (match(extension))
                    {
                        return extension;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("The extension {0} failed a lookup and is skipped: {1}", extension.GetType().FullName, ex.Message);
                }
            }

            return null;
        }

        private void Refresh()
        {
            var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly assembly in _loadedAssemblies())
            {
                loadedNames.Add(assembly.GetName().Name);
                if (References(assembly))
                {
                    AddExtensions(assembly);
                }
            }

            // A well-known assembly that is installed but not loaded yet (assemblies load lazily) is loaded by name.
            foreach (string name in _wellKnownAssemblies)
            {
                if (loadedNames.Contains(name))
                {
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = _load(name);
                }
                catch (Exception)
                {
                    // Not installed (or not loadable): there is no extension.
                    continue;
                }

                AddExtensions(assembly);
            }
        }

        private bool References(Assembly assembly)
        {
            if (assembly == OwnerAssembly)
            {
                return false;
            }

            try
            {
                return assembly.GetReferencedAssemblies().Any(reference => reference.Name == _ownerAssemblyName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void AddExtensions(Assembly assembly)
        {
            List<TAttribute> attributes;
            try
            {
                attributes = assembly.GetCustomAttributes<TAttribute>().ToList();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Extensions of {0} were not read: {1}", assembly.GetName().Name, ex.Message);
                return;
            }

            foreach (TAttribute attribute in attributes)
            {
                Type type = _typeOf(attribute);
                if (type == null)
                {
                    continue;
                }

                lock (_sync)
                {
                    if (_known.ContainsKey(type))
                    {
                        continue;
                    }
                }

                if (!typeof(TExtension).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface
                    || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Trace.TraceWarning(
                        "{0} is declared as an extension by {1} but is not a public, non-abstract class implementing {2} with a parameterless constructor; it is ignored.",
                        type.FullName,
                        assembly.GetName().Name,
                        typeof(TExtension).Name);
                    continue;
                }

                try
                {
                    var extension = (TExtension)Activator.CreateInstance(type);
                    lock (_sync)
                    {
                        if (!_known.ContainsKey(type))
                        {
                            _known.Add(type, extension);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("The extension {0} could not be created and is ignored: {1}", type.FullName, ex.Message);
                }
            }
        }
    }
}
