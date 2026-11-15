using System;
using UnityEngine;

namespace AmberScreen.Settings
{
    public enum SettingsCustomKind
    {
        Slider,
        Toggle,
        Stepper
    }

    /// <summary>
    /// A game-defined option shown in the built-in menu and saved with the rest.
    /// Add one with <see cref="GameSettings.AddCustomSlider"/>, <see cref="GameSettings.AddCustomToggle"/> or
    /// <see cref="GameSettings.AddCustomStepper"/>. Values are floats: toggles 0/1, steppers the choice index.
    /// </summary>
    public sealed class SettingsCustomOption
    {
        public string Key { get; internal set; }
        public string Label { get; internal set; }
        /// <summary>Menu heading the row goes under (default "Game").</summary>
        public string Section { get; internal set; }
        public SettingsCustomKind Kind { get; internal set; }
        public float Min { get; internal set; }
        public float Max { get; internal set; }
        public float DefaultValue { get; internal set; }
        public string[] Choices { get; internal set; }
        /// <summary>Text shown next to a slider (default: two decimals).</summary>
        public Func<float, string> Format { get; internal set; }
        internal Action<float> OnChanged;

        /// <summary>Clamps a value to what this option allows.</summary>
        public float Clamp(float value)
        {
            switch (Kind)
            {
                case SettingsCustomKind.Toggle:
                    return value >= 0.5f ? 1f : 0f;
                case SettingsCustomKind.Stepper:
                    int count = Choices != null ? Choices.Length : 0;
                    return count <= 0 ? 0f : Mathf.Clamp(Mathf.RoundToInt(value), 0, count - 1);
                default:
                    return Mathf.Clamp(value, Mathf.Min(Min, Max), Mathf.Max(Min, Max));
            }
        }

        internal string ValueText(float value)
        {
            switch (Kind)
            {
                case SettingsCustomKind.Toggle:
                    return value >= 0.5f ? "On" : "Off";
                case SettingsCustomKind.Stepper:
                    int i = (int)Clamp(value);
                    return Choices != null && i >= 0 && i < Choices.Length ? Choices[i] : "-";
                default:
                    return Format != null ? Format(value) : value.ToString("0.00");
            }
        }
    }
}
