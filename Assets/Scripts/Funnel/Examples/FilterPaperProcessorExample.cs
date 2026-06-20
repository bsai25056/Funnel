using UnityEngine;

namespace Laboratory.FunnelSystem.Examples
{
    /// <summary>
    /// Minimal example showing the contract expected from Filter Paper.
    /// The real Filter Paper module should replace this behavior.
    /// </summary>
    public sealed class FilterPaperProcessorExample :
        MonoBehaviour,
        IFilterPaperProcessor
    {
        [SerializeField] private int processedTickCount;
        [SerializeField] private float totalProcessedMl;
        [SerializeField] private Transform lastFunnelExitPort;

        public int ProcessedTickCount => processedTickCount;
        public float TotalProcessedMl => totalProcessedMl;
        public Transform LastFunnelExitPort => lastFunnelExitPort;

        public void ProcessLiquid(LiquidData liquidData, Transform funnelExitPort)
        {
            if (liquidData == null)
                return;

            processedTickCount++;
            totalProcessedMl += liquidData.VolumeMl;
            lastFunnelExitPort = funnelExitPort;
        }
    }
}
