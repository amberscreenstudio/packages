using System;

namespace AmberScreen.PhoneUI
{
    public interface IDeviceStatusProvider
    {
        DateTime NowLocal { get; }
        float BatteryPercent { get; }
        int SignalBars { get; }
    }
}
