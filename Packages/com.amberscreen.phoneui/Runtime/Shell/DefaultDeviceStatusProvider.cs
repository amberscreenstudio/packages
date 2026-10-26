using System;
using UnityEngine;

namespace AmberScreen.PhoneUI
{
    public sealed class DefaultDeviceStatusProvider : IDeviceStatusProvider
    {
        public DateTime NowLocal => DateTime.Now;
        public float BatteryPercent => Mathf.Clamp01(SystemInfo.batteryLevel < 0f ? 0.82f : SystemInfo.batteryLevel);
        public int SignalBars => 4;
    }
}
