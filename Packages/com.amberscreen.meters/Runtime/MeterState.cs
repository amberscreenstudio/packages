using System;

namespace AmberScreen.Meters
{
    /// <summary>
    /// Serializable snapshot of a single meter for save capture/apply.
    /// No dependency on any save system — games store these however they like.
    /// </summary>
    [Serializable]
    public struct MeterState
    {
        public string Id;
        public float Current;
        public float Max;

        public MeterState(string id, float current, float max)
        {
            Id = id;
            Current = current;
            Max = max;
        }
    }
}
