namespace vacanTEE_DS2.Models;

/// <summary>
/// Output of the Recursive Wait-Time Engine (Engine 2). Mirrors <see cref="Engine1Result"/> and
/// carries both the Results Section data and the Explanation Section data for the UI.
/// </summary>
public class Engine2Result
{
    /// <summary>"SUCCESS" or "ERROR".</summary>
    public string Status { get; set; } = "SUCCESS";

    /// <summary>Human-readable error description; empty when <see cref="Status"/> is "SUCCESS".</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Ordered steps (1..r) of the schedule with the lowest total wait.</summary>
    public List<ScheduleStep> OptimalSchedule { get; set; } = new();

    /// <summary>Minimum cumulative wait time W_r (in minutes) over all valid schedules.</summary>
    public int TotalWaitMins { get; set; }

    /// <summary>Step-by-step recurrence strings, e.g. "Step 1 (LAB): W_1 = max(0, 480 - 480) = 0 mins".</summary>
    public List<string> RecursiveTrace { get; set; } = new();

    /// <summary>Mathematical Induction verification summary for the UI Explanation Section.</summary>
    public string InductionProofSummary { get; set; } = string.Empty;
}