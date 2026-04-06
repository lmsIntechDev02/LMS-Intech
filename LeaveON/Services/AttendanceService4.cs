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



    public async Task<List<AspNetUser>> GetUserActiveList()
    {
     return dbLeaveOn.AspNetUsers.Where(x => x.IsActive == true).ToList();
    }

    //public Task ConnectToDBandReturnAttendanceData(DateTime startDate, DateTime endDate)
    public async Task<List<TimeData>> ConnectToDBandReturnAttendanceData(  List<TimeData> LstTimeData)

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

    public int GetUserAttendancData(
    DateTime startDate,
    DateTime endDate,
    SqlConnection con,                      // pass connection from outside
    AspNetUser aspNetUser,                  // single user
    out List<TimeData> LstTimeData
)
    {
      LstTimeData = new List<TimeData>();

     

      int userId = aspNetUser.BioStarEmpNum.Value;

      Console.WriteLine($"Processing: {aspNetUser.UserName} ({userId})");

      //SqlCommand cmd = new SqlCommand(@"
      //  SELECT user_id, devdt, bsevtdt, DEVID, devnm 
      //  FROM punchlog 
      //  WHERE user_id = @UserId 
      //  AND CONVERT(date, devdt) >= @StartDate
      //  AND CONVERT(date, devdt) <= @EndDate
      //  ORDER BY devdt", con);

      //cmd.Parameters.AddWithValue("@UserId", userId);
      //cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
      //cmd.Parameters.AddWithValue("@EndDate", endDate.Date);

      SqlCommand cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt", con);

      // cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id =" + UserId + " and  convert(date, devdt)= '" + startDate.ToString("yyyy-MM-dd") + "' order by devdt", con);

      cmd.Parameters.AddWithValue("@UserId", userId);
      cmd.Parameters.AddWithValue("@StartDate", startDate);
      cmd.Parameters.AddWithValue("@EndDate", endDate);
      

      List<AttendanceRecord> records = new List<AttendanceRecord>();

      using (SqlDataReader dr = cmd.ExecuteReader())
      {
        while (dr.Read())
        {
          records.Add(new AttendanceRecord
          {
            UserId = dr["user_id"].ToString(),
            DevDt = dr["devdt"] != DBNull.Value ? Convert.ToDateTime(dr["devdt"]) : DateTime.MinValue,
            Bsevtdt = dr["bsevtdt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["bsevtdt"]) : null,
            DevId = dr["DEVID"] != DBNull.Value ? Convert.ToInt32(dr["DEVID"]) : 0,
            DevNm = dr["devnm"].ToString(),
            Details = LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"] ?? 0)) ? "IN" : "OUT"
          });
        }
      }

      if (records.Count == 0)
        return 0;

      string userName = aspNetUser.UserName.Contains("@")
          ? aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ")
          : aspNetUser.UserName;

      string timeZone = aspNetUser.CountryName?.TimeZone ?? "Pakistan Standard Time";
      string countryName = aspNetUser.CntryName ?? "Pakistan";
      string policy = aspNetUser.UserLeavePolicy?.Description;

      DateTime currentDay = records.First().DevDt.Date;

      DateTime firstIn = DateTime.MinValue;
      DateTime lastOut = DateTime.MinValue;

      bool isIn = false;
      bool isOut = false;

      TimeSpan dailyWorking = TimeSpan.Zero;

      for (int i = 0; i < records.Count - 1; i++)
      {
        var current = records[i];
        var next = records[i + 1];

        DateTime currentTime = ConvertToCountryTimeZoneNew(current.DevDt, timeZone);
        DateTime nextTime = ConvertToCountryTimeZoneNew(next.DevDt, timeZone);

        // new day detected
        if (currentTime.Date != currentDay)
        {
          if (isIn && isOut)
          {
            AddTimeData(
                LstTimeData,
                userName,
                userId,
                timeZone,
                countryName,
                policy,
                firstIn,
                lastOut,
                dailyWorking,
                aspNetUser
            );
          }

          // reset for next day
          currentDay = currentTime.Date;
          firstIn = DateTime.MinValue;
          lastOut = DateTime.MinValue;
          dailyWorking = TimeSpan.Zero;
          isIn = false;
          isOut = false;
        }

        // IN → OUT pair
        if (LstCardReadersIn.Contains(current.DevId) &&
            !LstCardReadersIn.Contains(next.DevId) &&
            currentTime.Date == nextTime.Date)
        {
          if (!isIn)
          {
            firstIn = currentTime;
            isIn = true;
          }

          lastOut = nextTime;
          isOut = true;

          dailyWorking += (nextTime - currentTime);
        }
      }

      // last day push
      if (isIn && isOut)
      {
        AddTimeData(
            LstTimeData,
            userName,
            userId,
            timeZone,
            countryName,
            policy,
            firstIn,
            lastOut,
            dailyWorking,
            aspNetUser
        );
      }

      // ✅ Weekend handling
      var weekEnds = GetWeekEndList(startDate, endDate, aspNetUser.UserLeavePolicy?.WeeklyOffDays ?? "6,0");

      foreach (var date in weekEnds)
      {
        if (!LstTimeData.Any(x => x.Date.Date == date.Date))
        {
          LstTimeData.Add(new TimeData
          {
            EmployeeName = userName,
            EmployeeNumber = userId,
            Date = date,
            Status = "Weekend",
            TimeZone = timeZone,
            Policy= policy,
            Department= aspNetUser.DepartmentName,
            CountryName = countryName
          });
        }
      }

      // ✅ Absent / Holiday
      for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
      {
        var existing = LstTimeData.FirstOrDefault(x => x.Date.Date == date.Date);

        if (existing == null)
        {
          var holiday = dbLeaveOn.AnnualOffDays
              .FirstOrDefault(x => x.OffDay == date && x.UserLeavePolicyId == aspNetUser.UserLeavePolicyId);

          LstTimeData.Add(new TimeData
          {
            EmployeeName = userName,
            EmployeeNumber = userId,
            Date = date,
            Status = holiday != null ? holiday.Description : "Absent",
            isAbsent = holiday == null,
            
            TimeZone = timeZone,
            Policy = policy,
            Department = aspNetUser.DepartmentName,
            CountryName = countryName
          });
        }
      }

      return 1;
    }
    private void AddTimeData(
    List<TimeData> list,
    string userName,
    int userId,
    string timeZone,
    string countryName,
    string policy,
    DateTime timeIn,
    DateTime timeOut,
    TimeSpan workingHours,
    AspNetUser user
)
    {
      list.Add(new TimeData
      {
        EmployeeName = userName,
        EmployeeNumber = userId,
        TimeZone = timeZone,
        CountryName = countryName,
        Policy = policy,
        Date = timeIn.Date,
        Day = timeIn.DayOfWeek.ToString(),
        TimeIn = timeIn,
        TimeOut = timeOut,
        WorkingHours = workingHours,
        Department = user.DepartmentName,
        UserID = user.Id,
        ManagerEmail = user.ManagerEmail,
        Manager2Email = user.Manager2Email,
        ManagerID = user.ManagerID,
        Manager2ID = user.Manager2ID
      });
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
        Status = status,
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
