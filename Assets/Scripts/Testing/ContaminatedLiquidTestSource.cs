using System.Collections;
using UnityEngine;
using Laboratory.FunnelSystem;
using Laboratory.FunnelSystem.Examples;
using Laboratory.FilterPaperSystem;

namespace Laboratory.Testing
{
    /// <summary>
    /// Test-only source that sends a particle-containing liquid through the
    /// Funnel and Filter Paper when Play Mode starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContaminatedLiquidTestSource : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private FunnelController funnel;
        [SerializeField] private FilterPaperController filterPaper;
        [SerializeField] private FunnelFlowReceiverExample filtrateReceiver;

        [Header("Test Sample")]
        [SerializeField] private FilterPractical practical =
            FilterPractical.Practical6_9_CopperSulphate;
        [SerializeField] private string liquidType = "Copper Sulphate Solution";
        [SerializeField] private string suspendedSolidType = "Solid Impurities";
        [SerializeField, Min(0.01f)] private float sampleVolumeMl = 50f;
        [SerializeField, Min(0f)] private float suspendedSolidMassGrams = 1f;
        [SerializeField, Min(0.01f)] private float flowRateMlPerSecond = 25f;
        [SerializeField] private Color liquidColor =
            new Color(0.1f, 0.45f, 1f, 0.8f);
        [SerializeField] private bool runAutomatically = true;

        [Header("Live Test Result (read only)")]
        [SerializeField] private bool testRunning;
        [SerializeField] private bool testComplete;
        [SerializeField] private bool testPassed;
        [SerializeField] private float measuredResidueGrams;
        [SerializeField] private string measuredResidueType = "None";
        [SerializeField] private float measuredFiltrateVolumeMl;
        [SerializeField] private float filtrateSolidMassGrams;
        [SerializeField] private string resultSummary = "Not run";

        public bool TestRunning => testRunning;
        public bool TestComplete => testComplete;
        public bool TestPassed => testPassed;
        public string ResultSummary => resultSummary;

        private IEnumerator Start()
        {
            Application.runInBackground = true;

            if (!runAutomatically)
                yield break;

            yield return null;
            RunTest();
        }

        public void RunTest()
        {
            if (testRunning)
                return;

            if (funnel == null || filterPaper == null || filtrateReceiver == null)
            {
                resultSummary = "Missing Funnel, Filter Paper, or Filtrate Receiver.";
                testPassed = false;
                testComplete = true;
                Debug.LogError(resultSummary, this);
                return;
            }

            StopAllCoroutines();
            testRunning = true;
            testComplete = false;
            testPassed = false;
            resultSummary = "Filtering contaminated sample...";

            filterPaper.ConfigurePractical(practical);
            filterPaper.ClearResidue();
            filtrateReceiver.ResetReceivedData();
            filterPaper.SetFiltrateReceiver(filtrateReceiver);

            if (!filterPaper.IsAttached ||
                filterPaper.AttachedFunnel != funnel)
            {
                filterPaper.OnSnappedToFunnel(funnel);
            }

            funnel.SetFlowRate(flowRateMlPerSecond);
            funnel.ReceiveLiquid(new LiquidData(
                liquidType,
                sampleVolumeMl,
                20f,
                liquidColor,
                suspendedSolidType,
                suspendedSolidMassGrams));

            StartCoroutine(WaitForResult());
        }

        private IEnumerator WaitForResult()
        {
            while (funnel.IsFlowing || funnel.QueuedVolumeMl > 0.001f)
                yield return null;

            ResidueData residue = filterPaper.GetResidue();
            measuredResidueGrams = residue.AmountGrams;
            measuredResidueType = residue.ResidueType;
            measuredFiltrateVolumeMl = filtrateReceiver.TotalReceivedMl;
            filtrateSolidMassGrams =
                filtrateReceiver.TotalSuspendedSolidMassGrams;

            float expectedResidue = suspendedSolidMassGrams * 0.98f;
            testPassed =
                filterPaper.LastInputMatchedPractical &&
                Mathf.Abs(measuredResidueGrams - expectedResidue) <= 0.01f &&
                Mathf.Abs(measuredFiltrateVolumeMl - sampleVolumeMl) <= 0.01f;

            testRunning = false;
            testComplete = true;
            resultSummary =
                (testPassed ? "PASS" : "FAIL") +
                " | Filtrate: " + measuredFiltrateVolumeMl.ToString("0.00") +
                " mL | Residue: " + measuredResidueGrams.ToString("0.00") +
                " g " + measuredResidueType +
                " | Remaining solids: " +
                filtrateSolidMassGrams.ToString("0.00") + " g";

            Debug.Log(resultSummary, this);
        }
    }
}
