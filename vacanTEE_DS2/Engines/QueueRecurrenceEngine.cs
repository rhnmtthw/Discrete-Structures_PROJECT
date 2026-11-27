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
    /// <summary>
    /// Recursively computes the cumulative wait W_(kIndex+1) for <paramref name="sequence"/> and,
    /// as the recursion unwinds, appends one <see cref="ScheduleStep"/> and one trace line per step
    /// to <paramref name="steps"/> / <paramref name="trace"/> in chronological order (step 1 first).
    /// </summary>
    /// <param name="sequence">Slots of one schedule, in step order.</param>
    /// <param name="kIndex">0-based index of the step to evaluate (0 is the base case).</param>
    /// <param name="arrivalMins">Patient arrival time, in minutes from midnight.</param>
    /// <param name="durations">Department code to service duration (S) in minutes.</param>
    /// <param name="steps">Receives the schedule steps; must be empty on the initial call.</param>
    /// <param name="trace">Receives the formula strings; must be empty on the initial call.</param>
    /// <returns>The cumulative wait W_(kIndex+1) in minutes.</returns>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kIndex"/> is outside the sequence.</exception>
    /// <exception cref="ArgumentException">A slot's department has no known duration.</exception>
    public int ComputeRecursiveWait(
        IReadOnlyList<Slot> sequence,
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

        Slot current = sequence[kIndex] ?? throw new ArgumentException($"Step {kIndex + 1} is null.", nameof(sequence));
        if (!durations.TryGetValue(current.Dept, out int currentDuration))
            throw new ArgumentException($"No service duration is defined for department '{current.Dept}'.", nameof(durations));

        int k = kIndex + 1;
        int wait;
        string formula;

        if (kIndex == 0)
        {
            // BASE CASE: W_1 = max(0, Start_1 - T_arrival). No further recursion.
            wait = Math.Max(0, current.StartMins - arrivalMins);
            formula = $"Step 1 ({current.Dept}): W_1 = max(0, {current.StartMins} - {arrivalMins}) = {wait} mins";
        }
        else
        {
            // RECURSIVE STEP: first solve the smaller problem W_(k-1) ...
            int previousWait = ComputeRecursiveWait(sequence, kIndex - 1, arrivalMins, durations, steps, trace);

            // ... then (after the call returns) add this step's idle gap.
            Slot previous = sequence[kIndex - 1];
            int previousEnd = previous.StartMins + durations[previous.Dept];
            int gap = Math.Max(0, current.StartMins - previousEnd);
            wait = previousWait + gap;
            formula = $"Step {k} ({current.Dept}): W_{k} = {previousWait} + max(0, {current.StartMins} - {previousEnd}) = {wait} mins";
        }

        // Recorded after the recursive call returns, so step 1 is added first and step r last.
        steps.Add(new ScheduleStep
        {
            StepNumber = k,
            Slot = current,
            DeptName = ClinicMockData.GetDepartmentNames().GetValueOrDefault(current.Dept, current.Dept),
            TimeRange = ClinicMockData.FormatTimeRange(current.StartMins, currentDuration),
            CumulativeWaitMins = wait,
        });
        trace.Add(formula);

        return wait;
    }

    /// <summary>
    /// Evaluates every valid schedule from Engine 1, ranks them by total wait W_r (ties broken by the
    /// earliest finishing time, then by Engine 1's order) and returns the optimal one with its trace
    /// and induction summary. Failures are reported as <c>Status = "ERROR"</c>, never as exceptions.
    /// </summary>
    /// <param name="engine1">The result produced by <see cref="CombinatoricsEngine.Run"/>.</param>
    /// <param name="arrivalMins">Patient arrival time, in minutes from midnight.</param>
    /// <param name="durations">Optional department durations; defaults to <see cref="ClinicMockData.GetDurations"/>.</param>
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

        foreach (List<Slot> schedule in engine1.ValidSchedules)
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

            Slot last = schedule[^1];
            int finish = last.StartMins + durationMap[last.Dept];

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

    /// <summary>
    /// Builds the Mathematical Induction explanation shown in the UI, chosen by whether the
    /// optimal schedule has zero or positive total wait.
    /// </summary>
    /// <param name="totalWaitMins">The optimal schedule's total wait W_r.</param>
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
