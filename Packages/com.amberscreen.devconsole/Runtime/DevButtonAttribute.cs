using System;

namespace AmberScreen.DevConsole
{
    /// <summary>
    /// Marks a static parameterless method for discovery the first time the console opens.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
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
