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

        /// <summary>
        /// Formats an amount with the configured currency symbol (invariant digits).
        /// Negative amounts put the sign before the symbol: "-$1.00".
        /// </summary>
        public static string Format(float amount)
        {
            EnsureLoaded();
            string digits = Math.Abs(amount).ToString(amountFormat, CultureInfo.InvariantCulture);
            return (amount < 0f && !IsZero(digits) ? "-" : "") + symbol + digits;
        }

        /// <summary>
        /// Formats minor units (cents) exactly, without going through float. "-$1.00" for -100.
        /// </summary>
        public static string FormatMinor(long amountMinor)
        {
            EnsureLoaded();
            bool negative = amountMinor < 0;
            // decimal keeps cents exact; long.MinValue cannot be negated, so go through decimal first.
            decimal major = Math.Abs((decimal)amountMinor) / 100m;
            string digits = major.ToString(amountFormat, CultureInfo.InvariantCulture);
            return (negative && !IsZero(digits) ? "-" : "") + symbol + digits;
        }

        /// <summary>True when formatted digits are all zeros (so "-$0.00" is never shown).</summary>
        private static bool IsZero(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return true;
            for (int i = 0; i < digits.Length; i++)
            {
                char c = digits[i];
                if (c >= '1' && c <= '9') return false;
            }
            return true;
        }

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
