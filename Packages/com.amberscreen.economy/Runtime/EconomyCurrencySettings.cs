using UnityEngine;

namespace AmberScreen.Economy
{
    /// <summary>
    /// Currency display settings. Default € is a PLACEHOLDER — each game may override.
    /// </summary>
    [CreateAssetMenu(menuName = "AmberScreen/Economy/Currency Settings", fileName = "EconomyCurrency")]
    public sealed class EconomyCurrencySettings : ScriptableObject
    {
        [SerializeField, Tooltip("Placeholder € until the game picks a final symbol.")]
        private string symbol = "€";

        [SerializeField, Tooltip("Invariant number format appended after the symbol (no culture 'C').")]
        private string amountFormat = "0";

        public string Symbol => string.IsNullOrEmpty(symbol) ? "€" : symbol;
        public string AmountFormat => string.IsNullOrEmpty(amountFormat) ? "0" : amountFormat;
    }
}
