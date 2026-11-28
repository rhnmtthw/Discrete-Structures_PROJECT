namespace vacanTEE_DS2.Models;

/// <summary>
/// One scheduled procedure step in the optimal patient itinerary produced by Engine 2.
/// </summary>
public class ScheduleStep
{
    /// <summary>1-based position of this step in the itinerary (1, 2, ..., r).</summary>
    public int StepNumber { get; set; }

    /// <summary>The clinic slot (from Partner 1's <see cref="ClinicSlot"/> model) booked for this step.</summary>
    public ClinicSlot Slot { get; set; } = new();

    /// <summary>Full UP-PGH department name, ready for display in the UI.</summary>
    public string DeptName { get; set; } = string.Empty;

    /// <summary>Formatted appointment window, e.g. "08:00 AM - 08:15 AM".</summary>
    public string TimeRange { get; set; } = string.Empty;

    /// <summary>Cumulative wait/idle delay W_k (in minutes) accumulated up to and including this step.</summary>
    public int CumulativeWaitMins { get; set; }
}