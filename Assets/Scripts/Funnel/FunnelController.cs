using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.FunnelSystem
{
    [DisallowMultipleComponent]
    public sealed class FunnelController : MonoBehaviour
    {
        [Header("Connection Ports")]
        [SerializeField] private Transform topOpeningPort;
        [SerializeField] private Transform bottomStemPort;

        [Header("Flow")]
        [SerializeField, Min(0.01f)] private float flowRateMlPerSecond = 25f;
        [SerializeField] private MonoBehaviour chemicalLiquidSimulationReceiver;

        [Header("Filter Attachment")]
        [SerializeField] private bool hasFilterPaper;
        [SerializeField] private MonoBehaviour filterPaperProcessor;

        [Header("Live State (read only)")]
        [SerializeField] private bool isFlowing;
        [SerializeField] private float queuedVolumeMl;

        private readonly Queue<LiquidData> pendingLiquid = new Queue<LiquidData>();
        private Coroutine flowCoroutine;
        private bool hasWarnedAboutMissingEndpoint;

        public event Action<LiquidData, Transform> LiquidTickExited;
        public event Action<LiquidData, Transform> LiquidTickSentToFilter;

        public Transform TopOpeningPort => topOpeningPort;
        public Transform BottomStemPort => bottomStemPort;
        public float FlowRateMlPerSecond => flowRateMlPerSecond;
        public bool IsFlowing => isFlowing;
        public float QueuedVolumeMl => queuedVolumeMl;

        private void OnEnable()
        {
            StartFlowIfNeeded();
        }

        private void OnDisable()
        {
            if (flowCoroutine != null)
                StopCoroutine(flowCoroutine);

            flowCoroutine = null;
            isFlowing = false;
        }

        private void OnValidate()
        {
            flowRateMlPerSecond = Mathf.Max(0.01f, flowRateMlPerSecond);
        }

        /// <summary>
        /// Adds liquid to the funnel queue and begins gradual flow.
        /// Multiple calls are processed in the order received.
        /// </summary>
        public void ReceiveLiquid(LiquidData liquidData)
        {
            if (liquidData == null)
                throw new ArgumentNullException(nameof(liquidData));

            if (liquidData.VolumeMl <= 0f)
                return;

            pendingLiquid.Enqueue(liquidData.CreatePortion(liquidData.VolumeMl));
            queuedVolumeMl += liquidData.VolumeMl;
            StartFlowIfNeeded();
        }

        public bool IsFilterAttached()
        {
            return hasFilterPaper;
        }

        /// <summary>
        /// Called by the shared Snap & Connection System.
        /// SetFilterPaperProcessor should also be called when attaching a real filter.
        /// </summary>
        public void SetFilterAttached(bool attached)
        {
            hasFilterPaper = attached;
            hasWarnedAboutMissingEndpoint = false;
        }

        /// <summary>
        /// Convenience overload for a Snap system that has the attached component.
        /// </summary>
        public void SetFilterAttached(bool attached, MonoBehaviour attachedFilterProcessor)
        {
            SetFilterPaperProcessor(attachedFilterProcessor);
            SetFilterAttached(attached);
        }

        public bool SetFilterPaperProcessor(MonoBehaviour processor)
        {
            if (processor != null && !(processor is IFilterPaperProcessor))
            {
                Debug.LogError(
                    "Attached filter must implement IFilterPaperProcessor.",
                    processor);
                return false;
            }

            filterPaperProcessor = processor;
            hasWarnedAboutMissingEndpoint = false;
            return true;
        }

        public bool SetLiquidSimulationReceiver(MonoBehaviour receiver)
        {
            if (receiver != null && !(receiver is IChemicalLiquidFlowReceiver))
            {
                Debug.LogError(
                    "Liquid simulation receiver must implement IChemicalLiquidFlowReceiver.",
                    receiver);
                return false;
            }

            chemicalLiquidSimulationReceiver = receiver;
            hasWarnedAboutMissingEndpoint = false;
            return true;
        }

        public void SetFlowRate(float millilitresPerSecond)
        {
            flowRateMlPerSecond = Mathf.Max(0.01f, millilitresPerSecond);
        }

        private void StartFlowIfNeeded()
        {
            if (!isActiveAndEnabled || flowCoroutine != null || pendingLiquid.Count == 0)
                return;

            flowCoroutine = StartCoroutine(FlowQueuedLiquid());
        }

        private IEnumerator FlowQueuedLiquid()
        {
            isFlowing = true;

            while (pendingLiquid.Count > 0)
            {
                LiquidData currentLiquid = pendingLiquid.Dequeue();
                float remainingVolume = currentLiquid.VolumeMl;

                while (remainingVolume > 0.0001f)
                {
                    float frameCapacity = flowRateMlPerSecond * Time.deltaTime;
                    if (frameCapacity <= 0f)
                    {
                        yield return null;
                        continue;
                    }

                    float tickVolume = Mathf.Min(remainingVolume, frameCapacity);
                    LiquidData tick = currentLiquid.CreatePortion(tickVolume);

                    if (!TryDispatchTick(tick))
                    {
                        yield return null;
                        continue;
                    }

                    remainingVolume -= tickVolume;
                    queuedVolumeMl = Mathf.Max(0f, queuedVolumeMl - tickVolume);
                    hasWarnedAboutMissingEndpoint = false;
                    yield return null;
                }
            }

            queuedVolumeMl = 0f;
            isFlowing = false;
            flowCoroutine = null;
        }

        private bool TryDispatchTick(LiquidData tick)
        {
            if (bottomStemPort == null)
            {
                WarnOnce("Funnel cannot output liquid because BottomStemPort is not assigned.");
                return false;
            }

            if (hasFilterPaper)
            {
                IFilterPaperProcessor processor = filterPaperProcessor as IFilterPaperProcessor;
                bool hasEventReceiver = LiquidTickSentToFilter != null;

                if (processor == null && !hasEventReceiver)
                {
                    WarnOnce(
                        "Filter paper is marked attached, but no IFilterPaperProcessor is connected. " +
                        "Liquid is being held in the funnel.");
                    return false;
                }

                processor?.ProcessLiquid(tick, bottomStemPort);
                LiquidTickSentToFilter?.Invoke(tick, bottomStemPort);
                return true;
            }

            IChemicalLiquidFlowReceiver receiver =
                chemicalLiquidSimulationReceiver as IChemicalLiquidFlowReceiver;
            bool hasDirectEventReceiver = LiquidTickExited != null;

            if (receiver == null && !hasDirectEventReceiver)
            {
                WarnOnce(
                    "No Chemical Liquid Simulation receiver is connected. " +
                    "Liquid is being held in the funnel.");
                return false;
            }

            receiver?.ReceiveLiquidTick(tick, bottomStemPort);
            LiquidTickExited?.Invoke(tick, bottomStemPort);
            return true;
        }

        private void WarnOnce(string message)
        {
            if (hasWarnedAboutMissingEndpoint)
                return;

            Debug.LogWarning(message, this);
            hasWarnedAboutMissingEndpoint = true;
        }
    }
}
