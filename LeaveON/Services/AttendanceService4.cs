using System;
using System.Threading.Tasks;
using TimeManagement.Models;
using LeaveON.UtilityClasses;
using Repository.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace LeaveON.Services
{
  public class AttendanceService4
  {
    List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042, 
                                                543734917, 538205733, 538205730 };
    private BioStarEntities dbBioStar = new BioStarEntities();
     LeaveONEntities dbLeaveOn = new LeaveONEntities(); 
     LeaveONEntitiesTarget dbLeaveOnTarget = new LeaveONEntitiesTarget();


    //public Task ConnectToDBandReturnAttendanceData(DateTime startDate, DateTime endDate)
    public async Task<List<TimeData>> ConnectToDBandReturnAttendanceData(DateTime startDate, DateTime endDate)

    {
      var overallStopwatch = Stopwatch.StartNew();
      Console.WriteLine("Connecting to database...");
      string countryName = string.Empty;
      string previousCountryName = string.Empty;
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan TotalTime = new TimeSpan();
      TimeSpan TotalWorkingHours = new TimeSpan();
      List<string> logg = new List<string>();

      //  List<AspNetUser> users = dbLeaveOn.AspNetUsers
      //.Where(u => u.CntryName != null &&
      //    (u.CntryName.ToLower() == "pakistan" || u.CntryName.ToLower() == "iraq"))
      //.ToList();

      
      
      
      
      
      
      
      
      
      List<AspNetUser> users = dbLeaveOn.AspNetUsers.ToList();
      //mohsin.ali@intechww.com
      //Emmanuel.Dakore@intechww.com
      // Osaid.Hafeez@intechww.com
      // List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(u => u.Email == "salman.ashraf@intechww.com").ToList();
      List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
      List<BreakHour> LstBreakHours = new List<BreakHour>();
      con.Open();
      foreach (var aspNetUser in users)
      {
        Console.WriteLine($"Processing data for Employee: {aspNetUser.UserName} (ID: {aspNetUser.BioStarEmpNum})");
        int UserId = aspNetUser.BioStarEmpNum.Value;
        cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id =" + UserId + " and devdt BETWEEN '" + startDate.ToString("yyyy-MM-dd") + "' AND '" + endDate.ToString("yyyy-MM-dd") + "' order by devdt", con);
        cmd.Parameters.AddWithValue("@UserId", UserId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);
        dr = cmd.ExecuteReader();
        string UserName = string.Empty;
        //processing
        UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
        string timeZone = string.Empty;
        string userGuidId = aspNetUser.Id;
        string userLeavePolicyDescription = string.Empty;
        if (string.IsNullOrEmpty(aspNetUser.CntryName))
        {
          logg.Add(aspNetUser.UserName); dr.Close(); continue;
        }
        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          if (aspNetUser.CountryName != null)
          {
            timeZone = aspNetUser.CountryName.TimeZone;
          }
          else
          {
            timeZone = "Pakistan"; 
          }
          countryName = aspNetUser.CntryName;
        }
        List<AttendanceRecord> records = new List<AttendanceRecord>();
        while (dr.Read())
        {
          records.Add(new AttendanceRecord
          {
            UserId = dr["user_id"].ToString(),
            DevDt = dr["devdt"] != DBNull.Value ? Convert.ToDateTime(dr["devdt"]) : default(DateTime),
            Bsevtdt = dr["bsevtdt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["bsevtdt"]) : null,
            DevId = dr["DEVID"] != DBNull.Value ? Convert.ToInt32(dr["DEVID"]) : 0,
            DevNm = dr["devnm"].ToString(),
            Details = LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"] ?? 0)) ? "IN" : "OUT"
          });
        }
        dr.Close();

        int rowsCount = records.Count;
        Console.WriteLine($"Retrieved {rowsCount} attendance records for Employee: {aspNetUser.UserName} , Created Date : {aspNetUser.DateCreated}");

        if (rowsCount <= 0) continue;
        DateTime firstDateTime = records.First().DevDt;
        DateTime lastDateTime = records.Last().DevDt;
        int firstDay = firstDateTime.Day;
        int lastDay = lastDateTime.Day;
        List<int> LstEmptyDays = new List<int>();

        //---
        TimeData attendance;
        DateTime timeIn;
        DateTime timeOut;
        DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
        DateTime lastTimeOut = firsTimeIn;
        DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        DateTime leaveDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        bool IsCardIn = false, IsCardOut = false;
        bool lastsemiIn = false;
        TimeSpan ThidDayWorkingHours = new TimeSpan();
        List<OffTimeDetial> LstOffTimeDetial = new List<OffTimeDetial>();
        int k = 0;//-1;
       

        for (int j = 0; j <= rowsCount - 1; j++)
        {
          string shortCountryName = records[j].DevNm.ToString().Substring(0, 3);

          if (records[j].DevNm.ToString().Substring(0, 4) == "NG-L")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Lagos";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "NG-P")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Port Harcourt";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN01")
          {
            timeZone = "Pakistan Standard Time";
            countryName = "Pakistan";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN03")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN04")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Angola";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN05")
          {
            timeZone = "Arab Standard Time";
            countryName = "Saudi Arabia";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN07")
          {
            timeZone = "Arab Standard Time";
            countryName = "Iraq";
          }
          else if (records[j].DevNm.ToString().Substring(0, 4) == "IN08")
          {
            timeZone = "Arab Standard Time";
            countryName = "United Arab Emirates";
          }
          else
          {
            switch (shortCountryName)
            {
              case "PK ":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "PAK":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "UAE":
                timeZone = "Arab Standard Time";
                countryName = "United Arab Emirates";
                break;
              case "KSA":
                timeZone = "Arab Standard Time";
                countryName = "Saudi Arabia";
                break;
              case "GBR":
                timeZone = "GMT Standard Time";
                countryName = "United Kingdom";
                break;
              case "USA":
                timeZone = "Central Standard Time";
                countryName = "United States";
                break;
              case "NGA":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "NG-":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "EGY":
                timeZone = "Egypt Standard Time";
                countryName = "Egypt";
                break;
              case "IRQ":
                timeZone = "Arabic Standard Time";
                countryName = "Iraq";
                break;
              case "OMN":
                timeZone = "Arabian Standard Time";
                countryName = "Oman";
                break;
              case "QAT":
                timeZone = "Arab Standard Time";
                countryName = "Qatar";
                break;
              case "AGO":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Angola";
                break;
              case "KAZ":
                timeZone = "West Asia Standard Time";
                countryName = "Kazakhstan";
                break;
              // Add more cases for other countries
              default:
                timeZone = aspNetUser.CountryName.TimeZone; // Or handle the default case based on your requirements
                countryName = aspNetUser.CountryName.Name;
                break;
            }
          }
          // Convert the device date/time to the appropriate timezone
          firstDateTime = ConvertToCountryTimeZoneNew(records[j].DevDt, timeZone);
          // Check for a change in date indicating a new day of attendance records
          if (firstDateTime.Day != firstDay)
          {
            // If there are valid time-in and time-out records for the previous day, calculate total time
            if (IsCardIn && IsCardOut)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
              bool countryChanged = false;
              var leaveName = "";
              var leaveType = 0;
              var leaveForDay = dbLeaveOn.Leaves.Where(x => x.UserId == aspNetUser.Id && DbFunctions.TruncateTime(x.StartDate) == DbFunctions.TruncateTime(firsTimeIn)).FirstOrDefault();
              if (leaveForDay != null)
              {
                var fetchleaveType = dbLeaveOn.LeaveTypes.Find(leaveForDay.LeaveTypeId);
                leaveName = fetchleaveType.Name;
                leaveType = fetchleaveType.Id;
              }
              else
              {
                leaveName = "";
              }
              if (countryName != previousCountryName && countryName != "" & previousCountryName != "")
              {
                countryChanged = true;
              }
              bool isAbsent = false;
              string status = null;

              LstTimeData.Add(TimeDataCreated(
                UserName,
                 UserId,
                 timeZone,
                 countryChanged ? previousCountryName : countryName,
                 userLeavePolicyDescription,
                 firsTimeIn,
                 lastTimeOut,
                 ThidDayWorkingHours,
                 aspNetUser.DepartmentName,
                 aspNetUser.Id,
                 aspNetUser.ManagerEmail,
                 aspNetUser.Manager2Email,
                 aspNetUser.ManagerID,
                 aspNetUser.Manager2ID,
                 status,
                 isAbsent,
                 leaveType,
                 leaveName
               ));
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
            }
            //re-intiallize variables to next date calculations
            firsTimeIn = lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
            ThidDayWorkingHours = new TimeSpan();
            IsCardIn = false; IsCardOut = false;
            firstDay = firstDateTime.Day;
          }

          k = j + 1;

          //get actual working hour of this date

          if ((k <= rowsCount - 1) && LstCardReadersIn.Contains(records[j].DevId) &&
                        !LstCardReadersIn.Contains(records[k].DevId) &&
                        records[j].DevDt.Day == firstDay &&
                        records[k].DevDt.Day == firstDay)
          {
            if (IsCardIn == false)
            {
              firsTimeIn = ConvertToCountryTimeZoneNew(records[j].DevDt, timeZone);
              lastTimeOut = ConvertToCountryTimeZoneNew(records[k].DevDt, timeZone);
              IsCardIn = true;
            }
            timeIn = ConvertToCountryTimeZoneNew(records[j].DevDt, timeZone);
            timeOut = ConvertToCountryTimeZoneNew(records[k].DevDt, timeZone);
            TimeSpan workingHour = (timeOut - timeIn);
            ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);
            if (IsCardIn == true && !LstCardReadersIn.Contains(records[k].DevId))
            {
              IsCardOut = true;
              lastTimeOut = ConvertToCountryTimeZoneNew(records[k].DevDt, timeZone);
            }
          }
          previousCountryName = countryName;
        }
        //add last date to list here as loop ended
        if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)
        {
          TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
          string status = null;
          bool isAbsent = false;
          int leaveTypeID = 0;
          string leaveName = null;

          LstTimeData.Add(TimeDataCreated(
                 UserName,
                 UserId,
                 timeZone,
                 countryName,
                 userLeavePolicyDescription,
                 firsTimeIn,
                 lastTimeOut,
                 ThidDayWorkingHours,
                 aspNetUser.DepartmentName,
                 aspNetUser.Id,
                 aspNetUser.ManagerEmail,
                 aspNetUser.Manager2Email,
                 aspNetUser.ManagerID,
                 aspNetUser.Manager2ID,
                 status,
                 isAbsent,
                 leaveTypeID,
                 leaveName
               ));
          TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
        }

        ////Leaves Processing
        List<TimeData> offDays = new List<TimeData>();
        // Modify the existing foreach loop to iterate over the generated list of weekend dates:
        List<DateTime> lstThisMonthsWeekEnds = GetWeekEndList(startDate, endDate, aspNetUser.UserLeavePolicy?.WeeklyOffDays ?? "6,0");
        foreach (DateTime weekEndDate in lstThisMonthsWeekEnds)
        {
          TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Date == weekEndDate.Date && x.EmployeeNumber == UserId);
          if (thisWeekEnd != null)
          {
            thisWeekEnd.Status = "Weekend";
          }
          else
          {
            string status = "Weekend";
            bool isAbsent = false;

            offDays.Add(LeaveANDAbsentTimeData(
            UserName,
            UserId,
            timeZone,
            countryName,
            aspNetUser.DepartmentName,
            userLeavePolicyDescription,
            weekEndDate,
            weekEndDate.ToString("dddd"),
            aspNetUser.Id,
            aspNetUser.ManagerEmail,
            aspNetUser.Manager2Email,
            aspNetUser.ManagerID,
            aspNetUser.Manager2ID,
            status,
            isAbsent
          ));
          }
        }
        LstTimeData.AddRange(offDays);
        // Iterate over each day within the given date range
        for (DateTime date = startDate; date < endDate; date = date.AddDays(1))
        {
          // Check if the current day is an annual off day
          AnnualOffDay annualOffDay = dbLeaveOn.AnnualOffDays.FirstOrDefault(x => x.OffDay == date && x.UserLeavePolicyId == aspNetUser.UserLeavePolicyId);

          // Look for existing attendance data for this date
          TimeData timeDataForDay = LstTimeData.Find(x => x.Date.Date == date.Date && x.EmployeeNumber == UserId);

          // If no attendance data, determine status and absence
          if (timeDataForDay == null)
          {
            string status = "Absent";
            bool isAbsent = true; // Default to true, indicating the employee was absent

            if (annualOffDay != null)
            {
              status = annualOffDay.Description;
              isAbsent = false; // Do not mark as absent since it's a holiday
            }
            // Create and add the attendance data
            attendance = new TimeData()
            {
              EmployeeName = UserName,
              EmployeeNumber = UserId,
              TimeZone = timeZone,
              CountryName = countryName,
              Policy = userLeavePolicyDescription,
              Department = aspNetUser.DepartmentName,
              Date = date,
              Day = date.DayOfWeek.ToString(),
              Status = status,
              isAbsent = isAbsent,
              UserID = aspNetUser.Id,
              ManagerEmail = aspNetUser.ManagerEmail,
              Manager2Email = aspNetUser.Manager2Email,
              ManagerID = aspNetUser.ManagerID,
              Manager2ID = aspNetUser.Manager2ID,
            };
            LstTimeData.Add(attendance);
          }
          else if (annualOffDay != null) // If there's existing data and it's an off day, update status to indicate work on holiday
          {
            timeDataForDay.Status = annualOffDay.Description + " - Worked";
          }
        }

        //check if there was a leave?
        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
          var leaveRecord = dbLeaveOn.Leaves
  .Where(x => x.StartDate <= date && x.EndDate >= date &&
              x.IsAccepted1 != null && x.IsAccepted2 != null && x.UserId == userGuidId)
  .Select(x => new { x.LeaveTypeId, x.LeaveType.Name })
  .FirstOrDefault();

          if (leaveRecord != null)
          {
            // If a leave record exists, retrieve the corresponding leave type name
            var leaveTypeName = dbLeaveOn.LeaveTypes
                .Where(l => l.Id == leaveRecord.LeaveTypeId)
                .Select(l => l.Name)
                .FirstOrDefault();
            string status = "Leave";
            bool isAbsent = false;

            LstTimeData.Add(TimeDataCreated(
            UserName,
            UserId,
            timeZone,
            countryName,
            userLeavePolicyDescription,
            firsTimeIn,
            lastTimeOut,
            ThidDayWorkingHours,
            aspNetUser.DepartmentName,
            aspNetUser.Id,
            aspNetUser.ManagerEmail,
            aspNetUser.Manager2Email,
            aspNetUser.ManagerID,
            aspNetUser.Manager2ID,
            status,
            isAbsent,
            leaveRecord.LeaveTypeId,
            leaveTypeName
           ));
          }
        }
      }

;

      ////to avaid showing current month all data which is not happend yet
     LstTimeData = LstTimeData.Where(itm => itm.Date <= DateTime.Now.Date).ToList();

      // Group by EmployeeNumber and Date to get distinct entries
      var distinctTimeData = LstTimeData
          .GroupBy(x => new { x.EmployeeNumber, x.Date.Date }) // Group by EmployeeNumber and Date
          .Select(g => g.FirstOrDefault()) // Select the first occurrence of each group
          .ToList();


      foreach (var item in distinctTimeData)
      {
        var value = dbLeaveOn.AttendanceDatas;
        // Check if the user arrives after 9:30 AM
        bool lateArrival = item.TimeIn.TimeOfDay > new TimeSpan(9, 30, 0);
        item.isLateArrival = lateArrival; 
        bool earlyDeparture = item.TimeOut.TimeOfDay < new TimeSpan(16, 45, 0);
        item.isEarlyDeparture = earlyDeparture;
        // Check if an attendance record already exists for this user on the same date
        Console.WriteLine("distinctTimeData: " + item.EmployeeName + " for a: " + item.EmployeeNumber + " " + item.Date);

        bool exists = dbLeaveOn.AttendanceDatas
            .Any(ad => ad.BioStarEmpNum == item.EmployeeNumber &&
                        DbFunctions.TruncateTime(ad.CreatedDate) == item.Date.Date);
        // find existing record
        var existingRecord = dbLeaveOn.AttendanceDatas
            .FirstOrDefault(ad => ad.BioStarEmpNum == item.EmployeeNumber &&
                                  DbFunctions.TruncateTime(ad.CreatedDate) == item.Date.Date);

        //if (!exists)
        if (existingRecord != null)
        {
          // Update existing record 
          try
          {
            existingRecord.UserName = item.EmployeeName;
            existingRecord.DepartmentName = item.Department;
            existingRecord.UserLeavePolicyID = item.Policy;
            existingRecord.FirstPunchIn = item.TimeIn;
            existingRecord.LastPunchOut = item.TimeOut;
            existingRecord.TotalWorkHours = (long)item.WorkingHours.TotalSeconds;
            existingRecord.BreakHours = (long)(item.TotalTime.TotalSeconds - item.WorkingHours.TotalSeconds);
            existingRecord.IsLateArrival = item.isLateArrival;
            existingRecord.IsEarlyDeparture = item.isEarlyDeparture;
            existingRecord.IsAbsent = item.isAbsent;
            existingRecord.IsLeave = item.leaveTypeID != 0 ? true : false;
            existingRecord.LeaveTypeID = item.leaveTypeID;
            existingRecord.LeaveType = item.leaveType;
            existingRecord.CountryName = item.CountryName;
            existingRecord.TimeZone = item.TimeZone;
            existingRecord.ManagerEmail = item.ManagerEmail;
            existingRecord.Manager2Email = item.Manager2Email;
            existingRecord.ManagerId = item.ManagerID;
            existingRecord.Manager2Id = item.Manager2ID;
            existingRecord.UserID = item.UserID;


            Console.WriteLine("Updated existing attendance for user: " + existingRecord.UserName + " date: " + existingRecord.CreatedDate);
          }
          catch (Exception ex)
          {
            Console.WriteLine("Error updating AttendanceData: " + ex.Message);
          }
        }
        else
        {
          // Add new record
          try
          {
            AttendanceData attendanceDataToFill = new AttendanceData
            {
              BioStarEmpNum = item.EmployeeNumber,
              UserName = item.EmployeeName,
              DepartmentName = item.Department,
              UserLeavePolicyID = item.Policy,
              CreatedDate = item.Date,
              FirstPunchIn = item.TimeIn,
              LastPunchOut = item.TimeOut,
              TotalWorkHours = (long)item.WorkingHours.TotalSeconds,
              BreakHours = (long)(item.TotalTime.TotalSeconds - item.WorkingHours.TotalSeconds),
              IsLateArrival = item.isLateArrival,
              IsEarlyDeparture = item.isEarlyDeparture,
              IsAbsent = item.isAbsent,
              IsLeave = item.leaveTypeID != 0 ? true : false,
              LeaveTypeID = item.leaveTypeID,
              LeaveType = item.leaveType,
              CountryName = item.CountryName,
              TimeZone = item.TimeZone,
              ManagerEmail = item.ManagerEmail,
              Manager2Email = item.Manager2Email,
              ManagerId = item.ManagerID,
              Manager2Id = item.Manager2ID,
              UserID = item.UserID,
            };
            dbLeaveOn.AttendanceDatas.Add(attendanceDataToFill);
            Console.WriteLine("Added the user: " + attendanceDataToFill.UserName + " for a: " + attendanceDataToFill.CreatedDate);
          }
          catch (Exception ex)
          {
            Console.WriteLine("Error adding AttendanceData: " + ex.Message);
          }
        }
      }

      try
      {
        Console.WriteLine("Saved");
        dbLeaveOn.SaveChanges();
        //Console.Read();
      }
      catch (DbEntityValidationException dbEx)
      {
        foreach (var validationErrors in dbEx.EntityValidationErrors)
        {
          foreach (var validationError in validationErrors.ValidationErrors)
          {
            Console.WriteLine("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("An error occurred while saving changes: " + ex.Message);
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner exception: " + ex.InnerException.Message);
        }
      }

      con.Close();
      overallStopwatch.Stop(); // Stop the overall timer
      Console.WriteLine($"Total execution time for ConnectToDBandFillAttendanceData: {overallStopwatch.ElapsedMilliseconds} ms");
      return LstTimeData;
      ;
    }

    private DateTime ConvertToCountryTimeZoneNew(DateTime dateTime, string timeZone)
    {
      TimeZoneInfo customeTimeZone;
      DateTime ConvertedDateTime;
      customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
      ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, customeTimeZone);
      return ConvertedDateTime;
    }
    protected List<DateTime> GetWeekEndList(DateTime startDate, DateTime endDate, string weekEndDays)
    {
      List<int> weekEndDayIndexes = weekEndDays.Split(',').Select(int.Parse).ToList();
      List<DateTime> weekEndDates = new List<DateTime>();
      while (startDate <= endDate)
      {
        if (weekEndDayIndexes.Contains((int)startDate.DayOfWeek))
        {
          weekEndDates.Add(startDate);
        }
        startDate = startDate.AddDays(1);
      }
      return weekEndDates;
    }

    private class AttendanceRecord
    {
      public string UserId { get; set; }
      public DateTime DevDt { get; set; }
      public DateTime? Bsevtdt { get; set; }
      public int DevId { get; set; }
      public string DevNm { get; set; }
      public string Details { get; set; }
    }

    private TimeData TimeDataCreated(
   string UserName,
   int BioStarEmpNum,
   string timeZone,
   string countryName,
   string userLeavePolicyDescription,
   DateTime firsTimeIn,
   DateTime lastTimeOut,
   TimeSpan ThidDayWorkingHours,
   string depName,
   string userID,
   string managerEmail,
   string manager2Email,
   string managerId,
   string manger2Id,
   string status,
   bool isAbsent,
   int leaveTypeID,
   string leaveName
  )
    {
      return new TimeData()
      {
        EmployeeName = UserName,
        EmployeeNumber = BioStarEmpNum,
        TimeZone = timeZone,
        CountryName = countryName,
        Policy = userLeavePolicyDescription,
        Date = firsTimeIn.Date,
        Day = firsTimeIn.DayOfWeek.ToString(),
        TimeIn = firsTimeIn,
        TimeOut = lastTimeOut,
        WorkingHours = ThidDayWorkingHours,
        TotalTime = (lastTimeOut - firsTimeIn),
        Department = depName,
        UserID = userID,
        ManagerEmail = managerEmail,
        Manager2Email = manager2Email,
        ManagerID = managerId,
        Manager2ID = manger2Id,
        Status= status,
        isAbsent = isAbsent,
        leaveTypeID = leaveTypeID,
        leaveType = leaveName
      };
    }

    private TimeData LeaveANDAbsentTimeData(
  string UserName,
  int BioStarEmpNum,
  string timeZone,
  string countryName,
  string depName,
  string userLeavePolicyDescription,
  DateTime date,
  string day,
  string userID,
  string managerEmail,
  string manager2Email,
  string managerId,
  string manger2Id,
  string status,
  bool isAbsent
 )
    {
      return new TimeData()
      {
        EmployeeName = UserName,
        EmployeeNumber = BioStarEmpNum,
        TimeZone = timeZone,
        CountryName = countryName,
        Policy = userLeavePolicyDescription,
        Date = date,
        Day = day,
        Department = depName,
        UserID = userID,
        ManagerEmail = managerEmail,
        Manager2Email = manager2Email,
        ManagerID = managerId,
        Manager2ID = manger2Id,
        Status = status,
        isAbsent = isAbsent
      };
    }

  }
}
