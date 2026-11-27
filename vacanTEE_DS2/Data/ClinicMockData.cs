using System.Globalization;
using vacanTEE_DS2.Models;

namespace vacanTEE_DS2.Data;

/// <summary>
/// Static mock clinic dataset modelled on UP-PGH (Philippine General Hospital) outpatient services,
/// plus helpers to convert between "minutes from midnight" and clock text.
/// All times are integer minutes from midnight (480 = 08:00 AM) so overlap and wait
/// calculations are plain integer arithmetic.
/// </summary>
public static class ClinicMockData
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>Returns the five UP-PGH outpatient departments with their service durations.</summary>
    public static List<Department> GetDepartments() => new()
    {
        new Department { Code = "LAB", Name = "Central Laboratories – Phlebotomist", DurationMins = 15 },
        new Department { Code = "ECG", Name = "Dept. of Medicine – OPD ECG Unit", DurationMins = 15 },
        new Department { Code = "RAD", Name = "Dept. of Radiology – Outpatient X-Ray", DurationMins = 20 },
        new Department { Code = "PUL", Name = "Dept. of Medicine – Pulmonary (OPD Spirometry)", DurationMins = 25 },
        new Department { Code = "CON", Name = "Dept. of Family & Community Medicine – OPD Consult", DurationMins = 30 },
    };

    /// <summary>Returns department code to service duration in minutes (compatible with Partner 1's duration dictionary).</summary>
    public static Dictionary<string, int> GetDurations() =>
        GetDepartments().ToDictionary(d => d.Code, d => d.DurationMins);

    /// <summary>Returns department code to full UP-PGH department name.</summary>
    public static Dictionary<string, string> GetDepartmentNames() =>
        GetDepartments().ToDictionary(d => d.Code, d => d.Name);

    /// <summary>
    /// Returns the n = 10 open slots: six in the Morning OPD block (8AM-12NN)
    /// and four in the Afternoon OPD block (1PM-4PM).
    /// </summary>
    public static List<Slot> GetOpenSlots()
    {
        (string Id, string Dept, int Start)[] raw =
        {
            // Morning OPD
            ("PGH-LAB-01", "LAB", 480),
            ("PGH-RAD-01", "RAD", 500),
            ("PGH-CON-01", "CON", 525),
            ("PGH-ECG-01", "ECG", 540),
            ("PGH-LAB-02", "LAB", 570),
            ("PGH-PUL-01", "PUL", 600),
            // Afternoon OPD (ECG-02 ends at 795, exactly when CON-02 starts: a 0-minute wait)
            ("PGH-ECG-02", "ECG", 780),
            ("PGH-CON-02", "CON", 795),
            ("PGH-RAD-02", "RAD", 840),
            ("PGH-CON-03", "CON", 870),
        };

        return raw
            .Select(s => new Slot
            {
                SlotId = s.Id,
                Dept = s.Dept,
                StartMins = s.Start,
                Label = FormatMinutesTo12Hour(s.Start),
            })
            .ToList();
    }

    /// <summary>
    /// Converts minutes from midnight to 12-hour text, e.g. 480 to "08:00 AM", 780 to "01:00 PM".
    /// Values of 1440 or more wrap past midnight.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="totalMins"/> is negative.</exception>
    public static string FormatMinutesTo12Hour(int totalMins)
    {
        if (totalMins < 0)
            throw new ArgumentOutOfRangeException(nameof(totalMins), totalMins, "Minutes from midnight cannot be negative.");

        int minuteOfDay = totalMins % MinutesPerDay;
        int hour24 = minuteOfDay / 60;
        int minute = minuteOfDay % 60;
        string suffix = hour24 < 12 ? "AM" : "PM";
        int hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;

        return string.Create(CultureInfo.InvariantCulture, $"{hour12:00}:{minute:00} {suffix}");
    }

    /// <summary>
    /// Formats an appointment window, e.g. (480, 15) to "08:00 AM - 08:15 AM".
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative start or a non-positive duration.</exception>
    public static string FormatTimeRange(int startMins, int durationMins)
    {
        if (startMins < 0)
            throw new ArgumentOutOfRangeException(nameof(startMins), startMins, "Start time cannot be negative.");
        if (durationMins <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMins), durationMins, "Duration must be positive.");

        return $"{FormatMinutesTo12Hour(startMins)} - {FormatMinutesTo12Hour(startMins + durationMins)}";
    }

    /// <summary>
    /// Converts 24-hour "HH:mm" UI input (e.g. "08:50") to minutes from midnight.
    /// Returns -1 for null, blank, malformed or out-of-range input.
    /// </summary>
    public static int ParseTimeInputToMinutes(string? hhMm)
    {
        if (string.IsNullOrWhiteSpace(hhMm)) return -1;

        string[] parts = hhMm.Trim().Split(':');
        if (parts.Length != 2) return -1;
        if (parts[0].Length is < 1 or > 2 || parts[1].Length != 2) return -1;

        // NumberStyles.None rejects signs, spaces and decimal points.
        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int hours)) return -1;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minutes)) return -1;
        if (hours > 23 || minutes > 59) return -1;

        return hours * 60 + minutes;
    }
}