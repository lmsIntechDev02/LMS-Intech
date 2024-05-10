using System;
using Repository.Models;
using System.Data;
using System.Linq;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf; // Adjust based on actual namespace that includes AttendanceData and LeaveONEntities
using System.Net.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;
using iTextSharp.text.pdf.draw;
namespace LeaveON.Services
{

  public class AutomatedReportService
  {
    public const string LeavON_Email = "LMS@intechww.com";
    public const string LeavON_Password = "Pakistan12345678*";
    public class EmployeeReportData
    {
      public int? EmployeeID { get; set; }
      public string EmployeeName { get; set; }
      public string Department { get; set; }
      public long? TotalWorkHours { get; set; }
      public long? TotalBreakHours { get; set; }
      public int LateArrivals { get; set; }
      public int EarlyDepartures { get; set; }
      public int AbsentDays { get; set; }
      public TimeSpan AverageTimeIn { get; set; }
      public TimeSpan AverageTimeOut { get; set; }
    }
    public class EmailAndIDs
    {
      public int? userId { get; set; }
      public string email { get; set; }
    }
    public List<EmailAndIDs> GetUserEmailsAndIDs()
    {
      using (var context = new LeaveONEntities())
      {
        var users = context.AspNetUsers
            .Select(x => new EmailAndIDs
            {
              userId = x.BioStarEmpNum.Value, // Assuming EmployeeID is the correct property to link with AttendanceData
              email = x.Email
            })
            .ToList();

        return users;
      }
    }
    public Task GetMonthlyReportData(int month, int year)
    {
      var users = GetUserEmailsAndIDs();
      List<EmployeeReportData> managerReportOfUsersList = new List<EmployeeReportData>();
      foreach (var user in users)
      {
        using (var context = new LeaveONEntities())
        {
          var attendanceData = context.AttendanceDatas
              .Where(a => a.EmployeeID == user.userId && a.CreatedDate.Value.Month == month && a.CreatedDate.Value.Year == year)
              .ToList();

          if (attendanceData.Any())
          {
            var averageTimeInSecondsIn = attendanceData.Average(x => x.FirstPunchIn.Value.TimeOfDay.TotalSeconds);
            var averageTimeInSecondsOut = attendanceData.Average(x => x.LastPunchOut.Value.TimeOfDay.TotalSeconds);

            var reportData = new EmployeeReportData
            {
              EmployeeID = user.userId,
              EmployeeName = attendanceData.First().UserName,
              Department = attendanceData.First().DepartmentName,
              TotalWorkHours = attendanceData.Sum(x => x.TotalWorkHours),
              TotalBreakHours = attendanceData.Sum(x => x.BreakHours),
              LateArrivals = attendanceData.Count(x => x.IsLateArrival == true),
              EarlyDepartures = attendanceData.Count(x => x.IsEarlyDeparture == true),
              AbsentDays = attendanceData.Count(x => x.IsAbsent == true),
              AverageTimeIn = TimeSpan.FromSeconds(averageTimeInSecondsIn),
              AverageTimeOut = TimeSpan.FromSeconds(averageTimeInSecondsOut)
            };
            managerReportOfUsersList.Add(reportData);
            //GeneratePDFIndividuals(reportData, user.email); // Pass the user's email to the PDF generation and sending function
          }
        }
      }
      GeneratePDFManager(managerReportOfUsersList, "haiderali98.ha61@gmail.com");
      return null;
    }
    public void GeneratePDFIndividuals(EmployeeReportData reportData, string userEmail)
    {
      MailMessage mail = new MailMessage();
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com");
      smtpServer.UseDefaultCredentials = false;
      smtpServer.Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password);
      smtpServer.Port = 587;
      smtpServer.EnableSsl = true;

      try
      {
        mail.From = new MailAddress(LeavON_Email);
        mail.To.Add(new MailAddress("laiba.khan@intechww.com"));
        mail.Subject = $"Monthly Report - {reportData.EmployeeName}";
        mail.Body = $"Attached is the monthly report for {reportData.EmployeeName}.";

        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A4, 50, 50, 25, 25);
          PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
          document.Open();
          PdfPTable table = new PdfPTable(6); // Adjusted for more columns
          table.AddCell("Employee ID");
          table.AddCell("Name");
          table.AddCell("Department");
          table.AddCell("Total Work Hours");
          table.AddCell("Total Break Hours");
          table.AddCell("Details");

          table.AddCell(reportData.EmployeeID.ToString());
          table.AddCell(reportData.EmployeeName);
          table.AddCell(reportData.Department);
          table.AddCell(ConvertSecondsToReadableTime(reportData.TotalWorkHours.Value));
          table.AddCell(ConvertSecondsToReadableTime(reportData.TotalBreakHours.Value));
          table.AddCell($"Late: {reportData.LateArrivals}, Early: {reportData.EarlyDepartures}, Absent: {reportData.AbsentDays}");

          document.Add(table);

          Paragraph averages = new Paragraph($"Average Time In: {reportData.AverageTimeIn.ToString(@"hh\:mm\:ss")}\nAverage Time Out: {reportData.AverageTimeOut.ToString(@"hh\:mm\:ss")}");
          document.Add(averages);

          document.Close();
          // Convert the memory stream to an array of bytes
          byte[] bytes = memoryStream.ToArray();

          // Attach the PDF as an email attachment
          mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "MonthlyReport.pdf", "application/pdf"));
        }
        smtpServer.Send(mail);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error sending email: " + ex.Message);
        // Handle errors appropriately
      }
    }
    public void GeneratePDFManager(List<EmployeeReportData> reportData, string userEmail)
    {
      MailMessage mail = new MailMessage();
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com");
      smtpServer.UseDefaultCredentials = false;
      smtpServer.Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password);
      smtpServer.Port = 587;
      smtpServer.EnableSsl = true;

      try
      {
        mail.From = new MailAddress(LeavON_Email);
        mail.To.Add(new MailAddress("haiderali98.ha61@gmail.com"));
        mail.Subject = $"Monthly Report of All Employees";
        mail.Body = $"Attached is the monthly report for All Employees";

        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A4, 50, 50, 25, 25);
          PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
          document.Open();
          PdfPTable table = new PdfPTable(8); // Adjusted for more columns
          table.AddCell("Sr No.");
          table.AddCell("Employee ID");
          table.AddCell("Name");
          table.AddCell("Department");
          table.AddCell("Total Work Hours");
          table.AddCell("Total Break Hours");
          table.AddCell("Details");
          table.AddCell("Average");
          var count = 0;
          foreach (EmployeeReportData report in reportData)
          {
            table.AddCell(count.ToString());
            table.AddCell(report.EmployeeID.ToString());
            table.AddCell(report.EmployeeName);
            table.AddCell(report.Department);
            table.AddCell(ConvertSecondsToReadableTime(report.TotalWorkHours.Value)); 
            table.AddCell(ConvertSecondsToReadableTime(report.TotalBreakHours.Value));
            table.AddCell($"Late: {report.LateArrivals}, Early: {report.EarlyDepartures}, Absent: {report.AbsentDays}");
            table.AddCell($"Average Time In: {report.AverageTimeIn.ToString(@"hh\:mm\:ss")}\nAverage Time Out: {report.AverageTimeOut.ToString(@"hh\:mm\:ss")}");
            count++;
          }
          document.Add(table);
          document.Close();
          // Convert the memory stream to an array of bytes
          byte[] bytes = memoryStream.ToArray();

          // Attach the PDF as an email attachment
          mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "MonthlyReportAllEmployees.pdf", "application/pdf"));
        }
        smtpServer.Send(mail);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error sending email: " + ex.Message);
        // Handle errors appropriately
      }
    }
    public static string ConvertSecondsToReadableTime(long totalSeconds)
    {
      long hours = totalSeconds / 3600;
      long minutes = (totalSeconds % 3600) / 60;
      long seconds = totalSeconds % 60;

      return $"{hours} hours, {minutes} minutes, {seconds} seconds";
    }
  }
}
