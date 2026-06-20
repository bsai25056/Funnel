using System;
using UnityEngine;

namespace Laboratory.FunnelSystem
{
    [Serializable]
    public sealed class LiquidData
    {
        [SerializeField] private string liquidId = "Unknown";
        [SerializeField, Min(0f)] private float volumeMl;
        [SerializeField] private float temperatureCelsius = 20f;
        
        [SerializeField] private string suspendedSolidType = "None";
        [SerializeField, Min(0f)] private float suspendedSolidMassGrams;
[SerializeField] private Color displayColor = Color.cyan;

        public LiquidData(string liquidId, float volumeMl, float temperatureCelsius, Color displayColor)
            : this(liquidId, volumeMl, temperatureCelsius, displayColor, "None", 0f)
        {
        }

        public LiquidData(
            string liquidId,
            float volumeMl,
            float temperatureCelsius,
            Color displayColor,
            string suspendedSolidType,
            float suspendedSolidMassGrams)
        {
            this.liquidId = string.IsNullOrWhiteSpace(liquidId) ? "Unknown" : liquidId;
            this.volumeMl = Mathf.Max(0f, volumeMl);
            this.temperatureCelsius = temperatureCelsius;
            this.displayColor = displayColor;
            this.suspendedSolidType = string.IsNullOrWhiteSpace(suspendedSolidType)
                ? "None"
                : suspendedSolidType;
            this.suspendedSolidMassGrams = Mathf.Max(0f, suspendedSolidMassGrams);
        }

        public string LiquidId => liquidId;
        public float VolumeMl => volumeMl;
        public float TemperatureCelsius => temperatureCelsius;
        
        public string SuspendedSolidType => suspendedSolidType;
        public float SuspendedSolidMassGrams => suspendedSolidMassGrams;
public Color DisplayColor => displayColor;

public LiquidData CreatePortion(float portionVolumeMl)
        {
            float clampedVolume = Mathf.Clamp(portionVolumeMl, 0f, volumeMl);
            float volumeFraction = volumeMl > 0f ? clampedVolume / volumeMl : 0f;

            return new LiquidData(
                liquidId,
                clampedVolume,
                temperatureCelsius,
                displayColor,
                suspendedSolidType,
                suspendedSolidMassGrams * volumeFraction);
        }

public LiquidData WithSuspendedSolidMass(float newSolidMassGrams)
        {
            return new LiquidData(
                liquidId,
                volumeMl,
                temperatureCelsius,
                displayColor,
                suspendedSolidType,
                Mathf.Max(0f, newSolidMassGrams));
        }

    }

    /// <summary>
    /// Implement this on the Chemical Liquid Simulation System, or on an adapter.
    /// Each call represents one timed portion exiting the funnel stem.
    /// </summary>
    public interface IChemicalLiquidFlowReceiver
    {
        void ReceiveLiquidTick(LiquidData liquidData, Transform exitPort);
    }

    /// <summary>
    /// Implement this on Filter Paper. The filter owns all filtration behavior
    /// and any forwarding of the processed liquid.
    /// </summary>
    public interface IFilterPaperProcessor
    {
        void ProcessLiquid(LiquidData liquidData, Transform funnelExitPort);
    }
}
