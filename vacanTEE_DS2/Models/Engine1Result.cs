namespace vacanTEE_DS2.Models
{
    public class Engine1Result
    {
        public string Status { get; set; } = "SUCCESS";
        public string? ErrorMessage { get; set; }
        public string FcpCalculation { get; set; } = "";
        public string PermutationCalculation { get; set; } = "";
        public int ValidCount { get; set; }
        public List<List<ClinicSlot>> ValidSchedules { get; set; } = new();
    }
}
