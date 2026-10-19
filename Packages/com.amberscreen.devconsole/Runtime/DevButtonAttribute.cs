using System;
using UnityEngine.Scripting;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Marks a static parameterless method for discovery the first time the console opens.
    /// For IL2CPP, also put <see cref="PreserveAttribute"/> on the method (or use a link.xml).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Preserve]
    public sealed class DevButtonAttribute : Attribute
    {
        public string Section { get; }
        public string Label { get; }

        public DevButtonAttribute(string section, string label)
        {
            Section = section ?? string.Empty;
            Label = label ?? string.Empty;
        }
    }
}
