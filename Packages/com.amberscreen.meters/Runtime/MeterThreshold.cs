using System;

namespace AmberScreen.Meters
{
    /// <summary>Named fraction of Max (0..1) that raises cross events once per crossing.</summary>
    [Serializable]
    public struct MeterThreshold
    {
        public string Name;
        public float Normalized;

        public MeterThreshold(string name, float normalized)
        {
            Name = name;
            Normalized = normalized;
        }
    }
}
