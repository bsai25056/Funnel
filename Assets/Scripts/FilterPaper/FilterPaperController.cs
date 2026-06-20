using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.FunnelSystem;

namespace Laboratory.FilterPaperSystem
{
    public enum FilterPractical
    {
        Practical6_9_CopperSulphate,
        Practical15_1_SoftenedWater,
        Custom
    }

    [Serializable]
    public sealed class ResidueData
    {
        public ResidueData(string residueType, float amountGrams)
        {
            ResidueType = string.IsNullOrWhiteSpace(residueType) ? "None" : residueType;
            AmountGrams = Mathf.Max(0f, amountGrams);
        }

        public string ResidueType { get; }
        public float AmountGrams { get; }
    }

    [DisallowMultipleComponent]
    public sealed class FilterPaperController :
        MonoBehaviour,
        IFilterPaperProcessor
    {
        [Header("Practical")]
        [SerializeField] private FilterPractical practical =
            FilterPractical.Practical6_9_CopperSulphate;
        [SerializeField] private string expectedLiquidType = "Copper Sulphate Solution";
        [SerializeField] private string expectedResidueType = "Solid Impurities";
        [SerializeField, Range(0f, 1f)] private float retentionEfficiency = 0.98f;

        [Header("Connection Points")]
        [SerializeField] private Transform snapAnchor;
        [SerializeField] private Transform filtrateExitPort;

        [Header("Optional Starting Connection")]
        [SerializeField] private FunnelController startingFunnel;
        [SerializeField] private bool attachOnStart;

        [Header("Filtrate Output")]
        [SerializeField] private MonoBehaviour filtrateReceiver;

        [Header("Live State (read only)")]
        [SerializeField] private bool isAttached;
        [SerializeField] private string accumulatedResidueType = "None";
        [SerializeField, Min(0f)] private float accumulatedResidueGrams;
        [SerializeField, Min(0f)] private float totalFiltrateVolumeMl;
        [SerializeField] private bool lastInputMatchedPractical;

        private FunnelController attachedFunnel;
        private readonly Queue<PendingFiltrate> pendingFiltrate =
            new Queue<PendingFiltrate>();
        private bool hasWarnedAboutMissingReceiver;

        public event Action<LiquidData, Transform> FiltrateReady;
        public event Action<ResidueData> ResidueChanged;

        public FilterPractical Practical => practical;
        public Transform SnapAnchor => snapAnchor;
        public Transform FiltrateExitPort => filtrateExitPort;
        public FunnelController AttachedFunnel => attachedFunnel;
        public bool IsAttached => isAttached;
        public float TotalFiltrateVolumeMl => totalFiltrateVolumeMl;
        public bool LastInputMatchedPractical => lastInputMatchedPractical;

        private struct PendingFiltrate
        {
            public PendingFiltrate(LiquidData liquid, Transform exitPort)
            {
                Liquid = liquid;
                ExitPort = exitPort;
            }

            public LiquidData Liquid { get; }
            public Transform ExitPort { get; }
        }

        private void Start()
        {
            if (attachOnStart && startingFunnel != null)
                OnSnappedToFunnel(startingFunnel);
        }

        private void OnValidate()
        {
            retentionEfficiency = Mathf.Clamp01(retentionEfficiency);
        }

        public void ConfigurePractical(FilterPractical newPractical)
        {
            practical = newPractical;

            switch (practical)
            {
                case FilterPractical.Practical6_9_CopperSulphate:
                    expectedLiquidType = "Copper Sulphate Solution";
                    expectedResidueType = "Solid Impurities";
                    retentionEfficiency = 0.98f;
                    break;

                case FilterPractical.Practical15_1_SoftenedWater:
                    expectedLiquidType = "Softened Water";
                    expectedResidueType = "Calcium Precipitate";
                    retentionEfficiency = 0.98f;
                    break;
            }
        }

        public void ConfigureCustomFilter(
            string liquidType,
            string residueType,
            float efficiency)
        {
            practical = FilterPractical.Custom;
            expectedLiquidType = string.IsNullOrWhiteSpace(liquidType)
                ? "Unknown Liquid"
                : liquidType;
            expectedResidueType = string.IsNullOrWhiteSpace(residueType)
                ? "Solid Residue"
                : residueType;
            retentionEfficiency = Mathf.Clamp01(efficiency);
        }

        /// <summary>
        /// Called by the shared Snap & Connection System when placed on a funnel.
        /// </summary>
        public void OnSnappedToFunnel(FunnelController funnel)
        {
            if (funnel == null)
                throw new ArgumentNullException(nameof(funnel));

            if (attachedFunnel != null && attachedFunnel != funnel)
                OnDetachedFromFunnel();

            
            transform.SetParent(funnel.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
attachedFunnel = funnel;
            isAttached = true;
            funnel.SetFilterAttached(true, this);
        }

        /// <summary>
        /// Called by the shared Snap & Connection System when removed.
        /// </summary>
        public void OnDetachedFromFunnel()
        {
            if (attachedFunnel != null)
            {
                attachedFunnel.SetFilterAttached(false);
                attachedFunnel.SetFilterPaperProcessor(null);
            }

            
            transform.SetParent(null, true);
attachedFunnel = null;
            isAttached = false;
        }

        public bool SetFiltrateReceiver(MonoBehaviour receiver)
        {
            if (receiver != null && !(receiver is IChemicalLiquidFlowReceiver))
            {
                Debug.LogError(
                    "Filtrate receiver must implement IChemicalLiquidFlowReceiver.",
                    receiver);
                return false;
            }

            filtrateReceiver = receiver;
            hasWarnedAboutMissingReceiver = false;
            FlushPendingFiltrate();
            return true;
        }

        /// <summary>
        /// Called once for every gradual liquid tick produced by the Funnel.
        /// </summary>
        public void ProcessLiquid(LiquidData liquidData, Transform funnelExitPort)
        {
            if (liquidData == null)
                throw new ArgumentNullException(nameof(liquidData));

            float incomingSolidMass = liquidData.SuspendedSolidMassGrams;
            float capturedSolidMass = incomingSolidMass * retentionEfficiency;
            float remainingSolidMass = incomingSolidMass - capturedSolidMass;

            lastInputMatchedPractical =
                string.Equals(
                    liquidData.SuspendedSolidType,
                    expectedResidueType,
                    StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(
                    liquidData.LiquidId,
                    expectedLiquidType,
                    StringComparison.OrdinalIgnoreCase) ||
                 string.IsNullOrWhiteSpace(expectedLiquidType));

            if (capturedSolidMass > 0f)
                AccumulateResidue(liquidData.SuspendedSolidType, capturedSolidMass);

            LiquidData filtrate =
                liquidData.WithSuspendedSolidMass(remainingSolidMass);
            Transform outputPort =
                filtrateExitPort != null ? filtrateExitPort : funnelExitPort;

            totalFiltrateVolumeMl += filtrate.VolumeMl;
            ForwardOrQueueFiltrate(filtrate, outputPort);
        }

        public ResidueData GetResidue()
        {
            return new ResidueData(
                accumulatedResidueType,
                accumulatedResidueGrams);
        }

        public void ClearResidue()
        {
            accumulatedResidueType = "None";
            accumulatedResidueGrams = 0f;
            ResidueChanged?.Invoke(GetResidue());
        }

        private void AccumulateResidue(string residueType, float amountGrams)
        {
            string normalizedType = string.IsNullOrWhiteSpace(residueType)
                ? expectedResidueType
                : residueType;

            if (accumulatedResidueGrams <= 0f ||
                accumulatedResidueType == "None")
            {
                accumulatedResidueType = normalizedType;
            }
            else if (!string.Equals(
                accumulatedResidueType,
                normalizedType,
                StringComparison.OrdinalIgnoreCase))
            {
                accumulatedResidueType = "Mixed Residue";
            }

            accumulatedResidueGrams += amountGrams;
            ResidueChanged?.Invoke(GetResidue());
        }

        private void ForwardOrQueueFiltrate(
            LiquidData filtrate,
            Transform outputPort)
        {
            IChemicalLiquidFlowReceiver receiver =
                filtrateReceiver as IChemicalLiquidFlowReceiver;
            bool hasEventReceiver = FiltrateReady != null;

            if (receiver == null && !hasEventReceiver)
            {
                pendingFiltrate.Enqueue(
                    new PendingFiltrate(filtrate, outputPort));

                if (!hasWarnedAboutMissingReceiver)
                {
                    Debug.LogWarning(
                        "Filter Paper has no filtrate receiver. Clean liquid is queued.",
                        this);
                    hasWarnedAboutMissingReceiver = true;
                }

                return;
            }

            receiver?.ReceiveLiquidTick(filtrate, outputPort);
            FiltrateReady?.Invoke(filtrate, outputPort);
        }

        private void FlushPendingFiltrate()
        {
            IChemicalLiquidFlowReceiver receiver =
                filtrateReceiver as IChemicalLiquidFlowReceiver;

            if (receiver == null)
                return;

            while (pendingFiltrate.Count > 0)
            {
                PendingFiltrate pending = pendingFiltrate.Dequeue();
                receiver.ReceiveLiquidTick(
                    pending.Liquid,
                    pending.ExitPort);
                FiltrateReady?.Invoke(
                    pending.Liquid,
                    pending.ExitPort);
            }
        }
    }
}
