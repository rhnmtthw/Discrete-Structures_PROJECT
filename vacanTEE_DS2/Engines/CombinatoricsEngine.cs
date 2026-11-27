using vacanTEE_DS2.Models;
using vacanTEE_DS2.Data;
using vacanTEE_DS2.Models;

namespace vacanTEE_DS2.Engines;

public class CombinatoricsEngine
{
    public List<Slot> GetSampleSlots()
    {
        var slots = new List<Slot>();

        slots.Add(new Slot { SlotId = "S1", Dept = "LAB", StartMins = 480, Label = "8:00 AM" });
        slots.Add(new Slot { SlotId = "S2", Dept = "LAB", StartMins = 510, Label = "8:30 AM" });
        slots.Add(new Slot { SlotId = "S3", Dept = "RAD", StartMins = 500, Label = "08:20 AM" });
        slots.Add(new Slot { SlotId = "S4", Dept = "RAD", StartMins = 540, Label = "09:00 AM" });
        slots.Add(new Slot { SlotId = "S5", Dept = "CON", StartMins = 525, Label = "08:45 AM" });
        slots.Add(new Slot { SlotId = "S6", Dept = "CON", StartMins = 570, Label = "09:30 AM" });

        return slots;
    }

    public (int Total, string Explanation) CountFcp(List<Slot> slots, List<string> requiredDepts)
    {
        int product = 1;
        var parts = new List<string>();

        foreach(string code in requiredDepts)
        {
            int count = slots.Count(s => s.Dept == code);
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

    public List<List<Slot>> GeneratePermutations(List<Slot> slots, int r)
    {
        var results = new List<List<Slot>>();
        var current = new List<Slot>();
        var used = new bool[slots.Count];

        Build(slots, r, current, used, results);
        return results;
    }

    private void Build(List<Slot> slots, int r, List<Slot> current, bool[] used, List<List<Slot>> results)
    {
        if(current.Count == r)
        {
            results.Add(new List<Slot>(current));
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

    public Dictionary<string, int> GetSampleDurations()
    {
        return new Dictionary<string, int>
        {
            { "LAB", 15 },
            { "RAD", 20 },
            { "CON", 30 },
            { "ECG", 15 }
        };
    }

    public bool IsValid(List<Slot> sequence, List<string> requiredDepts, int arrivalMins, Dictionary<string, int> durations)
    {
        for(int k = 0; k < sequence.Count; k++)
        {
            if (sequence[k].Dept != requiredDepts[k]) return false;
            
            if(k == 0)
            {
                if (sequence[0].StartMins < arrivalMins) return false;
            }
            else
            {
                Slot prev = sequence[k-1];
                int prevEnd = prev.StartMins + durations[prev.Dept];
                if (sequence[k].StartMins < prevEnd) return false;
            }
        }

        return true;
    }

    public Engine1Result Run(List<Slot> allSlots, List<String> requiredDepts, int arrivalMins)
    {
        var result = new Engine1Result();

        if(requiredDepts == null || requiredDepts.Count == 0)
        {
            result.Status = "ERROR";
            result.ErrorMessage = "At least one procedure is required (r=0).";
            return result;
        }

        var slots = allSlots.Where(s => requiredDepts.Contains(s.Dept) && s.StartMins >= arrivalMins).ToList();

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
        var durations = ClinicMockData.GetDurations();

        var all = GeneratePermutations(slots, r);
        foreach(var seq in all)
        {
            if (IsValid(seq, requiredDepts, arrivalMins, durations))
                result.ValidSchedules.Add(seq);
        }

        result.ValidCount = result.ValidSchedules.Count;
        result.FcpCalculation = fcp.Explanation;
        result.PermutationCalculation = perm.Explanation + " -> " + result.ValidCount + "non-overlapping valid sequences";

        return result;
    }
}