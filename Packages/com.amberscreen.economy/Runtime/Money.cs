using System;
using System.Globalization;
using UnityEngine;

namespace AmberScreen.Economy
{
    /// <summary>
    /// Single money display path. Never hard-code currency symbols or ToString("C") in games.
    /// Wallet amounts are minor units (cents); display via Format / FormatMinor.
    /// </summary>
    public static class Money
    {
        private const string ResourcesPath = "EconomyCurrency";
        private static string symbol = "€";
        private static string amountFormat = "0";
        private static bool loaded;
        private static bool warnedMissingAsset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => EnsureLoaded();

        public static void Apply(EconomyCurrencySettings settings)
        {
            if (settings == null) return;
            symbol = settings.Symbol;
            amountFormat = settings.AmountFormat;
            loaded = true;
        }

        /// <summary>Major units → minor (cents). Away-from-zero rounding.</summary>
        public static long ToMinor(float major) =>
            (long)Math.Round(major * 100.0, MidpointRounding.AwayFromZero);

        /// <summary>Minor (cents) → major units for display math.</summary>
        public static float FromMinor(long minor) => minor / 100f;

        /// <summary>Formats an amount with the configured currency symbol (invariant digits).</summary>
        public static string Format(float amount)
        {
            EnsureLoaded();
            return symbol + amount.ToString(amountFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>Formats minor units via <see cref="Format"/>.</summary>
        public static string FormatMinor(long amountMinor) => Format(FromMinor(amountMinor));

        private static void EnsureLoaded()
        {
            if (loaded) return;
            var settings = Resources.Load<EconomyCurrencySettings>(ResourcesPath);
            if (settings != null)
            {
                Apply(settings);
                return;
            }

            symbol = "€";
            amountFormat = "0";
            loaded = true;
            if (!warnedMissingAsset)
            {
                warnedMissingAsset = true;
                Debug.LogWarning(
                    "[AmberScreen.Economy] Money: Resources/EconomyCurrency failed to load — using placeholder €.");
            }
        }
    }
}
