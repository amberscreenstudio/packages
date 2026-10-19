#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Reflection;
using UnityEngine;

namespace AmberScreen.Dev
{
    static class DevConsoleDiscovery
    {
        static readonly string[] s_SkipPrefixes =
        {
            "System", "Unity", "UnityEngine", "UnityEditor", "Mono", "mscorlib",
            "netstandard", "nunit", "Microsoft"
        };

        static bool ShouldSkip(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return true;
            string name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name))
                return true;
            for (int i = 0; i < s_SkipPrefixes.Length; i++)
            {
                if (name.StartsWith(s_SkipPrefixes[i], StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static void DiscoverAttributes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                if (ShouldSkip(assembly))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }

                if (types == null)
                    continue;

                for (int t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null)
                        continue;

                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch { continue; }

                    for (int m = 0; m < methods.Length; m++)
                    {
                        var method = methods[m];
                        var attr = method.GetCustomAttribute<DevButtonAttribute>();
                        if (attr == null)
                            continue;
                        if (method.GetParameters().Length != 0)
                            continue;

                        var captured = method;
                        Action invoke = () =>
                        {
                            try { captured.Invoke(null, null); }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                DevConsole.Note($"DevButton error: {e.Message}");
                            }
                        };
                        DevConsole.Register(attr.Section, attr.Label, invoke);
                        if (attr.Quick)
                            DevConsole.RegisterQuick(attr.Label, invoke);
                    }
                }
            }
        }

        public static void DiscoverProviders()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                if (ShouldSkip(assembly))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }

                if (types == null)
                    continue;

                for (int t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null || type.IsAbstract || type.IsInterface)
                        continue;
                    if (!typeof(IDevConsoleSectionProvider).IsAssignableFrom(type))
                        continue;

                    try
                    {
                        var instance = Activator.CreateInstance(type) as IDevConsoleSectionProvider;
                        instance?.Register();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}
#endif
