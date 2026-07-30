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
using System.Data.SqlClient;

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
      public int AvailedLeave { get; set; }
      public int LeaveDays { get; set; }
      public int WorkFromHomeDays { get; set; }
      public int OfficialDaysOff { get; set; }
      public string AverageTimeIn { get; set; }
      public string AverageTimeOut { get; set; }
      public string CountryName { get; set; }
      public string ManagerEmail { get; set; }
      public string Manager2Email { get; set; }
      public int TotalDays { get; set; }
      public string AverageTimeInOffice { get; set; }
      public string AssignedLeaveQuota { get; set; }
      public string BalanceLeave { get; set; }
      public string AvailableLeave { get; set; }
      public string CompensatoryLeave { get; set; }
      public string ShortHoursInMonth { get; set; }
      public string OffcialDaysOff { get; set; }
      public string CasualLeaves { get; set; }
      public string AnnualLeaves { get; set; }

    }
    public class EmailAndIDs
    {
      public int? userId { get; set; }
      public string email { get; set; }
      public int? userLeavePolicyID { get; set; }
      public string UserID { get; set; }
      public string EmployeeName { get; set; }
      public string DepartmentName { get; set; }

      public Nullable<System.DateTime> JoiningDate { get; set; }
    }
    public List<EmailAndIDs> GetUserEmailsAndIDs(string managerEmail)
    {
      using (var context = new LeaveONEntities())
      {
        //  var users = context.AspNetUsers.Where(y => y.CntryName != "Pakistan" && (y.ManagerID.ToLower() == managerEmail.ToLower() || y.Manager2ID.ToLower() == managerEmail.ToLower()))
        // users = users.Where(k => k.UserID == "2840417a-7247-44bf-bf71-0e98ae6bb956").ToList();
        //test
        // var users = context.AspNetUsers.Where(y => y.ManagerID.ToLower() == managerEmail.ToLower() && y.IsActive == true && (y.Id == "32044600-a0de-47c8-910e-d859c71ea97b"))
        //live
        var users = context.AspNetUsers.Where(y => y.BioStarEmpNum.HasValue && y.BioStarEmpNum.Value > 0 && y.ManagerID.ToLower() == managerEmail.ToLower() && y.IsActive == true && y.IsDeleted != true && !String.IsNullOrEmpty(y.DepartmentName))
        .Select(x => new EmailAndIDs
        {
          userId = x.BioStarEmpNum.Value,
          email = x.Email,
          userLeavePolicyID = x.UserLeavePolicyId,
          UserID = x.Id,
          JoiningDate = x.JoiningDate,
          EmployeeName = x.EmpolyeeName,
          DepartmentName = x.DepartmentName
        })
        .ToList();

        return users;
      }

    }

    public List<EmailAndIDs> GetUserDepartmentWise(string deparmentName)
    {
      using (var context = new LeaveONEntities())
      {
        //  var users = context.AspNetUsers.Where(y => y.CntryName != "Pakistan" && (y.ManagerID.ToLower() == managerEmail.ToLower() || y.Manager2ID.ToLower() == managerEmail.ToLower()))
        // users = users.Where(k => k.UserID == "2840417a-7247-44bf-bf71-0e98ae6bb956").ToList();
        //test
        // var users = context.AspNetUsers.Where(y => y.ManagerID.ToLower() == managerEmail.ToLower() && y.IsActive == true && (y.Id == "32044600-a0de-47c8-910e-d859c71ea97b"))
        //live
        var users = context.AspNetUsers.Where(y => y.BioStarEmpNum.HasValue && y.BioStarEmpNum.Value > 0 && !String.IsNullOrEmpty(y.DepartmentName) && y.DepartmentName.Trim().ToLower() == deparmentName.ToLower() && y.IsActive == true && y.IsDeleted != true)
        .Select(x => new EmailAndIDs
        {
          userId = x.BioStarEmpNum.Value,
          email = x.Email,
          userLeavePolicyID = x.UserLeavePolicyId,
          UserID = x.Id,
          JoiningDate = x.JoiningDate,
          EmployeeName = x.EmpolyeeName,
          DepartmentName = x.DepartmentName
        })
        .ToList();

        return users;
      }

    }

    public List<EmailAndIDs> GetUsersWithLeavePolicyByManager(string managerEmail)
    {
      using (var context = new LeaveONEntities())
      {
        var validPolicyIds = new[] { 1047, 1048 };

        var users = context.AspNetUsers
            .Where(user =>
                (user.ManagerID.ToLower() == managerEmail.ToLower() ||
                 user.Manager2ID.ToLower() == managerEmail.ToLower()) &&
                 user.IsActive == true &&
                user.UserLeavePolicyId.HasValue &&
                validPolicyIds.Contains(user.UserLeavePolicyId.Value))
            .Select(x => new EmailAndIDs
            {
              userId = x.BioStarEmpNum.Value,
              email = x.Email,
              userLeavePolicyID = x.UserLeavePolicyId,
              UserID = x.Id,
            })
            .ToList();

        return users;
      }
    }

    public List<EmailAndIDs> GetLegitemacyChckers()
    {
      using (var context = new LeaveONEntities())
      {
        int[] legitimacyCheckers = new int[1] { 2696 }; // these are the biostar numbers of legitimacy checkers

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

    // Get Monthly report data

    public Task GetMonthlyReportData(int month, int year, bool legitimacyCheckForReports)
    {
      string monthName = GetMonthName(month);
      List<DepartmentName> departmentList = GetDepartmentList();
      var legitimacyCheckers = GetLegitemacyChckers();
      List<ManagerDto> managerEmails = new List<ManagerDto>();

      managerEmails = GetManagersIDs();

      List<string> managerids = new List<string>();
      if (managerEmails.Any())
      {
        managerids = managerEmails.GroupBy(k => k.Id).Select(k => k.FirstOrDefault().Id).ToList();
      }
      //managerids = new List<string>
      //    {
      //  "6c75398c-4f4c-4ff5-baed-814c75138588"

      //      };

      managerEmails = GetManagersListById(managerids);

      foreach (var managerEmail in managerEmails)
      {
        using (var context = new LeaveONEntities())
        {
          var users = GetUserEmailsAndIDs(managerEmail.Id);

          ///users = users.Where(k => k.userId == 1080).ToList();
          if (users == null || users.Count == 0)
            continue;
          List<EmployeeReportData> reportList = BindUserMonthReportData(month, year, managerEmail, context, users);
          string hRBPEmail = String.Empty;
          if (managerEmail != null && !string.IsNullOrEmpty(managerEmail.DepartmentName))
          {
            hRBPEmail = "";// departmentList.FirstOrDefault(k => !string.IsNullOrEmpty(k.Name) && k.Name.ToLower() == managerEmail.DepartmentName.ToLower()).HRBPEmail;
          }

          GeneratePDFManager1(
              reportList,
              reportList.Count > 0 ? reportList[0].TotalDays : 0,
              monthName,
              year,
              managerEmail,
              legitimacyCheckForReports,
              legitimacyCheckers, hRBPEmail, false);
        }
      }

      return Task.CompletedTask;
    }

    private static List<EmployeeReportData> BindUserMonthReportData(int month, int year, ManagerDto managerEmail, LeaveONEntities context, List<EmailAndIDs> users)
    {
      List<int> policyIds = new List<int>();
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
      var userIds = users.Select(u => u.UserID).ToList();
      //var policyIds = users.Where(u => u.userLeavePolicyID.HasValue)
      //                     .Select(u => u.userLeavePolicyID.Value)
      //                     .Distinct()
      //                     .ToList();
      policyIds = users.Where(u => u.userLeavePolicyID.HasValue)
                           .Select(u => u.userLeavePolicyID.Value)
                           .Distinct()
                           .ToList();
      var attendanceMonth = context.AttendanceDatas
          .Where(a => userIds.Contains(a.UserID)
                   && a.CreatedDate >= startOfMonth
                   && a.CreatedDate <= endOfMonth)
          .ToList();

      //string policyName = String.Empty;

      List<String> policyName = new List<String>();
      List<UserLeavePolicy> userpolicyList = new List<UserLeavePolicy>();

      if (attendanceMonth != null && attendanceMonth.Count() > 0)
      {
        policyName = attendanceMonth.Select(u => u.UserLeavePolicyID).Distinct().ToList();
      }
      if (policyName.Where(k => !string.IsNullOrEmpty(k)).Count() > 0)
      {
        userpolicyList = context.UserLeavePolicies.Where(p => policyName.Any(l => l.Trim().ToLower() == p.Description.Trim().ToLower())).ToList();
        if (policyIds.Count() > 0)
        {
          policyIds.AddRange(userpolicyList.Select(u => u.Id).Distinct().ToList());
        }
        else
        {

          policyIds = userpolicyList.Select(u => u.Id).Distinct().ToList();
        }
      }
      else
      {
        policyIds = users.Where(u => u.userLeavePolicyID.HasValue)
                           .Select(u => u.userLeavePolicyID.Value)
                          .Distinct()
                           .ToList();
      }







      var cDate = DateTime.Today;

      var attendanceYear = context.AttendanceDatas
          .Where(a => userIds.Contains(a.UserID)
                   && a.CreatedDate.HasValue
                   && a.CreatedDate < cDate)
          .ToList();

      var leaves = context.Leaves
          .Where(l => userIds.Contains(l.UserId)
                   && l.IsAccepted1 == 1
                   && l.IsAccepted2 == 1)
          .ToList();

      var leavePolicies = context.UserLeavePolicies
          .Where(p => policyIds.Contains(p.Id))
          .ToList();

      var leavePolicyDetails = context.UserLeavePolicyDetails
          .Where(p => policyIds.Contains((int)p.UserLeavePolicyId))
          .ToList();

      var holidays = context.AnnualOffDays
          .Where(o => policyIds.Contains((int)o.UserLeavePolicyId))
          .ToList();
      List<EmployeeReportData> reportList = new List<EmployeeReportData>();
      foreach (var user in users)
      {
        if (!user.userLeavePolicyID.HasValue)
          continue;

        int userIdInt = Convert.ToInt32(user.userId);
        var userAttendanceMonth = attendanceMonth
           .Where(a => a.BioStarEmpNum == userIdInt && a.CreatedDate >= user.JoiningDate)
           .ToList();
        DateTime fiscalStart = new DateTime();
        DateTime fiscalEnd = new DateTime();
        String attedancePolicy = userAttendanceMonth.Count() > 0 ? userAttendanceMonth.FirstOrDefault().UserLeavePolicyID : String.Empty;
        UserLeavePolicy policy = new UserLeavePolicy();

        if (!String.IsNullOrEmpty(attedancePolicy))
        {
          policy = leavePolicies.FirstOrDefault(k => k.Description.ToLower().Trim() == attedancePolicy.ToLower().Trim()); //leavePolicies.First(p => p.Id == user.userLeavePolicyID.Value);
          fiscalStart = policy.FiscalYearStart.Value;
          fiscalEnd = policy.FiscalYearEnd.Value;
        }
        else
        {
          policy = leavePolicies.First(p => p.Id == user.userLeavePolicyID.Value);
          fiscalStart = policy.FiscalYearStart.Value;
          fiscalEnd = policy.FiscalYearEnd.Value;
        }






        //for missing get date list
        List<DateTime> workingMonthDateList = GetWorkingDayByMonth(year, month, user.userLeavePolicyID);
        DateTime currentDate = (DateTime.Now).AddDays(-1);
        workingMonthDateList = workingMonthDateList.Where(k => k.Date < currentDate.Date).ToList();
        List<DateTime> missedMonthAttendanceDateList = new List<DateTime>();
        int totalWorkingDaysCount = userAttendanceMonth.Where(k => k.TotalWorkHours.HasValue && k.TotalWorkHours.Value > 0).Count();

        if (workingMonthDateList.Count > totalWorkingDaysCount)

        {

          var list = workingMonthDateList.Where(date => date.Date < currentDate.Date && !userAttendanceMonth.Any(j => j.CreatedDate.Value.Date == date.Date)).ToList();


          var monthyLeaves = leaves
              .Where(l => l.UserId == user.UserID
                       && (l.LeaveTypeId == 1 || l.LeaveTypeId == 2)
                       && l.IsAccepted1 == 1
                       && l.IsAccepted2 == 1
                       && l.StartDate >= startOfMonth
                       && l.EndDate <= endOfMonth)
              .ToList();

          list = list.Where(k => !monthyLeaves.Any(g => g.DateCreated.Value.Date == k.Date.Date)).ToList();
          missedMonthAttendanceDateList.AddRange(list);
        }

        var userAttendanceYTD = attendanceYear
            .Where(a => a.BioStarEmpNum == userIdInt

                     && a.CreatedDate >= fiscalStart
                     && a.CreatedDate <= fiscalEnd && a.CreatedDate >= user.JoiningDate)
            .ToList();


        DateTime invalidDate = new DateTime(1, 1, 1);

        var validPunchIns = userAttendanceMonth
            .Where(a => a.FirstPunchIn.HasValue && a.FirstPunchIn.Value != invalidDate)
            .Select(a => a.FirstPunchIn.Value.TimeOfDay.TotalSeconds);

        var validPunchOuts = userAttendanceMonth
            .Where(a => a.LastPunchOut.HasValue && a.LastPunchOut.Value != invalidDate)
            .Select(a => a.LastPunchOut.Value.TimeOfDay.TotalSeconds);

        TimeSpan avgTimeIn = validPunchIns.Any()
            ? TimeSpan.FromSeconds(validPunchIns.Average())

            : TimeSpan.Zero;

        TimeSpan avgTimeOut = validPunchOuts.Any()
            ? TimeSpan.FromSeconds(validPunchOuts.Average())
            : TimeSpan.Zero;

        var publicHolidays = holidays
            .Where(h => h.UserLeavePolicyId == policy.Id
                     && h.OffDay >= fiscalStart
                     && h.OffDay <= fiscalEnd)
            .Select(h => h.OffDay.Value)
            .ToHashSet();

        var userLeavesYTD = leaves
              .Where(l => l.UserId == user.UserID
                       && (l.LeaveTypeId == 1 || l.LeaveTypeId == 2)
                       && l.IsAccepted1 == 1
                       && l.IsAccepted2 == 1
                       && l.StartDate >= fiscalStart
                       && l.EndDate <= fiscalEnd)
              .ToList();

        var absenteesYTD = leaves
              .Where(l => l.UserId == user.UserID
                       && l.IsAccepted1 == 1
                       && l.IsAccepted2 == 1
                       && l.StartDate >= fiscalStart
                       && l.EndDate <= fiscalEnd).ToList();

        int absentYTD = userAttendanceYTD.Count(a =>
            a.IsAbsent == true
            && !publicHolidays.Contains(a.CreatedDate.Value.Date)
            && !absenteesYTD.Any(l => a.CreatedDate >= l.StartDate && a.CreatedDate <= l.EndDate));
        decimal availedLeaveYTD = userLeavesYTD.Sum(l => (decimal)l.TotalDays);


        int shortHoursYTD = (int)context.Leaves
            .Where(l => l.UserId == user.UserID
                     && l.IsShortLeave == true
                       && l.IsAccepted1 == 1
                       && l.IsAccepted2 == 1
                     && l.StartDate >= fiscalStart
                     && l.EndDate <= fiscalEnd)
            .AsEnumerable()
            .Sum(l => (l.EndDate - l.StartDate).TotalHours);
        int shortDaysToCausalLeave = 0;

        if (shortHoursYTD >= 8)
        {
          shortDaysToCausalLeave = (int)shortHoursYTD / 8;
          shortHoursYTD = shortHoursYTD % 8;
          availedLeaveYTD = availedLeaveYTD + shortDaysToCausalLeave;
        }




        int totalWorkDays = GetWorkingDays(year, month, user.userLeavePolicyID);

        //  this if user not mark attensance on  userAttendanceMonth then it will as absent
        if (missedMonthAttendanceDateList.Count() > 0)
        {
          absentYTD += missedMonthAttendanceDateList.Count();// (totalWorkDays - userAttendanceMonth.Count());
        }

        double totalWorkSeconds = userAttendanceMonth
            .Where(a => a.TotalWorkHours.HasValue && a.TotalWorkHours.Value > 0)
            .Select(a => a.TotalWorkHours.Value)
            .Distinct()
            .Sum();
        int workingDays = userAttendanceMonth
            .Where(a => a.TotalWorkHours.HasValue && a.TotalWorkHours.Value > 0)
            .Select(a => a.CreatedDate.Value.Date)
            .Distinct()
            .Count();

        TimeSpan avgTimeInOffice = workingDays > 0
            ? TimeSpan.FromSeconds(totalWorkSeconds / workingDays)
            : TimeSpan.Zero;

        DateTime joiningDate = user.JoiningDate ?? fiscalStart;
        DateTime effectiveStart = joiningDate > fiscalStart ? joiningDate : fiscalStart;

        int workedMonths = joiningDate > fiscalEnd ? 0 :
            ((fiscalEnd.Year - effectiveStart.Year) * 12)
            + fiscalEnd.Month - effectiveStart.Month + 1;

        if (workedMonths > 12 || effectiveStart == fiscalStart) workedMonths = 12;

        var policyDetail = leavePolicyDetails
            .Where(p => p.UserLeavePolicyId == policy.Id)
            .ToList();
        int casual = 0;
        int annual = 0;
        if (policyDetail.Where(p => p.LeaveTypeId == 1).Count() > 0)
          casual = (int)policyDetail.Where(p => p.LeaveTypeId == 1)?.Select(p => p.Allowed)?.FirstOrDefault();
        if (policyDetail.Where(p => p.LeaveTypeId == 2).Count() > 0)
          annual = (int)policyDetail.Where(p => p.LeaveTypeId == 2)?.Select(p => p.Allowed)?.FirstOrDefault();

        if (workedMonths < 12)
        {
          casual = (int)Math.Ceiling((casual / 12.0) * workedMonths);
          annual = (int)Math.Ceiling((annual / 12.0) * workedMonths);
        }

        int assignedLeaveQuota = casual + annual;
        int availableLeave = assignedLeaveQuota - (int)availedLeaveYTD;



        string empname = string.Empty;

        if (!String.IsNullOrEmpty(user.EmployeeName))
        {
          empname = user.EmployeeName;
        }
        else
        {
          empname = !String.IsNullOrEmpty(user.EmployeeName) ? user.email.Split('@')[0].Replace('.', ' ') : string.Empty;
        }
        reportList.Add(new EmployeeReportData
        {
          EmployeeID = user.userId,
          EmployeeName = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().UserName : empname,
          Department = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().DepartmentName : user.DepartmentName,
          CountryName = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().CountryName : String.Empty,
          ManagerEmail = managerEmail.Email,

          TotalDays = totalWorkDays,
          AbsentDays = absentYTD, // Absents (YTD)
          AvailedLeave = (int)availedLeaveYTD, // Availed Leave (YTD)

          TotalWorkHours = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Sum(a => a.TotalWorkHours) : 0,
          TotalBreakHours = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Sum(a => a.BreakHours) : 0,

          LateArrivals = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Count(a => a.IsLateArrival == true) : 0,
          EarlyDepartures = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Count(a => a.IsEarlyDeparture == true) : 0,

          AverageTimeIn = avgTimeIn.ToString(@"hh\:mm"),
          AverageTimeOut = avgTimeOut.ToString(@"hh\:mm"),
          AverageTimeInOffice = avgTimeInOffice.ToString(@"hh\:mm"),

          ShortHoursInMonth = shortHoursYTD > 0 ? shortHoursYTD.ToString() : "0", // Short Hours (YTD)

          AssignedLeaveQuota = assignedLeaveQuota.ToString(),
          AvailableLeave = availableLeave > 0 ? availableLeave.ToString() : "0",
          CasualLeaves = casual.ToString(),
          AnnualLeaves = annual.ToString()
        });
      }

      return reportList;
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
        //mail.Bcc.Add(new MailAddress("saeed.dev125@gmail.com"));
        //  mail.To.Add(new MailAddress(userEmail));
        mail.Subject = $"Monthly Attendance Report";
        mail.Body = $"Dear {reportData.EmployeeName},\n\nPlease find the attached attendance report for your review. If you have any questions or need further clarification, please feel free to reach out. \n\nBest regards,\n";
        Console.WriteLine($"Sending email to => {userEmail}");
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
      workingDays = workingDays - annualOffDays.Count();
      //foreach (var offDay in annualOffDays)
      //{
      //  workingDays--;
      //}

      return workingDays;
    }


    public static List<DateTime> GetWorkingDayByMonth(int year, int month, int? userLeavePolicyID)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      List<DateTime> workingDates = new List<DateTime>();


      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        {
          workingDates.Add(date);
        }
      }

      var context = new LeaveONEntities();
      // Get all off days for the user that fall within the specified month and year
      var annualOffDays = context.AnnualOffDays
                                  .Where(x => x.UserLeavePolicyId == userLeavePolicyID
                                          && x.OffDay >= startOfMonth
                                          && x.OffDay <= endOfMonth).Select(k => k.OffDay)
                                  .ToList();


      // Subtract off days that are weekdays
      workingDates.RemoveAll(date => annualOffDays.Contains(date));

      return workingDates;
    }
    // Generate Manager Report



    public void GeneratePDFManager1(
        List<EmployeeReportData> reportData,
        int totalWorkDays,
        string monthName,
        int year,
        ManagerDto managerDetail,
        bool legitimacyCheckForReports,
        List<EmailAndIDs> legitimacyCheckers, string hrBpEmail, bool isHOD)
    {
      if (reportData == null || reportData.Count == 0)
      {
        Console.WriteLine("No report data available. Skipping email generation.");
        return;
      }

      using (var context = new LeaveONEntities())
      {

        if (string.IsNullOrEmpty(managerDetail.Email))
          return;

        string managerName = managerDetail.Email.Split('@')[0].Replace('.', ' ');

        SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
        {
          UseDefaultCredentials = false,
          Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
          Port = 587,
          EnableSsl = true
        };

        MailMessage mail = new MailMessage
        {
          From = new MailAddress(LeavON_Email),
          Subject = string.Format("Monthly Attendance Summary Report - {0}", monthName),
          Body = string.Format(
                "Dear {0},\n\nPlease find the attached attendance report for your review.\n\nBest regards,\n",
                CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerName))
        };

        //if (legitimacyCheckForReports)
        //{
        //  foreach (var checker in legitimacyCheckers)
        //    mail.To.Add(checker.email);
        //}
        //else
        //{
        // mail.Bcc.Add("laiba.khan@intechww.com");
        // TO

        if (!String.IsNullOrEmpty(managerDetail.Email))
        {
           mail.To.Add("laiba.khan@intechww.com");
          // mail.To.Add("esswaqas@hotmail.com");
        }
        // CC
        if (!String.IsNullOrEmpty(hrBpEmail))
        {
          //mail.CC.Add("esswaqas@hotmail.com");
        }

        // BCC
        // mail.Bcc.Add("laiba.khan@intechww.com");
        //mail.Bcc.Add("esswaqas@hotmail.com");

        //}

        using (var memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A3, 50, 50, 25, 25);
          PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          // ================= HEADER TABLE (UNCHANGED LAYOUT) =================

          PdfPTable territoryTable = new PdfPTable(3);
          territoryTable.WidthPercentage = 100;

          Font titleFont = new Font(Font.FontFamily.HELVETICA, 20, Font.BOLD, BaseColor.WHITE);
          int monthNo = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;
          int monthDays = DateTime.DaysInMonth(year, monthNo);
          string shortYear = string.Format("'{0:00}", year % 100);

          PdfPCell titleCell = new PdfPCell(new Phrase(
              string.Format("Attendance Report - {0} {1} ", monthName, shortYear),
              titleFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 10
          };
          territoryTable.AddCell(titleCell);

         
           Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
           
          PdfPCell headerCell = new PdfPCell(new Phrase("Country-wise Working Days & Official Holidays", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9
          };
          territoryTable.AddCell(headerCell);

          string[] territoryHeaders = { "Territory", "Working days", "Public / Official Holidays" };
          Font thFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);

          foreach (string h in territoryHeaders)
          {
            territoryTable.AddCell(new PdfPCell(new Phrase(h, thFont))
            {
              HorizontalAlignment = Element.ALIGN_CENTER,
              BackgroundColor = BaseColor.GRAY,
              Padding = 5
            });
          }

          var countryList = context.CountryNames
              .Where(c => c.Name != "United Kingdom" && c.Name != "Singapore" && c.Name != "Qatar" && c.Name != "China" && c.Name != "São Tomé and Príncipe" && c.Name != "Gabon")
              .OrderByDescending(c => c.Name == "Pakistan")
              .ToList();

          Font tdFont = new Font(Font.FontFamily.HELVETICA, 12);

          foreach (var country in countryList)
          {
            int monthNumber = monthNo;
            DateTime mStart = new DateTime(year, monthNumber, 1);
            DateTime mEnd = mStart.AddMonths(1).AddDays(-1);

            int? policyId = context.AspNetUsers
                .Where(u => u.CntryName == country.Name)
                .Select(u => u.UserLeavePolicyId)
                .FirstOrDefault();

            int holidayCount = 0;
            if (policyId.HasValue)
            {
              ////holidayCount = context.AnnualOffDays.Count(o =>
              ////    o.UserLeavePolicyId == (policyId== 2084? 2055: policyId) && o.OffDay >= mStart && o.OffDay <= mEnd);
              holidayCount = context.AnnualOffDays
    .Where(o => o.UserLeavePolicyId == (policyId == 2084 ? 2055 : policyId)
             && o.OffDay >= mStart
             && o.OffDay <= mEnd)
    .Select(o => o.OffDay)
    .Distinct()
    .Count();
            }

            int workingDays = GetWorkingDaysByCountry(year, monthNumber, country.Name) - holidayCount;

            territoryTable.AddCell(new PdfPCell(new Phrase(country.Name, tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            territoryTable.AddCell(new PdfPCell(new Phrase(workingDays.ToString(), tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            territoryTable.AddCell(new PdfPCell(new Phrase(holidayCount.ToString(), tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
          }

          document.Add(territoryTable);
          territoryTable.SpacingAfter = 20f;

          // ================= MAIN ATTENDANCE TABLE =================

          PdfPTable table = new PdfPTable(new float[]
              {
                  3f,   // ID
                  8f,   // Name
                  4.7f, // Casual Leave
                  4.9f, // Annual Leave
                  6f,   // Assigned Leave Quota
                  5f,   // Availed Leave (YTD)
                  6f,   // Available Leave Balance
                  6.2f, // Absents (YTD)
                  5f,   // Short Hours (YTD)
                  5.5f, // Avg. Entry Time
                  5.3f, // Avg. Exit Time
                  5.5f  // Avg. Time In Office
              });
          table.WidthPercentage = 100;
          table.SpacingBefore = 20f;






          PdfPCell managerCell = new PdfPCell(
            isHOD == true ?
                   new Phrase(

   string.Format("Head of Department - {0}", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerName)), headerFont)
                   :
            new Phrase(

   string.Format("Manager - {0}", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerName)), headerFont)
            )
          {
            Colspan = 12,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9,
           // /BorderWidthTop = 3f,
            //BorderWidthLeft = 3f,
             //BorderWidthRight = 3f
          };
          table.AddCell(managerCell);

          string[] headers = {
                "\n ID", "\n Name", "Casual \n Leave", "Annual \n Leave", "Assigned\n Leave\n Quota",
                "Availed Leave (YTD)", "Available\n Leave\n Balance", "Absents (YTD)",
                "Short Hours (YTD)", " Avg. Entry Time", " Avg. Exit Time", " Avg. Time\n In Office"
            };

          Font hf = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
          foreach (string h in headers)
          {
            table.AddCell(new PdfPCell(new Phrase(h, hf))
            {
              HorizontalAlignment = Element.ALIGN_CENTER,
              BackgroundColor = BaseColor.GRAY,
              Padding = 6
            });
          }

          Font dataFont = new Font(Font.FontFamily.HELVETICA, 12);
          string currentDept = null;

          foreach (var d in reportData.OrderBy(x => x.Department).ThenBy(x => x.EmployeeName))
          {
            if (currentDept != d.Department)
            {
              currentDept = d.Department;
              table.AddCell(new PdfPCell(new Phrase(currentDept, dataFont))
              {
                Colspan = 12,
                BackgroundColor = new BaseColor(200, 200, 200),
                Padding = 5,
                //BorderWidthLeft = 0f,
                //BorderWidthRight = 0f
                 
              });
            }

            table.AddCell(new PdfPCell(new Phrase(d.EmployeeID.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER,   });
            table.AddCell(new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(d.EmployeeName), dataFont)));
            table.AddCell(new PdfPCell(new Phrase(d.CasualLeaves, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AnnualLeaves, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AssignedLeaveQuota, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AvailedLeave.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AvailableLeave, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AbsentDays.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.ShortHoursInMonth, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeIn, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeOut, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeInOffice, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
          }

          document.Add(table);

          //document.NewPage();

          // Add space from top
          Paragraph space = new Paragraph(" ");
          space.SpacingBefore = 30f;
          document.Add(space);

          Font notesTitleFont = new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD);
          Font notesFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);

          Paragraph notesTitle = new Paragraph(
              "Definations:",
              notesTitleFont);

          notesTitle.Alignment = Element.ALIGN_LEFT;
          notesTitle.SpacingAfter = 20f;

          document.Add(notesTitle);
          Font boldFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
          Font normalFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);

          //Paragraph p = new Paragraph();

          //p.SetLeading(0f, 1.8f); // Smaller multiplier
          //p.SpacingBefore = 0f;
          //p.SpacingAfter = 0f; ;
          //territoryTable.SpacingAfter = 20f;
          // Casual Leave* 
          document.Add(CreateNote("* Casual Leave / Annual Leave: ",   
           "Entitlement is based on the current year leave policy and will be prorated according to the employee’s joining date."));

          // Absents
          document.Add(CreateNote("* Absents (YTD): ", "Total absent days recorded year-to-date, calculated from the joining date up to the current fiscal year period."));

          // Short Hours
          document.Add(CreateNote("* Short Hours (YTD): ","Total casual short leave occurrences recorded year-to-date, prorated according to the employee’s joining date."));

          // Avg Entry
          document.Add(CreateNote("* Avg. Entry Time: ", "Average office arrival time based on monthly attendance logs and the employee’s login territory." ));

          // Avg Exit
          document.Add(CreateNote("* Avg. Exit Time: ","Average office departure time based on monthly attendance logs and the employee’s logout territory."));

          // Avg Office Time
          document.Add(CreateNote("* Avg. Time in Office: ","Average productive office duration per working day, calculated between check-in and check-out timings."));

          // Availed Leave
          document.Add(CreateNote("* Availed Leave (YTD): ","Total leaves utilized during the current year as per policy."));

          // Available Leave
          document.Add(CreateNote("* Available Leave Balance: ","Remaining leave balance after adjustment of utilized leaves as per policy.\n\n"));

          // Important Note
          document.Add(CreateNote("  Note: ", "Higher absenteeism may occur due to pending leave approvals, missing LMS entries, unmarked business trips, or pending attendance regularization requests.For any discrepancy, clarification, or correction, employees may contact their respective HR Business Partner(HRBP)."));


         // document.Add(p);

          document.Close();

          mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()),
              string.Format("ManagerReport_{0}.pdf", monthName), "application/pdf"));
        }

        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email sent successfully ...");
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
        }
      }
    }
    public Paragraph CreateNote(string title, string description)
    {
      Font boldFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
      Font normalFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);
      Paragraph p = new Paragraph();
      p.SetLeading(0f, 1.5f);      // Reduce line spacing
      p.SpacingAfter = 3f;         // Small gap between notes
      p.IndentationLeft = 10;     // Left margin
      p.FirstLineIndent = -9;    // Hanging indent

      p.Add(new Chunk(title + " ", boldFont));
      p.Add(new Chunk(description, normalFont));

      return p;
    }


    public void GeneratePDFManager(List<EmployeeReportData> reportData, int totalWorkDays, string monthName, bool legitimacyCheckForReports, List<EmailAndIDs> legitimacyCheckers, HashSet<string> sentEmails)
    {

      if (reportData == null || reportData.Count == 0)
      {
        Console.WriteLine("No report data available. Skipping email generation.");
        return; // Exit the method if no data to process
      }
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
      {
        UseDefaultCredentials = false,
        Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
        Port = 587,
        EnableSsl = true
      };
      var groupedByManager = reportData
     .GroupBy(data => string.IsNullOrEmpty(data.ManagerEmail) ? data.Manager2Email : data.ManagerEmail)
     .ToList();

      //   var groupedByManager = reportData
      //.GroupBy(data => data.ManagerEmail)
      //.ToList();
      //  var groupedByDepartment = reportData.GroupBy(emp => emp.Department.Trim()).ToList();


      foreach (var group in groupedByManager)
      {
        string managerEmail = group.Key;

        if (string.IsNullOrEmpty(managerEmail))
        {
          Console.WriteLine($"No email found for manager of these employees.");
          continue;
        }


        // Skip if email already processed
        if (sentEmails.Contains(managerEmail.ToLower()))
        {
          Console.WriteLine($"Skipping email for {managerEmail}, already processed.");
          continue;
        }


        // Add to processed emails
        sentEmails.Add(managerEmail.ToLower());

        MailMessage mail = new MailMessage
        {
          From = new MailAddress(LeavON_Email),
          Subject = $"Monthly Attendance Report",
          Body = $"Dear {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerEmail.Split('@')[0].Replace('.', ' '))},\n\nPlease find the attached attendance report for your review. If you have any questions or need further clarification, please feel free to reach out. \n\nBest regards,\n"
        };

        // Check if legitimacy checks are needed
        if (legitimacyCheckForReports)
        {
          foreach (var checker in legitimacyCheckers)
          {
            mail.To.Add(new MailAddress(checker.email));
          }
        }
        else
        {
          mail.To.Add(new MailAddress(managerEmail));
          //mail.To.Add(new MailAddress("laiba.khan @intechww.com"));

        }

        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A3, 50, 50, 25, 25);
          PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          //PdfPTable table = new PdfPTable(9); // Assuming 8 columns as before
          PdfPTable table = new PdfPTable(new float[] { 3, 2, 2, 2, 2, 2, 2, 2, 2 });
          table.WidthPercentage = 100;

          // Header
          Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
          PdfPCell headerCell = new PdfPCell(new Phrase($"Number of working days in {monthName}: {totalWorkDays}", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 8
          };
          table.AddCell(headerCell);
          string MangerName = managerEmail.Split('@')[0].Replace('.', ' ');
          PdfPCell headerCell2 = new PdfPCell(new Phrase($"Manager: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MangerName.ToLower())}", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9
          };
          table.AddCell(headerCell2);

          // Column headers
          string[] headers = { "Employee Name", "Employee ID", "Department", "Average Entry Time", "Average Exit Time", "Total Working Days", "Absent Days", "Work From Home Days", "Official Days Off" };
          Font headerFont2 = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
          foreach (string header in headers)
          {
            PdfPCell colHeaderCell = new PdfPCell(new Phrase(header, headerFont2))
            {
              BackgroundColor = new BaseColor(0, 76, 153),
              HorizontalAlignment = Element.ALIGN_CENTER,
              Padding = 5
            };
            table.AddCell(colHeaderCell);
          }

          // Data rows
          Font dataFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);
          Font nameFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
          BaseColor yellowColor = new BaseColor(255, 255, 0);

          foreach (var data in group)
          {
            PdfPCell cell;

            //cell = new PdfPCell(new Phrase(data.EmployeeName, nameFont))
            cell = new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.EmployeeName.ToLower()), nameFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.EmployeeID.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.Department, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AverageTimeIn, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AverageTimeOut, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase((totalWorkDays - data.AbsentDays).ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AbsentDays.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER,
              BackgroundColor = data.AbsentDays >= 3 ? yellowColor : BaseColor.WHITE
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.WorkFromHomeDays.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.OfficialDaysOff.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);
          }

          document.Add(table);
          document.Close();

          // Attach the PDF
          mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()), $"TeamAttendanceReport_{managerEmail}.pdf", "application/pdf"));
        }

        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email send successfully ...");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to send email to {(legitimacyCheckForReports ? "legitimacy checkers" : "")}: {ex.Message}");
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
    private List<string> GetPolicyWiseManagerIDs()
    {
      // return new List<string> { "c6d11b88-5ad0-4d7b-8c95-d5cd797e6e27" };

      using (var context = new LeaveONEntities())
      {
        var targetPolicyIDs = new[] { 1047, 1048 };

        // Step 1: Get all unique ManagerIDs and Manager2IDs from users
        var referencedManagerIds = context.AspNetUsers
            .SelectMany(user => new[] { user.ManagerID, user.Manager2ID })
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
           .ToList();

        // Step 2: From those referenced managers, filter those who have the required UserLeavePolicyId
        var validManagerIds = context.AspNetUsers
            .Where(manager =>
                referencedManagerIds.Contains(manager.Id) &&
                manager.UserLeavePolicyId.HasValue &&
                targetPolicyIDs.Contains(manager.UserLeavePolicyId.Value))
            .Select(manager => manager.Id)
            .Distinct()
            .ToList();

        // Optional: Debug output
        foreach (var managerID in validManagerIds)
        {
          Console.WriteLine($"ManagerID => {managerID}");
        }

        return validManagerIds;
      }
    }

    private List<DepartmentName> GetDepartmentList()
    {
      using (var context = new LeaveONEntities())
      {
        return context.DepartmentNames.ToList();
      }
    }
    private List<ManagerDto> GetManagersListById(List<string> list)
    {
      using (var context = new LeaveONEntities())
      {
        // return context.AspNetUsers.Where(u => list.Any(k=>k== u.Id )).ToList();
        return context.AspNetUsers
        .Where(u => list.Contains(u.Id))
        .Select(u => new ManagerDto
        {
          Id = u.Id,
          UserName = u.UserName,
          Email = u.Email,
          PhoneNumber = u.PhoneNumber,
          ManagerName = u.ManagerName,
          DepartmentName = u.DepartmentName
        })
        .ToList();
      }
    }
    private List<ManagerDto> GetManagersIDs()
    {
      using (var context = new LeaveONEntities())
      // using (var context = new LeaveONEntitiesTarget())

      {
        //"WELLHEAD & SKIDS", "PROJECT QA/QC", "PROJECT MONITORING & CONTROL", "iCSG", "G&A"
        // var allowedDepartments = new[] { "IS&T" , "Automation Solution", "Electricall solution","digital solution","cybersecurity"};
        //var allowedDepartments = new[] { "G&A", "ICSG", "Solution Centre", "sales", "Marketing", "ht"., "Human Resource" , "FINANCE & ACCOUNTS" };
        // "AUTOMATION SOLUTIONS","ELECTRICAL SOLUTIONS"
        var allowedDepartments = new[] { "WELLHEAD & SKIDS", "IIS", "Digital Solutions" };  //, "Solution Centre" , "Project Monitoring & Control" 
                                                                                            //done  "Sales", "iCSG","G&A" ,"WELLHEAD & SKIDS","AUTOMATION SOLUTIONS","ELECTRICAL SOLUTIONS"
                                                                                            //var allowedDepartments = new[] { "AUTOMATION SOLUTIONS", "Sales", "Solution Centre", "WELLHEAD & SKIDSa", "Central Engineering Department" };

        var managersIDs = from u in context.AspNetUsers
                          join m in context.AspNetUsers on u.ManagerID equals m.Id
                          where allowedDepartments.Contains(m.DepartmentName)
                                && u.IsActive == true
                                && u.IsDeleted != true
                          orderby u.DepartmentName
                          select new ManagerDto
                          {
                            Id = u.ManagerID,
                            UserName = u.UserName,
                            Email = u.Email,
                            PhoneNumber = u.PhoneNumber,
                            ManagerName = m.UserName,
                            DepartmentName = u.DepartmentName
                          };


        return managersIDs.Distinct().ToList();
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


    public static int GetWorkingDaysByCountry(int year, int month, string countryName)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      HashSet<DayOfWeek> weekendDays = GetWeekendDaysForCountry(countryName);

      int workingDays = 0;
      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (!weekendDays.Contains(date.DayOfWeek))
        {
          workingDays++;
        }
      }

      return workingDays;
    }

    private static HashSet<DayOfWeek> GetWeekendDaysForCountry(string countryName)
    {
      switch (countryName.Trim().ToLower())
      {
        case "egypt":
        case "iraq":
        case "oman":
        case "qatar":
        case "saudi arabia":
          return new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };

        case "united arab emirates":
          return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }; // changed in Jan 2022

        // All others follow standard Saturday–Sunday
        case "angola":
        case "germany":
        case "kazakhstan":
        case "nigeria":
        case "pakistan":
        case "singapore":
        case "united kingdom":
        case "united states":
        default:
          return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
      }
    }


    private List<ManagerDto> GetHoDPerson()
    {
      using (var context = new LeaveONEntities())
      {

        // var allowedDepartments = new[] { "WELLHEAD & SKIDS", "IIS", "Digital Solutions" };
        var hODDeparments = context.DepartmentNames.Where(k => k.HODID.HasValue).Select(k => k.HODID).ToList();


        var managersIDs = from m in context.AspNetUsers

                          where m.BioStarEmpNum > 0 && hODDeparments.Contains(m.BioStarEmpNum)
                               && m.IsActive == true
                               && m.IsDeleted != true

                          orderby m.DepartmentName
                          select new ManagerDto
                          {
                            Id = m.Id,
                            UserName = m.UserName,
                            Email = m.Email,
                            PhoneNumber = m.PhoneNumber,
                            ManagerName = m.ManagerName,
                            DepartmentName = m.DepartmentName
                          };


        return managersIDs.Distinct().ToList();
      }
    }
    public Task GetHODDeparmentReport(int month, int year)
    {
      string monthName = GetMonthName(month);
      // List<DepartmentName> departmentList = GetDepartmentList();

      List<ManagerDto> managerEmails = new List<ManagerDto>();

      managerEmails = GetHoDPerson();
      foreach (var managerEmail in managerEmails)
      {
        using (var context = new LeaveONEntities())
        {
          var users = GetUserDepartmentWise(managerEmail.DepartmentName).Where(k => k.UserID != managerEmail.Id).ToList();

          if (users == null || users.Count == 0)
            continue;
          List<EmployeeReportData> reportList = BindUserMonthReportData(month, year, managerEmail, context, users);
          string hRBPEmail = String.Empty;
          if (managerEmail != null && !string.IsNullOrEmpty(managerEmail.DepartmentName))
          {
            hRBPEmail = "";// departmentList.FirstOrDefault(k => !string.IsNullOrEmpty(k.Name) && k.Name.ToLower() == managerEmail.DepartmentName.ToLower()).HRBPEmail;
          }

          GeneratePDFManager1(
              reportList,
              reportList.Count > 0 ? reportList[0].TotalDays : 0,
              monthName,
              year,
              managerEmail,
              false,
              new List<EmailAndIDs>(), hRBPEmail, true);
        }
      }
      return Task.CompletedTask;
    }



  }
  public class ManagerDto
  {
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ManagerName { get; set; }
    public string DepartmentName { get; set; }
    public string HoDName { get; set; }
  }
}

