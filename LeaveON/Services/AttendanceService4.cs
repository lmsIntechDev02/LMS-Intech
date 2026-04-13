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
using static LeaveON.Services.BreakHoursService;

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

    public async Task<List<AspNetUser>> GetUserActiveList()
    {
      return dbLeaveOn.AspNetUsers.Where(x => x.IsActive == true && x.IsDeleted != true).ToList();
    }


    public async Task<List<TimeData>> ConnectToDBandReturnAttendanceData(List<TimeData> LstTimeData)

    {
      try
      {
        Stopwatch overallStopwatch;
        SqlConnection con;
        //List<TimeData> LstTimeData;
        //NewMethod(startDate, endDate, out overallStopwatch, out con, out LstTimeData);


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
          bool lateArrival = item.TimeIn != DateTime.MinValue && item.TimeIn.TimeOfDay > new TimeSpan(9, 30, 0);
          item.isLateArrival = lateArrival;
          bool earlyDeparture = item.TimeOut != DateTime.MinValue && item.TimeOut.TimeOfDay < new TimeSpan(16, 45, 0);
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
              existingRecord.TotalWorkHours = item.WorkingHours.TotalSeconds > 0 ? (long)item.WorkingHours.TotalSeconds : 0;
              existingRecord.BreakHours = item.TotalTime.TotalSeconds > item.WorkingHours.TotalSeconds ?
                (long)(item.TotalTime.TotalSeconds - item.WorkingHours.TotalSeconds) : 0;
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
          //
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

        //con.Close();
        //overallStopwatch.Stop(); // Stop the overall timer
        //Console.WriteLine($"Total execution time for ConnectToDBandFillAttendanceData: {overallStopwatch.ElapsedMilliseconds} ms");
        return LstTimeData;
      }
      catch (Exception ex)
      {
        Console.WriteLine("An error occurred while saving changes: " + ex.Message);
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner exception: " + ex.InnerException.Message);
        }
        throw; // important
      }
    }


    public Task<List<TimeData>> GetEmployeeAttendacne(DateTime rstartDate, DateTime rendDate, AspNetUser aspNetUser)
    {

      string formattedStartDate = rstartDate.ToString("dd-MM-yyyy");
      string formattedEndDate = rendDate.ToString("dd-MM-yyyy");
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
      // Parse the formatted start and end dates
      DateTime startDate = DateTime.ParseExact(formattedStartDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      // DateTime endDate = DateTime.ParseExact(formattedEndDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)
                              .AddDays(1).AddSeconds(-1);

      int totalDays = (endDate - startDate).Days + 1;
      con.Open();
      //  foreach (int Id in UserIds)
      // {
      int UserId = aspNetUser.BioStarEmpNum.Value; //Assigns the current UserId for processing.

      cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE USER_ID = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt", con);
      cmd.Parameters.AddWithValue("@UserId", UserId);
      cmd.Parameters.AddWithValue("@StartDate", startDate);
      cmd.Parameters.AddWithValue("@EndDate", endDate);

      dr = cmd.ExecuteReader();//SqlCommand and SqlDataReader (cmd, dr) are initialized.

      DataTable dt = new DataTable();//A new DataTable dt is created for storing data related to the current user.

      string UserName = string.Empty;
      string timeZone = string.Empty;
      string userGuidId = string.Empty;

      // AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);

      //processing
      UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
      string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
      string userLeavePolicyDescription = string.Empty;
      userGuidId = aspNetUser.Id;
      if (aspNetUser.CountryName != null)
      {


        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          timeZone = aspNetUser.CountryName.TimeZone;
          countryName = aspNetUser.CntryName;
        }
        //Processes user data to get UserName, depName, timeZone, countryName, and leave policy data.
        //Checks for relocation and adjusts timeZone and countryName accordingly.

        //Creating and Filling the DataTable:
        DataColumn dc = new DataColumn("USER_ID", typeof(String));
        dt.Columns.Add(dc);

        dc = new DataColumn("devdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("bsevtdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("DEVID", typeof(Int32));
        dt.Columns.Add(dc);

        dc = new DataColumn("DETAILS", typeof(string));
        dt.Columns.Add(dc);

        dc = new DataColumn("devnm", typeof(string));
        dt.Columns.Add(dc);
        //Columns are added to dt to represent user attendance data.
        //A loop reads data from dr and populates dt with rows representing the attendance logs.
        //↓

        while (dr.Read())//This loop iterates over each row returned by the SQL query executed above.
        {
          // for each row from the database, add the retrieved table name to the list
          DataRow dtrw = dt.NewRow(); //A new DataRow (dtrw) is created to store data for each attendance log.
          dtrw[0] = dr["USER_ID"];
          dtrw[1] = (DateTime)dr["devdt"];
          dtrw[2] = dr["bsevtdt"];
          dtrw[3] = dr["DEVID"];

          //The next lines check if the 'DEVID' is in the list LstCardReadersIn.
          //Depending on the result, "IN" or "OUT" is stored in the fifth column of dtrw.
          if (LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"])))
          {
            dtrw[4] = "IN";
          }
          else
          {
            dtrw[4] = "OUT";
          }

          //dtrw[5] = dr["devnm"];: The 'devnm' (device name) column from dr is assigned to the sixth column of dtrw
          dtrw[5] = dr["devnm"];
          dt.Rows.Add(dtrw);
          //this will add the row at the end of the datatable
          //This step is repeated for each row returned by the SQL query, building up the attendance data for the user.
        }


        //After Reading All Data
        dr.Close();
        DataView view = dt.DefaultView;
        Console.WriteLine($"UserId: {dt.Rows}");
        Console.WriteLine($"dt.Rows.Count: {dt.Rows.Count}");
        view.Sort = "devdt ASC";
        //The DataTable dt's default view is sorted by 'devdt' in ascending order: view.Sort = "devdt ASC";.


        //Processing Sorted Attendance Data
        dt = view.ToTable();

        int rowsCount = dt.Rows.Count;

        if (rowsCount > 0)
        {


          /*

          Initialization of Variables for Attendance Calculation:
          Variables like firstDateTime, lastDateTime, firstDay, lastDay, and counters are initialized. 
          These will be used to track the dates, times, and other details for each punch.

          */
          var groupedData = dt.AsEnumerable()
      .GroupBy(r => Convert.ToDateTime(r["devdt"]).Date)
      .OrderBy(g => g.Key);

          DateTime firstDateTime = (DateTime)dt.Rows[0]["devdt"];//ConvertToCountryTimeZone(dt, 0, timeZone);//(DateTime)dt.Rows[0]["SRVDT"];
          DateTime lastDateTime = (DateTime)dt.Rows[rowsCount - 1]["devdt"];//ConvertToCountryTimeZone(dt, rowsCount - 1, timeZone);//(DateTime)dt.Rows[rowsCount - 1]["SRVDT"];
          int firstDay = firstDateTime.Day;
          int lastDay = lastDateTime.Day;
          List<int> LstEmptyDays = new List<int>();
          //---
          TimeData attendance;
          DateTime timeIn;
          DateTime timeOut;
          DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
          DateTime lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
          DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
          bool IsCardIn = false;
          bool IsCardOut = false;
          bool lastsemiIn = false;
          TimeSpan ThidDayWorkingHours = new TimeSpan();
          int k = 0;//-1;

          foreach (var dayGroup in groupedData)
          {
            var dayRows = dayGroup.OrderBy(r => Convert.ToDateTime(r["devdt"])).ToList();

            // Reset per day
            firsTimeIn = blankDateTime;
            lastTimeOut = blankDateTime;
            ThidDayWorkingHours = TimeSpan.Zero;
            IsCardIn = false;
            IsCardOut = false;

            for (int j = 0; j < dayRows.Count; j++)
            {
              string shortCountryName = dayRows[j]["devnm"].ToString().Substring(0, 3);

              /* SAME COUNTRY LOGIC (UNCHANGED) */
              if (dayRows[j]["devnm"].ToString().Substring(0, 4) == "NG-L")
              {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Lagos";
              }
              else if (dayRows[j]["devnm"].ToString().Substring(0, 4) == "NG-P")
              {
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria Port Harcourt";
              }
              else if (dayRows[j]["devnm"].ToString().Substring(0, 4) == "IN01")
              {
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
              }
              else
              {
                switch (shortCountryName)
                {
                  case "PK ":
                  case "PAK":
                    timeZone = "Pakistan Standard Time";
                    countryName = "Pakistan";
                    break;
                  case "UAE":
                    timeZone = "Arab Standard Time";
                    countryName = "United Arab Emirates";
                    break;
                  default:
                    timeZone = aspNetUser.CountryName.TimeZone;
                    countryName = aspNetUser.CountryName.Name;
                    break;
                }
              }

              DateTime currentTime = ConvertToCountryTimeZoneNew((DateTime)dayRows[j]["devdt"], timeZone);

              k = j + 1;

              /* 🔥 SAME LOGIC, BUT SAFE */
              if ((k < dayRows.Count) &&
                  LstCardReadersIn.Contains((int)dayRows[j]["DEVID"]) &&
                  !LstCardReadersIn.Contains((int)dayRows[k]["DEVID"]))
              {
                DateTime nextTime = ConvertToCountryTimeZoneNew((DateTime)dayRows[k]["devdt"], timeZone);

                if (!IsCardIn)
                {
                  firsTimeIn = currentTime;
                  lastTimeOut = nextTime;
                  IsCardIn = true;
                }

                timeIn = currentTime;
                timeOut = nextTime;

                TimeSpan workingHour = (timeOut - timeIn);
                ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);

                IsCardOut = true;
                lastTimeOut = nextTime;
              }

              previousCountryName = countryName;
            }

            /* ✅ ADD DAILY RECORD (NO NEED separate last-loop fix anymore) */
            if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);

              var leaveName = "";
              int leaveTypeID = 0;
              var leaveForDay = dbLeaveOn.Leaves
                  .Where(x => x.UserId == aspNetUser.Id &&
                  DbFunctions.TruncateTime(x.StartDate) == DbFunctions.TruncateTime(firsTimeIn))
                  .FirstOrDefault();

              if (leaveForDay != null)
              {
                var leaveType = dbLeaveOn.LeaveTypes.Find(leaveForDay.LeaveTypeId);
                leaveTypeID = leaveType.Id;
                leaveName = leaveType.Name;
              }
              bool isAbsent = false;
              attendance = new TimeData()
              {





                Date = firsTimeIn.Date,
                Day = firsTimeIn.DayOfWeek.ToString(),




                EmployeeName = UserName,
                EmployeeNumber = UserId,
                TimeZone = countryName,
                Department = aspNetUser.DepartmentName,
                Policy = userLeavePolicyDescription,
                TimeIn = firsTimeIn,
                TimeOut = lastTimeOut,
                WorkingHours = ThidDayWorkingHours,
                TotalTime = (lastTimeOut - firsTimeIn),

                UserID = aspNetUser.Id,
                ManagerEmail = aspNetUser.ManagerEmail,
                Manager2Email = aspNetUser.Manager2Email,
                ManagerID = aspNetUser.ManagerID,
                Manager2ID = aspNetUser.Manager2ID,
                Status = leaveName,
                isAbsent = isAbsent,
                leaveTypeID = leaveTypeID,
                leaveType = leaveName
              };

              LstTimeData.Add(attendance);
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
            }
          }

          ////Leaves Processing
          List<TimeData> offDays = new List<TimeData>();
          List<string> LstThisMonthsWeekEnds;
          if (aspNetUser.UserLeavePolicy == null || string.IsNullOrEmpty(aspNetUser.UserLeavePolicy.WeeklyOffDays))
          {
            LstThisMonthsWeekEnds = GetUserDataWeekEndLists(startDate, endDate, "6,0");
          }
          else
          {
            LstThisMonthsWeekEnds = GetUserDataWeekEndLists(startDate, endDate, aspNetUser.UserLeavePolicy.WeeklyOffDays);
          }

          int iEmpNum = aspNetUser.BioStarEmpNum.Value;

          DateTime latestTimeData = LstTimeData
                  .Where(x => x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday) // Exclude weekends
                  .OrderByDescending(x => x.Date)
                  .Select(x => x.Date.Date)
                  .FirstOrDefault();
          foreach (string weekEndDay in LstThisMonthsWeekEnds)
          {
            // Create a DateTime object for the weekend day

            DateTime weekEndDate = DateTime.ParseExact(weekEndDay, "MM-dd-yyyy", CultureInfo.InvariantCulture);
            if (weekEndDate <= latestTimeData)
            {
              TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Date == weekEndDate.Date);

              if (thisWeekEnd != null)
              {
                thisWeekEnd.Status = "Weekend";
              }
              else
              {
                TimeData weekEndOffDate = new TimeData
                {





                  Date = weekEndDate,
                  Day = weekEndDate.ToString("dddd"),
                  Status = "Weekend",





                  EmployeeName = UserName,
                  EmployeeNumber = UserId,
                  TimeZone = countryName,
                  Department = aspNetUser.DepartmentName,
                  Policy = userLeavePolicyDescription,
                  TimeIn = firsTimeIn,
                  TimeOut = lastTimeOut,
                  WorkingHours = ThidDayWorkingHours,
                  TotalTime = (lastTimeOut - firsTimeIn),

                  UserID = aspNetUser.Id,
                  ManagerEmail = aspNetUser.ManagerEmail,
                  Manager2Email = aspNetUser.Manager2Email,
                  ManagerID = aspNetUser.ManagerID,
                  Manager2ID = aspNetUser.Manager2ID,

                  isAbsent = false,
                  leaveTypeID = 0,
                  leaveType = String.Empty
                };
                offDays.Add(weekEndOffDate);
              }
            }
          }
          LstTimeData.AddRange(offDays);

          // Find the latest date for which timing data exists in the database (max date in LstTimeData)
          var lastExistingDate = LstTimeData
                 .Where(x => x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday) // Exclude weekends
                 .OrderByDescending(x => x.Date)
                 .Select(x => x.Date.Date)
                 .FirstOrDefault();

          // If no valid data is present, default to startDate
          if (lastExistingDate == default(DateTime))
          {
            lastExistingDate = startDate;
          }


          for (int day = 0; day < totalDays; day++)
          {
            DateTime currentDay = startDate.AddDays(day);

            if (currentDay > lastExistingDate)
            {
              // Stop processing dates after the last available data
              break;
            }
            var timeDataForDay = LstTimeData.FirstOrDefault(x => x.Date.Date == currentDay.Date);
            var annualOffDay = dbLeaveOn.AnnualOffDays
                .FirstOrDefault(x => DbFunctions.TruncateTime(x.OffDay) == currentDay.Date
                               && x.UserLeavePolicyId == aspNetUser.UserLeavePolicyId);
            // Check for any leave that spans the current day
            var leave = dbLeaveOn.Leaves
                  .FirstOrDefault(x => DbFunctions.TruncateTime(x.StartDate) <= currentDay.Date
                               && DbFunctions.TruncateTime(x.EndDate) >= currentDay.Date
                               && x.IsAccepted1 != null && x.IsAccepted2 != null
                               && x.UserId == userGuidId);
            //if (timeDataForDay == null) // Employee was absent
            // Handle cases where no time data exists for the day
            if (timeDataForDay == null)
            {
              string status = "Absent"; // Default to "Absent"
              bool isAbsent = false;                 // Check for holiday and leave
              if (annualOffDay != null)
              {
                status = annualOffDay.Description; // Holiday description
              }
              else if (leave != null)
              {
                // If a leave record exists, retrieve the corresponding leave type name
                var leaveTypeName = dbLeaveOn.LeaveTypes
                    .Where(l => l.Id == leave.LeaveTypeId)
                    .Select(l => l.Name)
                    .FirstOrDefault();
                status = leaveTypeName; // Leave type ID if on leave
              }
              isAbsent = true;
              //string status = annualOffDay != null ? annualOffDay.Description : "Absent"; // Use holiday description if it's a holiday, else mark as Absent
              depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId)?.DepartmentName ?? depName; // Safeguard against null
                                                                                                                         // Add attendance data for the day
              attendance = new TimeData()
              {
                EmployeeName = UserName,
                EmployeeNumber = UserId,
                TimeZone = countryName,
                Policy = userLeavePolicyDescription,
                Department = depName,
                Date = annualOffDay?.OffDay ?? currentDay,
                Day = (annualOffDay?.OffDay ?? currentDay).DayOfWeek.ToString(),
                Status = status,










                TimeIn = firsTimeIn,
                TimeOut = lastTimeOut,
                WorkingHours = ThidDayWorkingHours,
                TotalTime = (lastTimeOut - firsTimeIn),

                UserID = aspNetUser.Id,
                ManagerEmail = aspNetUser.ManagerEmail,
                Manager2Email = aspNetUser.Manager2Email,
                ManagerID = aspNetUser.ManagerID,
                Manager2ID = aspNetUser.Manager2ID,

                isAbsent = isAbsent,
                leaveTypeID = 0,
                leaveType = String.Empty
              };
              LstTimeData.Add(attendance);
            }
            else if (annualOffDay != null) // Employee worked on an annual holiday
            {
              timeDataForDay.Status = annualOffDay.Description; // Append 'Worked' to the holiday description
            }
          }
        }
      }


      //to avaid showing current month all data which is not happend yet
      foreach (var itm in LstTimeData.ToList())
      {
        if (itm.Date > DateTime.Now.Date)
        {
          LstTimeData.Remove(itm);
        }
      }

      // ViewBag.TotalHours = TotalTime.TotalHours.ToString("N2");
      // ViewBag.TotalWorkingHours = TotalWorkingHours.TotalHours.ToString("N2");
      con.Close();
      return Task.FromResult(LstTimeData);
    }

    public List<BreakHour> GetBreakHoursForUser(
        AspNetUser aspNetUser,
        DateTime startDate,
        DateTime endDate,
        SqlConnection con)
    {
      List<BreakHour> lstBreakHours = new List<BreakHour>();

      if (!aspNetUser.BioStarEmpNum.HasValue)
        return lstBreakHours;

      int userId = aspNetUser.BioStarEmpNum.Value;

      using (SqlCommand cmd = new SqlCommand(@"
        SELECT user_id, devdt, bsevtdt, DEVID, devnm 
        FROM punchlog 
        WHERE user_id = @UserId 
        AND devdt BETWEEN @StartDate AND @EndDate 
        ORDER BY devdt", con))
      {
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        using (SqlDataReader dr = cmd.ExecuteReader())
        {
          List<PunchLog> punchLogs = new List<PunchLog>();

          while (dr.Read())
          {
            punchLogs.Add(new PunchLog
            {
              UserId = Convert.ToInt32(dr["user_id"]),
              DeviceId = Convert.ToInt32(dr["DEVID"]),
              DeviceName = dr["devnm"].ToString(),
              DeviceDate = Convert.ToDateTime(dr["devdt"]),
              BreakStart = dr["bsevtdt"] != DBNull.Value
                    ? (DateTime?)dr["bsevtdt"]
                    : null
            });
          }

          // timezone logic
          string timeZone = aspNetUser.CountryName?.TimeZone ?? string.Empty;

          if (aspNetUser.IsRelocated)
          {
            timeZone = dbLeaveOn.CountryNames
                .FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp)?.TimeZone;
          }

          // your existing processing method
          ProcessBreakHours(punchLogs, aspNetUser, timeZone, lstBreakHours);
        }
      }

      return lstBreakHours;
    }


    public async Task ConnectToDBandFillBreakHours(List<BreakHour> lstBreakHours)
    {


      try
      {
        SaveBreakHours(lstBreakHours);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Exception => " + ex.Message);
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }
      }


    }
    protected List<string> GetUserDataWeekEndLists(DateTime startDate, DateTime endDate, string WeekEndDays)
    {
      List<int> LstWeekEndDays = WeekEndDays.Split(',').Select(int.Parse).ToList();
      List<string> LstThisMonthsWeekEnds = new List<string>();

      CultureInfo ci = new CultureInfo("en-US");

      // Loop through each day in the given date range
      for (DateTime date = startDate; date < endDate; date = date.AddDays(1))
      {
        // Check if the current day's DayOfWeek matches any of the provided weekend days
        if (LstWeekEndDays.Contains((int)date.DayOfWeek))
        {
          LstThisMonthsWeekEnds.Add(date.ToString("MM-dd-yyyy"));
        }
      }

      LstThisMonthsWeekEnds.Sort();
      return LstThisMonthsWeekEnds;
    }
    private DateTime ConvertToCountryTimeZoneNew(DateTime dateTime, string timeZone)
    {
      try
      {
        TimeZoneInfo customeTimeZone;
        DateTime ConvertedDateTime;
        customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, customeTimeZone);
        return ConvertedDateTime;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Timezone exception" + ex.Message);
        throw;
      }
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
