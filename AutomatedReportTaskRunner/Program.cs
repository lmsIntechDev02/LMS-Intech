using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeaveON.Services;

namespace AutomatedReportTaskRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("LOADING");
            DateTime today = DateTime.Today;
            DateTime previousMonthDate = today.AddMonths(-1);
            int month = previousMonthDate.Month;
            int year = previousMonthDate.Year;
            // Initialize the service
            AutomatedReportService service = new AutomatedReportService();  // Make sure any dependencies are resolved
            service.GetMonthlyReportData(1, 2024);
            Console.WriteLine("Automated Report Made");
        }
    }
}
