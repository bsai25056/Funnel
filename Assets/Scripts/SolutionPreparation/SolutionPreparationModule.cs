using System;
using UnityEngine;

namespace SolutionPreparationSystem
{
    /// <summary>
    /// Prefab-friendly wrapper around SolutionPreparationEngine.
    /// Other modules can reference this component and read SolutionPrepared,
    /// ConfirmedMolarity, or the detailed check results.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SolutionPreparationModule : MonoBehaviour
    {
        [Header("Solution Specification")]
        [SerializeField] private string chemicalName = "NaOH";
        [SerializeField, Min(0.000001f)] private float targetMolarity = 0.1f;
        [SerializeField, Min(0.000001f)] private float targetVolumeCm3 = 100f;
        [SerializeField, Min(0.000001f)] private float molarMass = 40f;

        [Header("Weighing Input")]
        [SerializeField, Min(0f)] private float measuredMassGrams = 0.4f;
        [SerializeField, Min(0f)] private float massToleranceGrams = 0.02f;

        [Header("Volume Input")]
        [SerializeField, Min(0f)] private float measuredLiquidLevelCm3 = 100f;
        [SerializeField, Min(0.000001f)] private float calibrationTargetCm3 = 100f;
        [SerializeField, Min(0f)] private float volumeToleranceCm3 = 0.01f;

        [Header("Live Output (read only at runtime)")]
        [SerializeField] private float requiredMassGrams;
        [SerializeField] private bool massCorrect;
        [SerializeField] private float massPercentageError;
        [SerializeField] private bool soluteAdded;
        [SerializeField] private bool waterAdded;
        [SerializeField] private bool stirred;
        [SerializeField] private bool isDissolved;
        [SerializeField] private VolumeAdjustmentStatus volumeStatus;
        [SerializeField] private bool solutionPrepared;
        [SerializeField] private float confirmedMolarity;

        private DissolvingState dissolvingState = new DissolvingState();

        public event Action<SolutionPreparationResult> ResultChanged;

        public string ChemicalName => chemicalName;
        public double TargetMolarity => targetMolarity;
        public double TargetVolumeCm3 => targetVolumeCm3;
        public double MolarMass => molarMass;
        public double RequiredMassGrams => requiredMassGrams;
        public bool MassCorrect => massCorrect;
        public double MassPercentageError => massPercentageError;
        public bool SoluteAdded => soluteAdded;
        public bool WaterAdded => waterAdded;
        public bool Stirred => stirred;
        public bool IsDissolved => isDissolved;
        public VolumeAdjustmentStatus VolumeStatus => volumeStatus;

        // Primary integration outputs for Notebook and Validation modules.
        public bool SolutionPrepared => solutionPrepared;
        public double ConfirmedMolarity => confirmedMolarity;

        public MassCheckResult LastMassCheck { get; private set; }
        public VolumeAdjustmentResult LastVolumeCheck { get; private set; }
        public SolutionPreparationResult LastResult { get; private set; }

        private void Awake()
        {
            ResetPreparation();
        }

        private void OnValidate()
        {
            targetMolarity = Mathf.Max(targetMolarity, 0.000001f);
            targetVolumeCm3 = Mathf.Max(targetVolumeCm3, 0.000001f);
            molarMass = Mathf.Max(molarMass, 0.000001f);
            measuredMassGrams = Mathf.Max(measuredMassGrams, 0f);
            massToleranceGrams = Mathf.Max(massToleranceGrams, 0f);
            measuredLiquidLevelCm3 = Mathf.Max(measuredLiquidLevelCm3, 0f);
            calibrationTargetCm3 = Mathf.Max(calibrationTargetCm3, 0.000001f);
            volumeToleranceCm3 = Mathf.Max(volumeToleranceCm3, 0f);

            Recalculate();
        }

        public void ConfigureSolution(
            string newChemicalName,
            float newTargetMolarity,
            float newTargetVolumeCm3,
            float newMolarMass)
        {
            chemicalName = newChemicalName;
            targetMolarity = Mathf.Max(newTargetMolarity, 0.000001f);
            targetVolumeCm3 = Mathf.Max(newTargetVolumeCm3, 0.000001f);
            molarMass = Mathf.Max(newMolarMass, 0.000001f);
            Recalculate();
        }

        public void SetMeasuredMass(float massGrams)
        {
            measuredMassGrams = Mathf.Max(massGrams, 0f);
            Recalculate();
        }

        public void SetMassTolerance(float toleranceGrams)
        {
            massToleranceGrams = Mathf.Max(toleranceGrams, 0f);
            Recalculate();
        }

        public void SetLiquidLevel(float liquidLevelCm3)
        {
            measuredLiquidLevelCm3 = Mathf.Max(liquidLevelCm3, 0f);
            Recalculate();
        }

        public void SetCalibrationTarget(float targetCm3, float toleranceCm3 = 0.01f)
        {
            calibrationTargetCm3 = Mathf.Max(targetCm3, 0.000001f);
            volumeToleranceCm3 = Mathf.Max(toleranceCm3, 0f);
            Recalculate();
        }

        public void AddSolute()
        {
            dissolvingState.AddSolute();
            Recalculate();
        }

        public bool AddWater()
        {
            bool accepted = dissolvingState.AddWater();
            Recalculate();
            return accepted;
        }

        public bool Stir()
        {
            bool accepted = dissolvingState.Stir();
            Recalculate();
            return accepted;
        }

        public bool MarkDissolved()
        {
            bool accepted = dissolvingState.MarkDissolved();
            Recalculate();
            return accepted;
        }

        public void ResetPreparation()
        {
            dissolvingState.Reset();
            Recalculate();
        }

        public SolutionPreparationResult Recalculate()
        {
            double requiredMass = SolutionPreparationEngine.CalculateRequiredMass(
                targetMolarity,
                targetVolumeCm3,
                molarMass);

            LastMassCheck = SolutionPreparationEngine.CheckMass(
                measuredMassGrams,
                requiredMass,
                massToleranceGrams);

            LastVolumeCheck = SolutionPreparationEngine.CheckVolume(
                measuredLiquidLevelCm3,
                calibrationTargetCm3,
                volumeToleranceCm3);

            LastResult = SolutionPreparationEngine.EvaluatePreparation(
                LastMassCheck,
                dissolvingState.IsDissolved,
                LastVolumeCheck,
                targetMolarity);

            requiredMassGrams = (float)requiredMass;
            massCorrect = LastMassCheck.Passed;
            massPercentageError = (float)LastMassCheck.PercentageError;
            soluteAdded = dissolvingState.SoluteAdded;
            waterAdded = dissolvingState.WaterAdded;
            stirred = dissolvingState.Stirred;
            isDissolved = dissolvingState.IsDissolved;
            volumeStatus = LastVolumeCheck.Status;
            solutionPrepared = LastResult.SolutionPrepared;
            confirmedMolarity = (float)LastResult.ConfirmedMolarity;

            ResultChanged?.Invoke(LastResult);
            return LastResult;
        }
    }
}
