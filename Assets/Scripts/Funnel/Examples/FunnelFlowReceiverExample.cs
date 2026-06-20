using UnityEngine;

namespace Laboratory.FunnelSystem.Examples
{
    /// <summary>
    /// Minimal receiver showing how the Chemical Liquid Simulation System
    /// connects to FunnelController. Replace this with the real renderer.
    /// </summary>
    public sealed class FunnelFlowReceiverExample :
        MonoBehaviour,
        IChemicalLiquidFlowReceiver
    {
        [SerializeField] private int receivedTickCount;
        
        [SerializeField] private float totalSuspendedSolidMassGrams;
[SerializeField] private float totalReceivedMl;
        [SerializeField] private Transform lastExitPort;

        public int ReceivedTickCount => receivedTickCount;
        
        public float TotalSuspendedSolidMassGrams => totalSuspendedSolidMassGrams;
public float TotalReceivedMl => totalReceivedMl;
        public Transform LastExitPort => lastExitPort;

        public void ReceiveLiquidTick(LiquidData liquidData, Transform exitPort)
        {
            if (liquidData == null)
                return;

            receivedTickCount++;
            
            totalSuspendedSolidMassGrams += liquidData.SuspendedSolidMassGrams;
totalReceivedMl += liquidData.VolumeMl;
            lastExitPort = exitPort;
        }

        public void ResetReceivedData()
        {
            receivedTickCount = 0;
            
            totalSuspendedSolidMassGrams = 0f;
totalReceivedMl = 0f;
            lastExitPort = null;
        }
    }
}
