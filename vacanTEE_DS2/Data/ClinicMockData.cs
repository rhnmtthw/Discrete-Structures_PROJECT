using vacanTEE_DS2.Models;

namespace vacanTEE_DS2.Data;

public class DepartmentInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMins { get; set; } // Service duration S_k (in minutes)
}

public static class ClinicMockData
{
    public static Dictionary<string, DepartmentInfo> GetDepartments()
    {
        return new Dictionary<string, DepartmentInfo>
        {
            { "LAB", new DepartmentInfo { Code = "LAB", Name = "Central Laboratories – Phlebotomist", DurationMins = 15 } },
            { "ECG", new DepartmentInfo { Code = "ECG", Name = "Dept. of Medicine – OPD ECG Unit", DurationMins = 15 } },
            { "RAD", new DepartmentInfo { Code = "RAD", Name = "Dept. of Radiology – Outpatient X-Ray", DurationMins = 20 } },
            { "PUL", new DepartmentInfo { Code = "PUL", Name = "Dept. of Medicine – Pulmonary (OPD Spirometry)", DurationMins = 25 } },
            { "CON", new DepartmentInfo { Code = "CON", Name = "Dept. of Family & Community Medicine – OPD Consult", DurationMins = 30 } }
        };
    }

    // Department code -> service duration in minutes (used by Engine 2)
    public static Dictionary<string, int> GetDurations() =>
        GetDepartments().ToDictionary(d => d.Key, d => d.Value.DurationMins);

    // Department code -> full department name (used by Engine 2)
    public static Dictionary<string, string> GetDepartmentNames() =>
        GetDepartments().ToDictionary(d => d.Key, d => d.Value.Name);

    // Open Clinic Slots (n = 10): Morning OPD and Afternoon OPD sessions
    public static List<ClinicSlot> GetOpenSlots()
    {
        var depts = GetDepartments();

        var rawSlots = new List<(string Id, string Dept, int StartMins)>
        {
            // Morning OPD Block
            ("PGH-LAB-01", "LAB", 480), // 08:00 AM (15 mins -> ends 08:15 AM)
            ("PGH-RAD-01", "RAD", 500), // 08:20 AM (20 mins -> ends 08:40 AM)
            ("PGH-CON-01", "CON", 525), // 08:45 AM (30 mins -> ends 09:15 AM)
            ("PGH-ECG-01", "ECG", 540), // 09:00 AM (15 mins -> ends 09:15 AM)
            ("PGH-LAB-02", "LAB", 570), // 09:30 AM (15 mins -> ends 09:45 AM)
            ("PGH-PUL-01", "PUL", 600), // 10:00 AM (25 mins -> ends 10:25 AM)

            // Afternoon OPD Block
            ("PGH-ECG-02", "ECG", 780), // 01:00 PM (15 mins -> ends 01:15 PM)
            ("PGH-CON-02", "CON", 795), // 01:15 PM (30 mins -> ends 01:45 PM), contiguous with ECG-02 (0 wait)
            ("PGH-RAD-02", "RAD", 840), // 02:00 PM (20 mins -> ends 02:20 PM)
            ("PGH-CON-03", "CON", 870)  // 02:30 PM (30 mins -> ends 03:00 PM)
        };

        var slots = new List<ClinicSlot>();
        foreach (var item in rawSlots)
        {
            var deptInfo = depts[item.Dept];
            slots.Add(new ClinicSlot
            {
                SlotId = item.Id,
                DeptCode = item.Dept,
                DeptName = deptInfo.Name,
                StartMins = item.StartMins,
                DurationMins = deptInfo.DurationMins,
                TimeLabel = FormatMinutesTo12Hour(item.StartMins)
            });
        }

        return slots;
    }

    // Converts minutes from midnight (e.g., 480) -> "08:00 AM"
    public static string FormatMinutesTo12Hour(int totalMins)
    {
        if (totalMins < 0) return "Invalid Time";

        int hours24 = (totalMins / 60) % 24;
        int mins = totalMins % 60;
        string period = hours24 >= 12 ? "PM" : "AM";
        int hours12 = hours24 % 12;
        if (hours12 == 0) hours12 = 12;

        return $"{hours12:D2}:{mins:D2} {period}";
    }

    // Formats slot window -> "08:00 AM - 08:15 AM"
    public static string FormatTimeRange(int startMins, int durationMins)
    {
        string start = FormatMinutesTo12Hour(startMins);
        string end = FormatMinutesTo12Hour(startMins + durationMins);
        return $"{start} - {end}";
    }

    // Converts HTML <input type="time"> ("HH:mm") -> minutes from midnight
    public static int ParseTimeInputToMinutes(string hhMm)
    {
        if (string.IsNullOrWhiteSpace(hhMm)) return -1;

        var parts = hhMm.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int hours) || !int.TryParse(parts[1], out int mins))
            return -1;

        if (hours < 0 || hours > 23 || mins < 0 || mins > 59)
            return -1;

        return (hours * 60) + mins;
    }
}