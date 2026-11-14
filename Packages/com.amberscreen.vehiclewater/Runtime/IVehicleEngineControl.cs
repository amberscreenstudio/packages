namespace AmberScreen.VehicleWater
{
    /// <summary>The few engine controls water ingress needs. Implement it over your vehicle controller.</summary>
    public interface IVehicleEngineControl
    {
        bool IsEngineRunning { get; }

        /// <summary>False while the engine is drowned: block the player's ignition.</summary>
        bool CanStart { get; set; }

        /// <summary>Stop the engine now (no throttle, no drive).</summary>
        void Stall();

        /// <summary>Start the engine again. Only called after the water has drained and the game allows it.</summary>
        void Restart();
    }
}
