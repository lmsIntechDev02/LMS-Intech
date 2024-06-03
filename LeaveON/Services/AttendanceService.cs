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
namespace LeaveON.Services
{
  public class AttendanceService
  {
    List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042 };
    private BioStarEntities dbBioStar = new BioStarEntities();
    LeaveONEntities dbLeaveOn = new LeaveONEntities();

    public Task ConnectToDBandFillAttendanceData(DateTime startDate, DateTime endDate)
    {
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
      List<AspNetUser> users = dbLeaveOn.AspNetUsers.ToList<AspNetUser>();
      List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
      con.Open();
      foreach (int Id in userIds)
      {
          //"' and devdt BETWEEN '" + "2024-01-01" + "' AND '" + "2024-04-04" + "' order by devdt"
          int UserId = Id;//Assigns the current UserId for processing.//startDate.ToString("yyyy-MM-dd")
          cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id =" + UserId + "and devdt BETWEEN '" + startDate.ToString("yyyy-MM-dd") + "' AND '" + endDate.ToString("yyyy-MM-dd") + "' order by devdt", con);
          dr = cmd.ExecuteReader();

          DataTable dt = new DataTable();//A new DataTable dt is created for storing data related to the current user.

          string UserName = string.Empty;
          string timeZone = string.Empty;
          string userGuidId = string.Empty;
          AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);
          //Fetches user-related data from dbLeaveOn.AspNetUsers based on UserId.

          //processing
          UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
          userGuidId = aspNetUser.Id;
          string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          string userLeavePolicyDescription = string.Empty;
          if (aspNetUser.CountryName == null)
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
          view.Sort = "devdt ASC";
          //The DataTable dt's default view is sorted by 'devdt' in ascending order: view.Sort = "devdt ASC";.


          //Processing Sorted Attendance Data
          dt = view.ToTable();

          int rowsCount = dt.Rows.Count;
          if (rowsCount <= 0) continue;

          /*

          Initialization of Variables for Attendance Calculation:
          Variables like firstDateTime, lastDateTime, firstDay, lastDay, and counters are initialized. 
          These will be used to track the dates, times, and other details for each punch.

          */
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
          DateTime leaveDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
          bool IsCardIn = false;
          bool IsCardOut = false;
          bool lastsemiIn = false;
          TimeSpan ThidDayWorkingHours = new TimeSpan();
          int k = 0;//-1;

          for (int j = 0; j <= rowsCount - 1; j++)//this loop iterates over each row of the sorted DataTable to process attendance data
          {
            string shortCountryName = dt.Rows[j]["devnm"].ToString().Substring(0, 3);

            if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-L")
            {
              timeZone = "W. Central Africa Standard Time";
              countryName = "Nigeria Lagos";
            }
            else if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-P")
            {
              timeZone = "W. Central Africa Standard Time";
              countryName = "Nigeria Port Harcourt";
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
            firstDateTime = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);


            // Check for a change in date indicating a new day of attendance records
            if (firstDateTime.Day != firstDay)
            {//its mean new date started. so add all previois date calcuation here and add to list

              // If there are valid time-in and time-out records for the previous day, calculate total time
              if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
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
                attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryChanged ? previousCountryName : countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn), Status = leaveName, leaveType = leaveName, leaveTypeID = leaveType };

                LstTimeData.Add(attendance);
                TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);

              }
              //re-intiallize variables to next date calculations
              firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
              lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
              ThidDayWorkingHours = new TimeSpan();
              IsCardIn = false; IsCardOut = false;
              firstDay = firstDateTime.Day;
            }

            k = j + 1;

            //get actual working hour of this date

            if ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[j]["DEVID"]) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]) &&
              Convert.ToDateTime(dt.Rows[j]["devdt"]).Day == firstDay && Convert.ToDateTime(dt.Rows[k]["devdt"]).Day == firstDay)
            {
              if (IsCardIn == false)
              {
                firsTimeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
                lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
                IsCardIn = true;
              }

              timeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
              timeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
              TimeSpan workingHour = (timeOut - timeIn);
              ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);
              if (IsCardIn == true && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]))
              {
                IsCardOut = true;
                lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
              }
            }

            previousCountryName = countryName;
          }
          //datetime for end here

          //add last date to list here as loop ended
          if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
          {
            TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
            depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
            attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

            LstTimeData.Add(attendance);
            TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
          }

          ////Leaves Processing
          List<TimeData> offDays = new List<TimeData>();
          // Modify the existing foreach loop to iterate over the generated list of weekend dates:
          List<DateTime> lstThisMonthsWeekEnds = GetWeekEndList(startDate, endDate, aspNetUser.UserLeavePolicy?.WeeklyOffDays ?? "6,0");
          foreach (DateTime weekEndDate in lstThisMonthsWeekEnds)
          {
            TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Date == weekEndDate.Date && x.EmployeeNumber == Id);
            if (thisWeekEnd != null)
            {
              thisWeekEnd.Status = "Weekend";
            }
            else
            {
              TimeData weekEndOffDate = new TimeData
              {
                EmployeeName = UserName,
                EmployeeNumber = Id,
                TimeZone = countryName,
                Department = depName,
                Policy = userLeavePolicyDescription,
                Date = weekEndDate,
                Day = weekEndDate.ToString("dddd"),
                Status = "Weekend"
              };
              offDays.Add(weekEndOffDate);
            }
          }
          LstTimeData.AddRange(offDays);
          // Restrict the absent day checks to the date range:
          // Iterate over each day within the given date range
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
                status = annualOffDay.Description; // Use the holiday description
                isAbsent = false; // Do not mark as absent since it's a holiday
              }

              // Retrieve department name safely
              depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId)?.DepartmentName ?? depName;

              // Create and add the attendance data
              attendance = new TimeData()
              {
                EmployeeName = UserName,
                EmployeeNumber = UserId,
                TimeZone = countryName,
                Policy = userLeavePolicyDescription,
                Department = depName,
                Date = date,
                Day = date.DayOfWeek.ToString(),
                Status = status,
                isAbsent = isAbsent
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

              // Adding leave data to the list
              attendance = new TimeData()
              {
                EmployeeName = UserName,
                EmployeeNumber = UserId,
                TimeZone = countryName,
                Policy = userLeavePolicyDescription,
                Department = depName,
                Date = date,
                Day = date.DayOfWeek.ToString(),
                Status = "Leave",
                isAbsent = false,
                leaveTypeID = leaveRecord.LeaveTypeId,
                leaveType = leaveTypeName
              };
              LstTimeData.Add(attendance);
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
      foreach (var item in LstTimeData.ToList())
      {
        // Check if the user arrives after 9:30 AM
        bool lateArrival = item.TimeIn.TimeOfDay > new TimeSpan(9, 30, 0);
        item.isLateArrival = lateArrival;  // Directly assign the boolean value

        // Check if the user leaves before 4:45 PM
        bool earlyDeparture = item.TimeOut.TimeOfDay < new TimeSpan(16, 45, 0);
        item.isEarlyDeparture = earlyDeparture;  // Directly assign the boolean value

        AttendanceData attendanceDataToFill = new AttendanceData
        {
          EmployeeID = item.EmployeeNumber,
          UserName = item.EmployeeName,
          DepartmentName = item.Department,
          UserLeavePolicyID = item.Policy,
          CreatedDate = item.Date,
          FirstPunchIn = item.TimeIn,
          LastPunchOut = item.TimeOut,
          TotalWorkHours = (long)item.WorkingHours.TotalSeconds,
          BreakHours = (long)(item.TotalTime.TotalSeconds - item.WorkingHours.TotalSeconds),
          IsLateArrival = item.isLateArrival, // Use boolean directly
          IsEarlyDeparture = item.isEarlyDeparture, // Use boolean directly
          IsAbsent = item.isAbsent, // Assuming isAbsent is also being assigned elsewhere as a string
          IsLeave = item.leaveTypeID != 0 ? true : false,
          LeaveTypeID = item.leaveTypeID,
          LeaveType = item.leaveType,
          CountryName = item.TimeZone,
        };
        dbLeaveOn.AttendanceDatas.Add(attendanceDataToFill);
      }

      try
      {
        dbLeaveOn.SaveChanges();
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
      return null;
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
  }
}
