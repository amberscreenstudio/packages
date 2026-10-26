using System;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Optional gameplay/input gate set by the host (e.g. DevConsole).
    /// PhoneUI itself does not reference AmberScreen.Dev.
    /// </summary>
    public static class PhoneInputGate
    {
        /// <summary>When true, phone UI should not process taps / open apps.</summary>
        public static Func<bool> ShouldBlockPhoneInput;

        /// <summary>When true, Back/Esc should not navigate the phone (console consumed close).</summary>
        public static Func<bool> ShouldIgnoreBack;

        public static bool BlockPhone => ShouldBlockPhoneInput != null && ShouldBlockPhoneInput();
        public static bool IgnoreBack => ShouldIgnoreBack != null && ShouldIgnoreBack();
    }
}
