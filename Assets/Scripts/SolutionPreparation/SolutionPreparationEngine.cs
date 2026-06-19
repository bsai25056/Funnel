using System;

namespace SolutionPreparationSystem
{
    public enum VolumeAdjustmentStatus
    {
        Undershoot,
        Correct,
        Overshoot
    }

    public readonly struct MassCheckResult
    {
        public MassCheckResult(
            double requiredMassGrams,
            double measuredMassGrams,
            double toleranceGrams,
            double absoluteErrorGrams,
            double percentageError,
            bool passed)
        {
            RequiredMassGrams = requiredMassGrams;
            MeasuredMassGrams = measuredMassGrams;
            ToleranceGrams = toleranceGrams;
            AbsoluteErrorGrams = absoluteErrorGrams;
            PercentageError = percentageError;
            Passed = passed;
        }

        public double RequiredMassGrams { get; }
        public double MeasuredMassGrams { get; }
        public double ToleranceGrams { get; }
        public double AbsoluteErrorGrams { get; }
        public double PercentageError { get; }
        public bool Passed { get; }
    }

    public readonly struct VolumeAdjustmentResult
    {
        public VolumeAdjustmentResult(
            double measuredVolumeCm3,
            double targetVolumeCm3,
            double toleranceCm3,
            VolumeAdjustmentStatus status)
        {
            MeasuredVolumeCm3 = measuredVolumeCm3;
            TargetVolumeCm3 = targetVolumeCm3;
            ToleranceCm3 = toleranceCm3;
            Status = status;
        }

        public double MeasuredVolumeCm3 { get; }
        public double TargetVolumeCm3 { get; }
        public double ToleranceCm3 { get; }
        public VolumeAdjustmentStatus Status { get; }
        public bool IsCorrect => Status == VolumeAdjustmentStatus.Correct;
    }

    public readonly struct SolutionPreparationResult
    {
        public SolutionPreparationResult(
            bool solutionPrepared,
            double confirmedMolarity,
            MassCheckResult massCheck,
            bool isDissolved,
            VolumeAdjustmentResult volumeCheck)
        {
            SolutionPrepared = solutionPrepared;
            ConfirmedMolarity = confirmedMolarity;
            MassCheck = massCheck;
            IsDissolved = isDissolved;
            VolumeCheck = volumeCheck;
        }

        // This is the main value for Notebook and Validation modules to read.
        public bool SolutionPrepared { get; }

        // Zero until every preparation check passes.
        public double ConfirmedMolarity { get; }
        public MassCheckResult MassCheck { get; }
        public bool IsDissolved { get; }
        public VolumeAdjustmentResult VolumeCheck { get; }
    }

    /// <summary>
    /// Tracks the required dissolving sequence without depending on any scene objects.
    /// A step can only be completed after its prerequisite step.
    /// </summary>
    public sealed class DissolvingState
    {
        public bool SoluteAdded { get; private set; }
        public bool WaterAdded { get; private set; }
        public bool Stirred { get; private set; }
        public bool Dissolved { get; private set; }
        public bool IsDissolved => Dissolved;

        public void AddSolute()
        {
            SoluteAdded = true;
        }

        public bool AddWater()
        {
            if (!SoluteAdded)
                return false;

            WaterAdded = true;
            return true;
        }

        public bool Stir()
        {
            if (!WaterAdded)
                return false;

            Stirred = true;
            return true;
        }

        public bool MarkDissolved()
        {
            if (!Stirred)
                return false;

            Dissolved = true;
            return true;
        }

        public void Reset()
        {
            SoluteAdded = false;
            WaterAdded = false;
            Stirred = false;
            Dissolved = false;
        }
    }

    /// <summary>
    /// Scene-independent calculation and validation logic for molar solution preparation.
    /// </summary>
    public static class SolutionPreparationEngine
    {
        public const double DefaultMassToleranceGrams = 0.02d;
        public const double DefaultCalibrationVolumeCm3 = 100d;
        public const double DefaultVolumeToleranceCm3 = 0.01d;

        public static double CalculateRequiredMass(
            double targetMolarity,
            double targetVolumeCm3,
            double molarMass)
        {
            RequirePositive(targetMolarity, nameof(targetMolarity));
            RequirePositive(targetVolumeCm3, nameof(targetVolumeCm3));
            RequirePositive(molarMass, nameof(molarMass));

            return targetMolarity * (targetVolumeCm3 / 1000d) * molarMass;
        }

        public static MassCheckResult CheckMass(
            double measuredMassGrams,
            double requiredMassGrams,
            double toleranceGrams = DefaultMassToleranceGrams)
        {
            RequireNonNegative(measuredMassGrams, nameof(measuredMassGrams));
            RequirePositive(requiredMassGrams, nameof(requiredMassGrams));
            RequireNonNegative(toleranceGrams, nameof(toleranceGrams));

            double absoluteError = Math.Abs(measuredMassGrams - requiredMassGrams);
            double percentageError = absoluteError / requiredMassGrams * 100d;
            bool passed = absoluteError <= toleranceGrams;

            return new MassCheckResult(
                requiredMassGrams,
                measuredMassGrams,
                toleranceGrams,
                absoluteError,
                percentageError,
                passed);
        }

        public static VolumeAdjustmentResult CheckVolume(
            double measuredVolumeCm3,
            double calibrationTargetCm3 = DefaultCalibrationVolumeCm3,
            double toleranceCm3 = DefaultVolumeToleranceCm3)
        {
            RequireNonNegative(measuredVolumeCm3, nameof(measuredVolumeCm3));
            RequirePositive(calibrationTargetCm3, nameof(calibrationTargetCm3));
            RequireNonNegative(toleranceCm3, nameof(toleranceCm3));

            VolumeAdjustmentStatus status;
            if (measuredVolumeCm3 < calibrationTargetCm3 - toleranceCm3)
                status = VolumeAdjustmentStatus.Undershoot;
            else if (measuredVolumeCm3 > calibrationTargetCm3 + toleranceCm3)
                status = VolumeAdjustmentStatus.Overshoot;
            else
                status = VolumeAdjustmentStatus.Correct;

            return new VolumeAdjustmentResult(
                measuredVolumeCm3,
                calibrationTargetCm3,
                toleranceCm3,
                status);
        }

        public static SolutionPreparationResult EvaluatePreparation(
            MassCheckResult massCheck,
            bool isDissolved,
            VolumeAdjustmentResult volumeCheck,
            double targetMolarity)
        {
            RequirePositive(targetMolarity, nameof(targetMolarity));

            bool solutionPrepared =
                massCheck.Passed &&
                isDissolved &&
                volumeCheck.IsCorrect;

            return new SolutionPreparationResult(
                solutionPrepared,
                solutionPrepared ? targetMolarity : 0d,
                massCheck,
                isDissolved,
                volumeCheck);
        }

        private static void RequirePositive(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
        }

        private static void RequireNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
        }
    }
}
