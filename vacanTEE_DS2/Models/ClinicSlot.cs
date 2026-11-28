namespace vacanTEE_DS2.Models;

public class ClinicSlot
{
    public string SlotId { get; set; } = "";
    public string DeptCode { get; set; } = "";
    public string DeptName { get; set; } = "";
    public int StartMins { get; set; }
    public int DurationMins { get; set; }
    public string TimeLabel { get; set; } = "";
}