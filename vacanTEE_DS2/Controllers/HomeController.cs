using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using vacanTEE_DS2.Models;
using vacanTEE_DS2.Engines;
using vacanTEE_DS2.Data;

namespace vacanTEE_DS2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Test()
        {
            var engine = new CombinatoricsEngine();
            var slots = ClinicMockData.GetOpenSlots();
            string output = "";

            output += Show("TC-1", engine.Run(slots, new List<string> { "LAB", "RAD", "CON" }, 480));

            output += Show("TC-2", engine.Run(slots, new List<string> { "RAD", "CON" }, 530));

            output += Show("TC-5a", engine.Run(slots, new List<string>(), 480));

            output += Show("TC-5b", engine.Run(slots, new List<string> { "LAB", "RAD", "CON" }, 900));

            var timer = System.Diagnostics.Stopwatch.StartNew();
            var big = engine.Run(slots, new List<string> { "LAB", "ECG", "RAD", "PUL", "CON" }, 480);
            timer.Stop();
            output += Show("LARGE", big) + "Time: " + timer.ElapsedMilliseconds + " ms\n";

            // ENGINE 1 -> ENGINE 2 (TC-1)
            var engine2 = new QueueRecurrenceEngine();
            var e1 = engine.Run(slots, new List<string> { "LAB", "RAD", "CON" }, 480);
            var e2 = engine2.Run(e1, 480);

            output += "\nENGINE 2: " + e2.Status + "\n";
            if (e2.Status == "ERROR")
            {
                output += e2.ErrorMessage + "\n";
            }
            else
            {
                foreach (var step in e2.OptimalSchedule)
                    output += step.StepNumber + ". " + step.Slot.SlotId + " " + step.TimeRange
                            + " (W = " + step.CumulativeWaitMins + ")\n";
                output += "Total wait: " + e2.TotalWaitMins + " mins\n";
                foreach (var line in e2.RecursiveTrace)
                    output += line + "\n";
            }

            // Zero-wait case: ECG then CON, arriving at 01:00 PM (780)
            var z1 = engine.Run(slots, new List<string> { "ECG", "CON" }, 780);
            var z2 = engine2.Run(z1, 780);
            output += "\nZERO-WAIT: " + z2.Status + " | total " + z2.TotalWaitMins + "\n";

            return Content(output);
        }

        private string Show(string name, Engine1Result r)
        {
            string text = "\n" + name + ": " + r.Status + "\n";

            if (r.Status == "ERROR")
                return text + r.ErrorMessage + "\n";

            text += r.FcpCalculation + "\n" + r.PermutationCalculation + "\n";
            foreach (var seq in r.ValidSchedules)
                text += string.Join(" > ", seq.Select(s => s.SlotId)) + "\n";

            return text;
        }
    }
}
