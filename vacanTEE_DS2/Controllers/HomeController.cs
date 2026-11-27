using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using vacanTEE_DS2.Models;
using vacanTEE_DS2.Engines;

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
            var slots = engine.GetSampleSlots();

            
            var r1 = engine.Run(slots, new List<string> { "LAB", "RAD", "CON" }, 480);
            string output = "TC-1: " + r1.Status + "\n" + r1.FcpCalculation + "\n" + r1.PermutationCalculation + "\n";
            foreach (var seq in r1.ValidSchedules)
                output += string.Join(" > ", seq.Select(s => s.SlotId)) + "\n";

           
            var r2 = engine.Run(slots, new List<string> { "RAD", "CON" }, 530);
            output += "\nTC-2: " + r2.Status + " | " + r2.PermutationCalculation + "\n";

            
            var r5a = engine.Run(slots, new List<string>(), 480);
            output += "\nTC-5a: " + r5a.Status + " | " + r5a.ErrorMessage + "\n";

            var r5b = engine.Run(slots, new List<string> { "LAB", "RAD", "CON" }, 560);
            output += "TC-5b: " + r5b.Status + " | " + r5b.ErrorMessage + "\n";

            return Content(output);
        }
    }
}
