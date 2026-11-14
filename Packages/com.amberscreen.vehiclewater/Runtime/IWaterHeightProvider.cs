using UnityEngine;

namespace AmberScreen.VehicleWater
{
    /// <summary>Where the water surface is. Implement this over your water asset (see README for an example).</summary>
    public interface IWaterHeightProvider
    {
        /// <summary>World-space height of the water surface above/below this point, or
        /// <see cref="float.NegativeInfinity"/> when there is no water here.</summary>
        float GetWaterHeight(Vector3 worldPosition);
    }
}
