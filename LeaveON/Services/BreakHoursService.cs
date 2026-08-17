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
  public class BreakHoursService
  {
    List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042,
                                                 543734917, 538205733, 538205730};
    private BioStarEntities dbBioStar = new BioStarEntities();
    LeaveONEntities dbLeaveOn = new LeaveONEntities();
   // LeaveONEntitiesTarget dbLeaveOnTarget = new LeaveONEntitiesTarget();


    public   List<BreakHour> GetBreakHoursForUser(
    AspNetUser aspNetUser,
    DateTime startDate,
    DateTime endDate,
    SqlConnection con)
    {
      if (con.State == ConnectionState.Closed)
      {
        con.Open();
      }
      List<BreakHour> lstBreakHours = new List<BreakHour>();

      if (!aspNetUser.BioStarEmpNum.HasValue)
        return lstBreakHours;

      int userId = aspNetUser.BioStarEmpNum.Value;

      using (SqlCommand cmd = new SqlCommand(@"
        SELECT user_id, devdt, bsevtdt, DEVID, devnm 
        FROM punchlog 
        WHERE user_id = @UserId 
        AND  convert(date,devdt) >= @StartDate 
        AND convert(date,devdt) <=   @EndDate 
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
    public async Task ConnectToDBandFillBreakHours( List<BreakHour> lstBreakHours)
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

    private void ProcessBreakHours(List<PunchLog> punchLogs, AspNetUser aspNetUser, string timeZone, List<BreakHour> LstBreakHours)
    {
      for (int i = 0; i < punchLogs.Count - 1; i++)
      {
        var currentLog = punchLogs[i];
        var nextLog = punchLogs[i + 1];

        if (!LstCardReadersIn.Contains(currentLog.DeviceId) && LstCardReadersIn.Contains(nextLog.DeviceId))
        {

          // Get Attendance timezone and Country
         //var atttimeZone = GetAttendacneTimeZone(deviceCode, shortCountryName, aspNetUser);
          

          //  countryName = atttimeZone.CountryName;
          DateTime timeOutt = ConvertToCountryTimeZoneNew(currentLog.DeviceDate, timeZone);
          DateTime timeInn = ConvertToCountryTimeZoneNew(nextLog.DeviceDate, timeZone);

          if (timeInn > timeOutt)
          {
            TimeSpan breakDuration = timeInn - timeOutt;

            // Check for duplicates in LstBreakHours
            bool exists = LstBreakHours.Any(b =>
                b.UserId == aspNetUser.Id &&
                b.Date == timeOutt.Date &&
                b.PunchIn == timeOutt &&
                b.PunchOut == timeInn);

            if (!exists)
            {

              if (aspNetUser.BioStarEmpNum == null)
              {
                Console.WriteLine($"BioStarEmpNum is null for user with Id {aspNetUser.Id}. Skipping entry.");
                continue;
              }
              BreakHour breakEntry = new BreakHour
              {
                UserId = aspNetUser.Id,
                BioStarEmpNum = aspNetUser.BioStarEmpNum.Value,
                Date = timeOutt.Date,
                PunchIn = timeOutt,
                PunchOut = timeInn
              };
              Console.WriteLine($"Processing User: {breakEntry.UserId},{breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}");
              LstBreakHours.Add(breakEntry);
            }
          }
        }
      }
    }
    private DateTime ConvertToCountryTimeZoneNew(DateTime dateTime, string timeZone)
    {
      try
      {
        var customTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, customTimeZone);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error converting time: {ex.Message}");
        return dateTime;
      }
    }
    private void SaveBreakHours(List<BreakHour> LstBreakHours)
    {
      foreach (var breakEntry in LstBreakHours)
      {
        if (string.IsNullOrEmpty(breakEntry.UserId))
        {
          Console.WriteLine($"Skipping user with bio Id: {breakEntry.BioStarEmpNum}");
          continue;
        }
        bool dbExists = dbLeaveOn.BreakHours.Any(b =>
            b.UserId == breakEntry.UserId &&
            b.Date == breakEntry.Date &&
            b.PunchIn == breakEntry.PunchIn &&
            b.PunchOut == breakEntry.PunchOut);

        // Find existing record matching UserId & Date
        var existingRecord = dbLeaveOn.BreakHours
            .FirstOrDefault(b =>
                b.UserId == breakEntry.UserId &&
                DbFunctions.TruncateTime(b.Date) == breakEntry.Date);

        //if (!dbExists)
        //{

        //  Console.WriteLine($"BreakHours for user: {breakEntry.UserId}, {breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}, PunchIn: {breakEntry.PunchIn}, PunchOut: {breakEntry.PunchOut}");
        //  dbLeaveOn.BreakHours.Add(breakEntry);
        //}

        if (existingRecord != null)
        {
          // Update existing record with new punch times
          existingRecord.PunchIn = breakEntry.PunchIn;
          existingRecord.PunchOut = breakEntry.PunchOut;
          Console.WriteLine($"Updated BreakHours for user: {breakEntry.UserId}, Date: {breakEntry.Date}");
        }
        else
        {
          // Add new record
          Console.WriteLine($"BreakHours for user: {breakEntry.UserId}, {breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}, PunchIn: {breakEntry.PunchIn}, PunchOut: {breakEntry.PunchOut}");
          dbLeaveOn.BreakHours.Add(breakEntry);
        }
      }
      try
      {
        dbLeaveOn.SaveChanges();
      }
      catch (SqlException ex)
      {
        Console.WriteLine($"SQL Error: {ex.Message}");
      }
    }

    public class PunchLog
    {
      public int UserId { get; set; }
      public int DeviceId { get; set; }
      public string DeviceName { get; set; }
      public DateTime DeviceDate { get; set; }
      public DateTime? BreakStart { get; set; }
    }
  }

}
