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
using System.Globalization;

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
      public int LeaveDays { get; set; }
      public int WorkFromHomeDays { get; set; }
      public int OfficialDaysOff { get; set; }
      public string AverageTimeIn { get; set; }
      public string AverageTimeOut { get; set; }
      public string CountryName { get; set; }
      public string ManagerEmail { get; set; }
      public string Manager2Email { get; set; }
    }
    public class EmailAndIDs
    {
      public int? userId { get; set; }
      public string email { get; set; }
      public int? userLeavePolicyID { get; set; }
    }
    public List<EmailAndIDs> GetUserEmailsAndIDs(string managerEmail)
    {
      using (var context = new LeaveONEntities())
      {
        var users = context.AspNetUsers.Where(y => y.CntryName == "Pakistan" && (y.ManagerID.ToLower() == managerEmail.ToLower() || y.Manager2ID.ToLower() == managerEmail.ToLower()))
        .Select(x => new EmailAndIDs
        {
          userId = x.BioStarEmpNum.Value,
          email = x.Email,
          userLeavePolicyID = x.UserLeavePolicyId,
        })
        .ToList();

        return users;
      }

    }
    public List<EmailAndIDs> GetLegitemacyChckers()
    {
      using (var context = new LeaveONEntities())
      {
        int[] legitimacyCheckers = new int[3] { 2205, 2696, 2434 }; // these are the biostar numbers of legitimacy checkers

        var users = context.AspNetUsers
                           .Where(y => y.BioStarEmpNum.HasValue && legitimacyCheckers.Contains(y.BioStarEmpNum.Value))
                           .Select(x => new EmailAndIDs
                           {
                             userId = x.BioStarEmpNum.Value,
                             email = x.Email,
                             userLeavePolicyID = x.UserLeavePolicyId
                           })
                           .ToList();

        return users;
      }
    }
    public static string GetMonthName(int monthNumber)
    {
      if (monthNumber < 1 || monthNumber > 12)
        throw new ArgumentOutOfRangeException("monthNumber", "Month number must be between 1 and 12.");

      return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
    }
    public Task GetMonthlyReportData(int month, int year, bool legetimacyCheckForReports)
    {
      string monthName = GetMonthName(month);
      var legitimacyCheckers = GetLegitemacyChckers();
      var managerEmails = GetManagersIDs();
      foreach (var managerEmail in managerEmails)
      {
        var usersAgainstManagers = GetUserEmailsAndIDs(managerEmail);
        List<EmployeeReportData> managerReportOfUsersList = new List<EmployeeReportData>();
        var totalWorkDays = 0;
        foreach (var user in usersAgainstManagers)
        {
          totalWorkDays = GetWorkingDays(year, month, user.userLeavePolicyID);
          using (var context = new LeaveONEntities())
          {
            DateTime invalidDate = new DateTime(0001, 01, 01);

            var attendanceData = context.AttendanceDatas.Distinct()
            .Where(a => a.BioStarEmpNum == user.userId && a.CreatedDate.Value.Month == month && a.CreatedDate.Value.Year == year &&
            a.IsLeave != true)
            .ToList();

            if (attendanceData.Any())
            {
              // Filter out entries with invalid FirstPunchIn or LastPunchOut times
              var validPunchIns = attendanceData
              .Where(x => x.FirstPunchIn.HasValue && x.FirstPunchIn.Value != invalidDate)
              .Select(x => x.FirstPunchIn.Value.TimeOfDay.TotalSeconds);

              var validPunchOuts = attendanceData
              .Where(x => x.LastPunchOut.HasValue && x.LastPunchOut.Value != invalidDate)
              .Select(x => x.LastPunchOut.Value.TimeOfDay.TotalSeconds);

              // Compute averages only if there are valid entries
              double averageTimeInSecondsIn = validPunchIns.Any() ? validPunchIns.Average() : 0;
              double averageTimeInSecondsOut = validPunchOuts.Any() ? validPunchOuts.Average() : 0;

              // Convert average seconds to TimeSpan
              TimeSpan averageTimeIn = TimeSpan.FromSeconds(averageTimeInSecondsIn);
              TimeSpan averageTimeOut = TimeSpan.FromSeconds(averageTimeInSecondsOut);

              // Format TimeSpan to 12-hour format with AM/PM
              string formattedAverageTimeIn = new DateTime(averageTimeIn.Ticks).ToString("hh:mm tt");
              string formattedAverageTimeOut = new DateTime(averageTimeOut.Ticks).ToString("hh:mm tt");

              var reportData = new EmployeeReportData
              {
                EmployeeID = user.userId,
                EmployeeName = attendanceData.First().UserName,
                Department = attendanceData.First().DepartmentName,
                TotalWorkHours = attendanceData.Sum(x => x.TotalWorkHours),
                TotalBreakHours = attendanceData.Sum(x => x.BreakHours),
                LateArrivals = attendanceData.Count(x => x.IsLateArrival == true),
                EarlyDepartures = attendanceData.Count(x => x.IsEarlyDeparture == true),
                AbsentDays = attendanceData.Count(x => x.IsAbsent == true || x.IsLeave == true),
                LeaveDays = attendanceData.Count(x => x.IsLeave == true),
                AverageTimeIn = averageTimeIn.ToString(@"hh\:mm\:ss"),
                AverageTimeOut = averageTimeOut.ToString(@"hh\:mm\:ss"),
                WorkFromHomeDays = attendanceData.Count(x => x.LeaveTypeID == 10),
                OfficialDaysOff = attendanceData.Count(x => x.LeaveTypeID == 8 || x.LeaveTypeID == 9),
                CountryName = attendanceData.First().CountryName,
                ManagerEmail = attendanceData.First().ManagerEmail,
                Manager2Email = attendanceData.First().Manager2Email,
              };
              managerReportOfUsersList.Add(reportData);
              if (legetimacyCheckForReports)//make it true again, false is for testing
              {
                foreach (EmailAndIDs legitChecker in legitimacyCheckers)
                {
                  GeneratePDFIndividuals(reportData, legitChecker.email, totalWorkDays, monthName); // Pass the user's email to the PDF generation and sending function
                }
              }
              else
              {

                //if(reportData.Department.ToLower() == "is&t")
                //{
                GeneratePDFIndividuals(reportData, user.email, totalWorkDays, monthName); // Pass the user's email to the PDF generation and sending function
              }
            }
            Console.WriteLine($"Data not exist against this user {user.email}");
          }

        }
        GeneratePDFManager(managerReportOfUsersList, totalWorkDays, monthName, legetimacyCheckForReports, legitimacyCheckers);
      }
      return null;
    }
    public void GeneratePDFIndividuals(EmployeeReportData reportData, string userEmail, int totalWorkDays, string monthName)
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
        mail.To.Add(new MailAddress(userEmail));
        mail.Subject = $"Monthly Report - {reportData.EmployeeName}";
        mail.Body = $"Attached is the monthly report for {reportData.EmployeeName}.";

        Console.WriteLine($"Email Subject: {mail.Subject}");
        Console.WriteLine($"Email Body: {mail.Body}");


        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A4, 50, 50, 25, 25);
          PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          PdfPTable table = new PdfPTable(2); // Two columns
          table.WidthPercentage = 100; // Table size is set to 100% of the page

          // Define a single-cell header
          PdfPCell header = new PdfPCell(new Phrase("Your Monthly Report", new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.WHITE)));
          header.Colspan = 2;
          header.HorizontalAlignment = Element.ALIGN_CENTER;
          header.BackgroundColor = new BaseColor(0, 51, 102); // Light blue background
          header.Border = Rectangle.BOTTOM_BORDER; // Only bottom border
          header.PaddingBottom = 10;
          table.AddCell(header);

          // Helper method to create a cell with specific styles
          void AddStyledCell(string content, int colspan = 1, bool isHeader = false)
          {
            PdfPCell cell = new PdfPCell(new Phrase(content, new Font(Font.FontFamily.HELVETICA, 12, isHeader ? Font.BOLD : Font.NORMAL, isHeader ? BaseColor.WHITE : BaseColor.BLACK)));
            cell.Colspan = colspan;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            cell.Border = Rectangle.BOTTOM_BORDER;
            if (isHeader)
            {
              cell.BackgroundColor = new BaseColor(0, 51, 102); // Slightly darker blue for header cells
            }
            table.AddCell(cell);
          }
          Font dataFont = new Font(Font.FontFamily.HELVETICA, 15, Font.NORMAL);

          //// Adding dynamic data
          AddStyledCell($"Number working days in {monthName}: {totalWorkDays}", 2, true);
          AddStyledCell(reportData.EmployeeName.ToUpper() + " (" + reportData.EmployeeID + ")", 2, true);
          AddStyledCell("Average Entry Time:", 1, true);
          AddStyledCell(reportData.AverageTimeIn, 1);
          AddStyledCell("Average Exit Time:", 1, true);
          AddStyledCell(reportData.AverageTimeOut, 1);
          AddStyledCell("Working days of Employee", 1, true);
          AddStyledCell((totalWorkDays - ((reportData.AbsentDays))).ToString(), 1);
          AddStyledCell("Absents (Casual/Annual)", 1, true);
          AddStyledCell(reportData.AbsentDays.ToString(), 1);
          AddStyledCell("Work from home", 1, true);
          AddStyledCell(reportData.WorkFromHomeDays.ToString(), 1);
          AddStyledCell("Official Days off", 1, true);
          AddStyledCell(reportData.OfficialDaysOff.ToString(), 1);

          document.Add(table);
          document.Close();

          // Convert the memory stream to an array of bytes
          byte[] bytes = memoryStream.ToArray();

          // Attach the PDF as an email attachment
          mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "MonthlyReport.pdf", "application/pdf"));
        }

        // Try to send the email and capture any exceptions
        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
          Console.WriteLine("Email sending failed: {ex.Message}");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error sending email: " + ex.Message);
      }
    }
    public static int GetWorkingDays(int year, int month, int? userLeavePolicyID)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      int workingDays = 0;
      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        {
          workingDays++;
        }
      }

      var context = new LeaveONEntities();
      // Get all off days for the user that fall within the specified month and year
      var annualOffDays = context.AnnualOffDays
                                  .Where(x => x.UserLeavePolicyId == userLeavePolicyID
                                          && x.OffDay >= startOfMonth
                                          && x.OffDay <= endOfMonth)
                                  .ToList();

      // Subtract off days that are weekdays
      foreach (var offDay in annualOffDays)
      {
        workingDays--;
      }

      return workingDays;
    }
    public void GeneratePDFManager(List<EmployeeReportData> reportData, int totalWorkDays, string monthName, bool legitimacyCheckForReports, List<EmailAndIDs> legitimacyCheckers)
    {
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
      {
        UseDefaultCredentials = false,
        Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
        Port = 587,
        EnableSsl = true
      };
        var managerEmails = GetManagersIDs();
      foreach (var data in reportData)
      {
        MailMessage mail = new MailMessage
        {
          From = new MailAddress(LeavON_Email),
          Subject = $"Monthly Attendance Report for {data.EmployeeName}",
          Body = "Attached is the monthly attendance report."
        };

        // Check if legitimacy checks are needed
        if (legitimacyCheckForReports)
        {
          // Add all legitimacy checkers to the email
          foreach (var checker in legitimacyCheckers)
          {
            mail.To.Add(new MailAddress(checker.email));
          }
        }
        else
        {
          foreach (var manager in managerEmails)
          {
            try
            {
              using (var context = new LeaveONEntities())
              {
                //var email = new MailAddress(manager); // This will throw if the email is invalid
                //mail.To.Add(email);  // Add only if the email is valid
                var managerEmail = context.AspNetUsers
            .Where(u => u.ManagerID == manager || u.Manager2ID == manager)
            .Select(u => u.Email) // Assuming the email field is named "Email"
            .FirstOrDefault();
                mail.To.Add(new MailAddress(managerEmail));
              }
            }
            catch (FormatException)
            {
              // Handle invalid email format
              Console.WriteLine($"Invalid email: {manager}");
              // Optionally, log the invalid email or skip this iteration
            }
          }

        }

        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A3, 50, 50, 25, 25);
          PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          PdfPTable table = new PdfPTable(8); // Assuming 8 columns as before
          table.WidthPercentage = 100;

          // Column headers
          string[] headers = { "Employee Name", "Employee ID", "Average Entry Time", "Average Exit Time", "Total Working Days", "Absent Days", "Work From Home Days", "Official Days Off" };
          foreach (string header in headers)
          {
            PdfPCell headerCell = new PdfPCell(new Phrase(header, FontFactory.GetFont("Arial", 12, Font.BOLD)))
            {
              HorizontalAlignment = Element.ALIGN_CENTER,
              Padding = 5
            };
            table.AddCell(headerCell);
          }

          // Data cells
          table.AddCell(new Phrase(data.EmployeeName, FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.EmployeeID.ToString(), FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.AverageTimeIn, FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.AverageTimeOut, FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase((totalWorkDays - data.AbsentDays).ToString(), FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.AbsentDays.ToString(), FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.WorkFromHomeDays.ToString(), FontFactory.GetFont("Arial", 12)));
          table.AddCell(new Phrase(data.OfficialDaysOff.ToString(), FontFactory.GetFont("Arial", 12)));

          document.Add(table);
          document.Close();

          // Attach the PDF
          mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()), "MonthlyAttendanceReport.pdf", "application/pdf"));
        }

        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email send successfully ...");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to send email to {(legitimacyCheckForReports ? "legitimacy checkers" : data.ManagerEmail)}: {ex.Message}");
        }
      }
    }

    public static string ConvertSecondsToReadableTime(long totalSeconds)
    {
      long hours = totalSeconds / 3600;
      long minutes = (totalSeconds % 3600) / 60;
      long seconds = totalSeconds % 60;

      return $"{hours} hours, {minutes} minutes, {seconds} seconds";
    }

    private List<string> GetManagersIDs()
    {
      using (var context = new LeaveONEntities())
      {
       // var managersIDs = context.Managers.Select(x => x.UserID).ToList();
        var managersIDs = context.AspNetUsers
               .Where(user => !string.IsNullOrEmpty(user.ManagerID) || !string.IsNullOrEmpty(user.Manager2ID)) // Filter out null or empty ManagerIDs
               .Select(user => !string.IsNullOrEmpty(user.ManagerID) ? user.ManagerID : user.Manager2ID) // Select only ManagerID
               .Distinct() // Ensure unique IDs (optional)
               .ToList();
        return managersIDs;
      }
    }
    private List<string> GetManagerEmailByDepartment(string department)
    {
      using (var context = new LeaveONEntities())
      {
        var managers = context.AspNetUserClaims.Where(x => x.ClaimType == department && x.isReportEmail == true)
        .Select(x => x.UserId).ToList();

        var managerEmails = new List<string>();
        foreach (var manager in managers)
        {
          // Fetch the email of the manager, make sure only one email is added
          var email = context.AspNetUsers
          .Where(x => x.Id == manager)
          .Select(x => x.Email)
          .FirstOrDefault(); // Ensures you get a single result or null, not a collection

          if (!string.IsNullOrEmpty(email))
          {
            managerEmails.Add(email);
          }
        }

        return managerEmails;
      }
    }

    //public void GeneratePDFManager(List<EmployeeReportData> reportData, int totalWorkDays, string monthName, bool legetimacyCheckForReports, List<EmailAndIDs> legitimacyCheckers)
    //{
    //  SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
    //  {
    //    UseDefaultCredentials = false,
    //    Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
    //    Port = 587,
    //    EnableSsl = true
    //  };

    //  var groupedByDepartment = reportData.GroupBy(emp => emp.Department.Trim()).ToList();
    //  foreach (var group in groupedByDepartment)
    //  {
    //    List<string> managerEmails = GetManagerEmailByDepartment(group.Key);  // Get manager emails for the department
    //    foreach (string managerEmail in managerEmails)
    //    {
    //      MailMessage mail = new MailMessage
    //      {
    //        From = new MailAddress(LeavON_Email),
    //        Subject = $"Monthly Report for {group.Key} Department",
    //        Body = $"Attached is the monthly report for {group.Key} Department."
    //      };
    //      if (legetimacyCheckForReports)
    //      {
    //        foreach (EmailAndIDs legitChecker in legitimacyCheckers)
    //        {
    //          mail.To.Add(new MailAddress(legitChecker.email));
    //        }
    //      }
    //      else
    //      {
    //        mail.To.Add(new MailAddress("haiderali98.ha61@gmail.com"));  // Send to each manager
    //      }

    //      using (MemoryStream memoryStream = new MemoryStream())
    //      {
    //        Document document = new Document(PageSize.A3, 50, 50, 25, 25);
    //        PdfWriter.GetInstance(document, memoryStream);
    //        document.Open();

    //        PdfPTable table = new PdfPTable(new float[] { 3, 2, 2, 2, 2, 2, 2, 2 });
    //        table.WidthPercentage = 100;

    //        // Header
    //        Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
    //        PdfPCell headerCell = new PdfPCell(new Phrase($"Number of working days in {monthName}: {totalWorkDays}", headerFont))
    //        {
    //          Colspan = 7,
    //          HorizontalAlignment = Element.ALIGN_CENTER,
    //          BackgroundColor = new BaseColor(0, 51, 102),
    //          Padding = 8
    //        };
    //        table.AddCell(headerCell);
    //        PdfPCell headerCell2 = new PdfPCell(new Phrase($"Department: {group.Key}", headerFont))
    //        {
    //          Colspan = 7,
    //          HorizontalAlignment = Element.ALIGN_CENTER,
    //          BackgroundColor = new BaseColor(0, 51, 102),
    //          Padding = 8
    //        };
    //        table.AddCell(headerCell2);
    //        // Column headers
    //        float[] columnWidths = new float[] { 3f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
    //        table.SetWidths(columnWidths);
    //        Font headerFont2 = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
    //        string[] headers = { "Elements", "Average Entry Time", "Average Exit Time", "Working days of Employee", "Absents (Casual/  Annual)", "Work from home", "Official Days off", "Country" };
    //        foreach (string header in headers)
    //        {
    //          PdfPCell colHeaderCell = new PdfPCell(new Phrase(header, headerFont2))
    //          {
    //            BackgroundColor = new BaseColor(0, 76, 153),
    //            HorizontalAlignment = Element.ALIGN_CENTER,
    //            Padding = 5
    //          };
    //          table.AddCell(colHeaderCell);
    //        }
    //        // Data cells
    //        Font dataFont = new Font(Font.FontFamily.HELVETICA, 15, Font.NORMAL);
    //        Font nameFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD);
    //        BaseColor yellowColor = new BaseColor(255, 255, 0); // RGB for yellow
    //        foreach (EmployeeReportData data in group)
    //        {
    //          PdfPCell cell;
    //          bool hasAbsentDays = data.AbsentDays >= 3;
    //          // Create a new PdfPCell, set its Phrase and Font, then align it to center
    //          cell = new PdfPCell(new Phrase(data.EmployeeName.ToUpper() + " (" + data.EmployeeID + ")", nameFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.AverageTimeIn, dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.AverageTimeOut, dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase((totalWorkDays - data.AbsentDays).ToString(), dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.AbsentDays.ToString(), dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          if (hasAbsentDays) cell.BackgroundColor = yellowColor;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.WorkFromHomeDays.ToString(), dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.OfficialDaysOff.ToString(), dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);

    //          cell = new PdfPCell(new Phrase(data.CountryName.ToString(), dataFont));
    //          cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //          table.AddCell(cell);
    //        }
    //        document.Add(table);
    //        document.Close();

    //        // Convert the memory stream to an array of bytes
    //        byte[] bytes = memoryStream.ToArray();

    //        // Attach the PDF as an email attachment
    //        mail.Attachments.Add(new Attachment(new MemoryStream(bytes), $"MonthlyReport_{group.Key}.pdf", "application/pdf"));
    //      }
    //      try
    //      {
    //        smtpServer.Send(mail);
    //      }
    //      catch (Exception ex)
    //      {
    //        Console.WriteLine($"Failed to send email to {managerEmail}: {ex.Message}");
    //      }
    //    }
    //  }
    //}
  }
}

