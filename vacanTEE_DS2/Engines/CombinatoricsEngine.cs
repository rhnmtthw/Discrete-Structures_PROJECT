using vacanTEE_DS2.Models;

namespace vacanTEE_DS2.Engines;

public class CombinatoricsEngine
{
    public List<ClinicSlot> GetSampleSlots()
    {
        var slots = new List<ClinicSlot>();

        slots.Add(new ClinicSlot { SlotId = "S1", DeptCode = "LAB", StartMins = 480, TimeLabel = "8:00 AM" });
        slots.Add(new ClinicSlot { SlotId = "S2", DeptCode = "LAB", StartMins = 510, TimeLabel = "8:30 AM" });
        slots.Add(new ClinicSlot { SlotId = "S3", DeptCode = "RAD", StartMins = 500, TimeLabel = "08:20 AM" });
        slots.Add(new ClinicSlot { SlotId = "S4", DeptCode = "RAD", StartMins = 540, TimeLabel = "09:00 AM" });
        slots.Add(new ClinicSlot { SlotId = "S5", DeptCode = "CON", StartMins = 525, TimeLabel = "08:45 AM" });
        slots.Add(new ClinicSlot { SlotId = "S6", DeptCode = "CON", StartMins = 570, TimeLabel = "09:30 AM" });

        return slots;
    }

    public (int Total, string Explanation) CountFcp(List<ClinicSlot> slots, List<string> requiredDepts)
    {
        int product = 1;
        var parts = new List<string>();

        foreach(string code in requiredDepts)
        {
            int count = slots.Count(s => s.DeptCode == code);
            product *= count;
            parts.Add(count + " (" + code + ")");
        }

        string explanation = string.Join(" x ", parts) + " = " + product + " raw combinations";
        return (product, explanation);
    }

    public (long Total, string Explanation) CountPermutations(int n, int r)
    {
        if (r > n) return (0, $"r cannot exceed n, where r is {r} and n is {n}");
        if (r <= 0) return (0, $"At least one procedure is required");

        long result = 1;

        for(int i = 0; i < r; i++)
        {
            result *= (n - i);
        }

        string explanation = $"P({n}, {r}) = {n}! / ({n} - {r})! = {result}";
        return (result, explanation);
    }

    public List<List<ClinicSlot>> GeneratePermutations(List<ClinicSlot> slots, int r)
    {
        var results = new List<List<ClinicSlot>>();
        var current = new List<ClinicSlot>();
        var used = new bool[slots.Count];

        Build(slots, r, current, used, results);
        return results;
    }

    private void Build(List<ClinicSlot> slots, int r, List<ClinicSlot> current, bool[] used, List<List<ClinicSlot>> results)
    {
        if(current.Count == r)
        {
            results.Add(new List<ClinicSlot>(current));
            return;
        }

        for(int i = 0; i < slots.Count; i++)
        {
            if (used[i] == true) continue;
            used[i] = true;
            current.Add(slots[i]);
            Build(slots, r, current, used, results);
            used[i] = false;
            current.RemoveAt(current.Count - 1);
        }
    }

    public bool IsValid(List<ClinicSlot> sequence, List<string> requiredDepts, int arrivalMins)
    {
        for(int k = 0; k < sequence.Count; k++)
        {
            if (sequence[k].DeptCode != requiredDepts[k]) return false;
            
            if(k == 0)
            {
                if (sequence[0].StartMins < arrivalMins) return false;
            }
            else
            {
                ClinicSlot prev = sequence[k-1];
                int prevEnd = prev.StartMins + prev.DurationMins;
                if (sequence[k].StartMins < prevEnd) return false;
            }
        }

        return true;
    }

    public Engine1Result Run(List<ClinicSlot> allSlots, List<String> requiredDepts, int arrivalMins)
    {
        var result = new Engine1Result();

        if(requiredDepts == null || requiredDepts.Count == 0)
        {
            result.Status = "ERROR";
            result.ErrorMessage = "At least one procedure is required (r=0).";
            return result;
        }

        var slots = allSlots.Where(s => requiredDepts.Contains(s.DeptCode) && s.StartMins >= arrivalMins).ToList();

        int r = requiredDepts.Count;
        int n = slots.Count;

        if( r > n)
        {
            result.Status = "ERROR";
            result.ErrorMessage = $"r cannot exceed n, where r is {r} and n is {n}.";
            return result;
        }

        var fcp = CountFcp(slots, requiredDepts);
        var perm = CountPermutations(n, r);

        var all = GeneratePermutations(slots, r);
        foreach(var seq in all)
        {
            if (IsValid(seq, requiredDepts, arrivalMins))
                result.ValidSchedules.Add(seq);
        }

        result.ValidCount = result.ValidSchedules.Count;
        result.FcpCalculation = fcp.Explanation;
        result.PermutationCalculation = perm.Explanation + " -> " + result.ValidCount + " non-overlapping valid sequences";

        return result;
    }
}