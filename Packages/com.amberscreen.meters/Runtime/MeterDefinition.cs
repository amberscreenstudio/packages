using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Meters
{
    /// <summary>
    /// Authoring data for a <see cref="Meter"/>. Games create assets (Stamina, Breath, Coolant…)
    /// — no hard-coded game values live in the module.
    /// </summary>
    [CreateAssetMenu(fileName = "MeterDefinition", menuName = "Amber Screen/Meters/Meter Definition")]
    public class MeterDefinition : ScriptableObject
    {
        [SerializeField] string id = "Meter";
        [SerializeField] float max = 100f;
        [SerializeField] float startValue = 100f;
        [SerializeField] float defaultDrainPerSecond;
        [SerializeField] float defaultFillPerSecond;
        [SerializeField] List<MeterThreshold> thresholds = new();

        public string Id => id;
        public float Max => max;
        public float StartValue => startValue;
        public float DefaultDrainPerSecond => defaultDrainPerSecond;
        public float DefaultFillPerSecond => defaultFillPerSecond;
        public IReadOnlyList<MeterThreshold> Thresholds => thresholds;

#if UNITY_EDITOR
        public void EditorSet(
            string meterId,
            float capacity,
            float start,
            float drain,
            float fill,
            List<MeterThreshold> thresholdList)
        {
            id = meterId;
            max = capacity;
            startValue = start;
            defaultDrainPerSecond = drain;
            defaultFillPerSecond = fill;
            thresholds = thresholdList ?? new List<MeterThreshold>();
        }
#endif
    }
}
