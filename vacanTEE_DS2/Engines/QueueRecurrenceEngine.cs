using vacanTEE_DS2.Data;
using vacanTEE_DS2.Models;

namespace vacanTEE_DS2.Engines;

/// <summary>
/// Engine 2: Recursive Wait-Time and Schedule Optimization Engine.
/// Takes every valid schedule found by Engine 1, computes its cumulative wait W_k with a
/// true recursive definition, and picks the schedule with the smallest total wait W_r.
/// </summary>
/// <remarks>
/// Recursive definition (k is 1-based; kIndex = k - 1 is 0-based):
/// <code>
/// W_1 = max(0, Start_1 - T_arrival)                                  (base case)
/// W_k = W_(k-1) + max(0, Start_k - (Start_(k-1) + S_(k-1)))          (recursive step, k &gt; 1)
/// </code>
/// </remarks>
public class QueueRecurrenceEngine
{
    public int ComputeRecursiveWait(
        IReadOnlyList<ClinicSlot> sequence,
        int kIndex,
        int arrivalMins,
        IReadOnlyDictionary<string, int> durations,
        List<ScheduleStep> steps,
        List<string> trace)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(durations);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(trace);

        if (kIndex < 0 || kIndex >= sequence.Count)
            throw new ArgumentOutOfRangeException(nameof(kIndex), kIndex, $"kIndex must be between 0 and {sequence.Count - 1}.");

        ClinicSlot current = sequence[kIndex] ?? throw new ArgumentException($"Step {kIndex + 1} is null.", nameof(sequence));
        if (!durations.TryGetValue(current.DeptCode, out int currentDuration))
            throw new ArgumentException($"No service duration is defined for department '{current.DeptCode}'.", nameof(durations));

        int k = kIndex + 1;
        int wait;
        string formula;

        if (kIndex == 0)
        {
            // BASE CASE: W_1 = max(0, Start_1 - T_arrival). No further recursion.
            wait = Math.Max(0, current.StartMins - arrivalMins);
            formula = $"Step 1 ({current.DeptCode}): W_1 = max(0, {current.StartMins} - {arrivalMins}) = {wait} mins";
        }
        else
        {
            // RECURSIVE STEP: first solve the smaller problem W_(k-1) ...
            int previousWait = ComputeRecursiveWait(sequence, kIndex - 1, arrivalMins, durations, steps, trace);

            // ... then (after the call returns) add this step's idle gap.
            ClinicSlot previous = sequence[kIndex - 1];
            int previousEnd = previous.StartMins + durations[previous.DeptCode];
            int gap = Math.Max(0, current.StartMins - previousEnd);
            wait = previousWait + gap;
            formula = $"Step {k} ({current.DeptCode}): W_{k} = {previousWait} + max(0, {current.StartMins} - {previousEnd}) = {wait} mins";
        }

        // Recorded after the recursive call returns, so step 1 is added first and step r last.
        steps.Add(new ScheduleStep
        {
            StepNumber = k,
            Slot = current,
            DeptName = ClinicMockData.GetDepartmentNames().GetValueOrDefault(current.DeptCode, current.DeptCode),
            TimeRange = ClinicMockData.FormatTimeRange(current.StartMins, currentDuration),
            CumulativeWaitMins = wait,
        });
        trace.Add(formula);

        return wait;
    }

    public Engine2Result Run(Engine1Result engine1, int arrivalMins, IReadOnlyDictionary<string, int>? durations = null)
    {
        if (engine1 is null)
            return Error("Engine 1 result is missing.");

        if (engine1.Status == "ERROR")
            return Error($"Engine 1 failed: {engine1.ErrorMessage}");

        if (arrivalMins < 0 || arrivalMins >= 24 * 60)
            return Error($"Arrival time must be between 0 and 1439 minutes from midnight, but was {arrivalMins}.");

        if (engine1.ValidSchedules is null || engine1.ValidSchedules.Count == 0)
            return Error("No valid schedules were found, so no wait time can be computed.");

        IReadOnlyDictionary<string, int> durationMap = durations ?? ClinicMockData.GetDurations();

        Evaluation? best = null;
        int bestFinish = int.MaxValue;

        foreach (List<ClinicSlot> schedule in engine1.ValidSchedules)
        {
            if (schedule is null || schedule.Count == 0)
                return Error("Engine 1 returned an empty schedule.");

            var steps = new List<ScheduleStep>();
            var trace = new List<string>();
            int totalWait;

            try
            {
                totalWait = ComputeRecursiveWait(schedule, schedule.Count - 1, arrivalMins, durationMap, steps, trace);
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }

            ClinicSlot last = schedule[^1];
            int finish = last.StartMins + durationMap[last.DeptCode];

            // Strictly better wait wins; on a tie the earlier finish wins; otherwise the first found stays.
            if (best is null || totalWait < best.TotalWait || (totalWait == best.TotalWait && finish < bestFinish))
            {
                best = new Evaluation(totalWait, steps, trace);
                bestFinish = finish;
            }
        }

        return new Engine2Result
        {
            Status = "SUCCESS",
            OptimalSchedule = best!.Steps,
            TotalWaitMins = best.TotalWait,
            RecursiveTrace = best.Trace,
            InductionProofSummary = GetInductionProofSummary(best.TotalWait),
        };
    }

    public string GetInductionProofSummary(int totalWaitMins)
    {
        if (totalWaitMins < 0)
            throw new ArgumentOutOfRangeException(nameof(totalWaitMins), totalWaitMins, "Total wait cannot be negative.");

        if (totalWaitMins == 0)
        {
            return string.Join(Environment.NewLine,
                "Theorem: if Start_1 = T_arrival and A_(k+1) = S_k for every step, then W_k = 0 for all k >= 1.",
                "Base case (k = 1): W_1 = max(0, Start_1 - T_arrival) = max(0, 0) = 0.",
                "Inductive hypothesis: assume W_k = 0 for some k >= 1.",
                "Inductive step: W_(k+1) = W_k + max(0, Start_(k+1) - (Start_k + S_k)) = 0 + max(0, 0) = 0, because the next slot starts exactly when step k ends (A_(k+1) = S_k).",
                "Conclusion: by Mathematical Induction W_k = 0 for all k >= 1. Verified: this schedule is back-to-back, so the total wait is 0 mins.");
        }

        return string.Join(Environment.NewLine,
            "Claim: W_k >= W_(k-1) for every k > 1 (cumulative wait never decreases).",
            "Base case: W_1 = max(0, Start_1 - T_arrival) >= 0.",
            "Inductive hypothesis: assume W_(k-1) >= 0.",
            "Inductive step: W_k = W_(k-1) + max(0, Start_k - (Start_(k-1) + S_(k-1))), and the max term is >= 0, so W_k >= W_(k-1) >= 0.",
            "Conclusion: by Mathematical Induction every transition gap only adds to the delay. "
            + $"Among all valid schedules, the smallest total wait is W_r = {totalWaitMins} mins.");
    }

    private static Engine2Result Error(string message) => new()
    {
        Status = "ERROR",
        ErrorMessage = message,
    };

    /// <summary>One evaluated schedule: its total wait plus the steps and trace produced while computing it.</summary>
    private sealed record Evaluation(int TotalWait, List<ScheduleStep> Steps, List<string> Trace);
}