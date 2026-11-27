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
