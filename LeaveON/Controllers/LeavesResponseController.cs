using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Repository.Models;
using Microsoft.AspNet.Identity;
using LeaveON.EmailSender;
using LMS.Constants;
using System.Data.SqlClient;
using LeaveON.Models.Procedures;
using System.Text;

namespace LeaveON.Controllers
{

  [Authorize(Roles = "Admin,Manager, User")]
  public class LeavesResponseController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: Leaves
    public async Task<ActionResult> Index()
    {
      //var leaves = db.Leaves.Include(l => l.LeaveType).Include(l => l.UserLeavePolicy);
      //var leaves = db.Leaves.Include(l => l.LeaveType);
      string LoggedInUserId = User.Identity.GetUserId();
      var leaves = db.Leaves.Where(x => x.IsQuotaRequest == false && (x.LineManager1Id == LoggedInUserId || x.LineManager2Id == LoggedInUserId));
      return View(await leaves.ToListAsync());
    }
    public async Task<ActionResult> QuotaResponseHistory()
    {
      //var leaves = db.Leaves.Include(l => l.LeaveType).Include(l => l.UserLeavePolicy);
      //var leaves = db.Leaves.Include(l => l.LeaveType);
      string LoggedInUserId = User.Identity.GetUserId();
      var leaves = db.Leaves.Where(x => x.IsQuotaRequest == true && (x.LineManager1Id == LoggedInUserId || x.LineManager2Id == LoggedInUserId));
      return View(await leaves.ToListAsync());
    }
    public List<AspNetUser> GetSeniorStaff()
    {
      List<AspNetUser> Seniors = new List<AspNetUser>();
      foreach (AspNetUser user in db.AspNetUsers.ToList<AspNetUser>())
      {
        foreach (AspNetRole role in user.AspNetRoles.ToList<AspNetRole>())
        {
          if (role.Name == "Admin" || role.Name == "Manager" || role.Name == "User")
          {
            AspNetUser userFound = Seniors.Find(x => x.Id == user.Id);
            if (userFound == null)
            {
              Seniors.Add(user);
            }
          }
        }
      }
      return Seniors;
    }

    // GET: Leaves/Edit/5
    public async Task<ActionResult> Edit(decimal id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      Leave leave = await db.Leaves.FindAsync(id);

      if (leave == null)
      {
        return HttpNotFound();
      }
      if (User.Identity.GetUserId() == leave.LineManager1Id || User.Identity.GetUserId() == leave.LineManager2Id)
      { 
        
      }
      else
      {
        return RedirectToAction("Index");
      }

      //ViewBag.LineManager1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id).UserName;
      //ViewBag.LineManager2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id).UserName;
      List<AspNetUser> Seniors = GetSeniorStaff();
     var user = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);
      bool IsLineManager1 = false;
      if (User.Identity.GetUserId() == leave.LineManager1Id)
      {
        IsLineManager1 = true;
      }
      else
      {
        IsLineManager1 = false;
      }
      ViewBag.IsLineManager1 = IsLineManager1;

      ViewBag.LineManagers = new SelectList(Seniors, "Id", "UserName");
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      ViewBag.JoiningDate = user != null ? (user.JoiningDate.HasValue ? String.Format(LeaveON.UtilityClasses.Constants.DateFormatDayMonthYear, user.JoiningDate.Value) : String.Empty) : String.Empty;

      ViewBag.ApplicantName = user.UserName;
      ViewBag.UserLeavePolicyId = leave.UserLeavePolicyID;
      ViewBag.LeaveUserId = leave.AspNetUser.Id;
      ViewBag.totalDays = Convert.ToInt32(leave.TotalDays);
      return View(leave);
    }

    // POST: Leaves/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,ResponseDate1,ResponseDate2,IsAccepted1,IsAccepted2,LineManager1Id,LineManager2Id,Remarks1,Remarks2,DateCreated,DateModified,UserLeavePolicyId")] Leave leave, string IsLineManager1)
    {
      //assign values to variable as we will reassing these values to the object
      leave.IsQuotaRequest = false;
      Nullable<int> IsAccepted1 = null;
      Nullable<int> IsAccepted2 = null;
      string Remarks1 = string.Empty;
      string Remarks2 = string.Empty;
      
      if (IsLineManager1 == "True")
      {
        IsAccepted1 = leave.IsAccepted1;
        Remarks1 = leave.Remarks1;
      
      }
      else
      {
        IsAccepted2 = leave.IsAccepted2;
        Remarks2 = leave.Remarks2;
      }
      //--------------------------------------------------
      Leave leaveOld = db.Leaves.FirstOrDefault(x => x.Id == leave.Id);


      if (leaveOld.LeaveTypeId == 1 || leaveOld.LeaveTypeId == 2 || leaveOld.LeaveTypeId == 10) // Casual Short Day
      {
        var balance = db.LeaveBalances
      .Where(x =>
          x.UserLeavePolicyId == leaveOld.UserLeavePolicyID &&
          x.UserId == leaveOld.UserId &&
          x.LeaveTypeId == leaveOld.LeaveTypeId)
      .Sum(x => (decimal?)x.Balance) ?? 0;


        var approvedDays = db.Leaves
            .Where(x =>
                x.UserId == leaveOld.UserId &&
                x.Id != leave.Id &&
                x.UserLeavePolicyID == leaveOld.UserLeavePolicyID &&
                x.IsAccepted1 == 1 &&
                x.IsAccepted2 == 1 &&
                x.LeaveTypeId == leaveOld.LeaveTypeId)
            .Sum(x => (decimal?)x.TotalDays) ?? 0;


        //  ONLY selected leaves (bulk)
        var requestedDays =  db.Leaves
            .Where(x =>
                x.Id == leave.Id
                && x.UserId == leaveOld.UserId
                && (x.IsAccepted1 != 1 || x.IsAccepted2 != 1)
                && x.UserLeavePolicyID == leaveOld.UserLeavePolicyID &&
                x.LeaveTypeId == leaveOld.LeaveTypeId)
            .Sum(x => (decimal?)x.TotalDays) ?? 0;


        //  Final check
        if ((approvedDays+ requestedDays) > balance)
        {
          TempData["ErrorMessage"] = "The selected customer's leave request exceeds the available balance.";
          AspNetUser admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leaveOld.LineManager1Id);


          string name = leaveOld.AspNetUser.EmpolyeeName;
          StringBuilder sb = new StringBuilder();
          sb.AppendLine("Dear" + name + ",");
          sb.AppendLine("Your leave request cannot be processed as it exceeds your available leave balance.");
          sb.AppendLine("Please contact your manager for further assistance.");
          string body = sb.ToString();

            
          SendEmail.SendsEmail(leaveOld.AspNetUser.Email, "Leave Request Balance Exceeds", body);

          //SendEmail.SendEmailUsingLeavON(leave, admin, leave.AspNetUser, "LeaveResponse");
          return RedirectToAction("Index");
        }
      }

      leave = leaveOld;





      if (IsLineManager1 == "True")
      {
        leave.IsAccepted1 = IsAccepted1;
        leave.Remarks1 = Remarks1;
        leave.ResponseDate1 = DateTime.Now;

        //if (IsAccepted1 == Consts.ApprovedWithComments)
        //{
        //  leave.Remarks1 = string.Empty;
        //  leave.TotalDays = decimal.Parse(Remarks1);
        //}

        if (leave.LineManager1Id == leave.LineManager2Id)
        {
          leave.IsAccepted2 = IsAccepted1;
          leave.Remarks2 = leave.Remarks1;
          leave.ResponseDate2 = DateTime.Now;
          if (leave.IsAccepted2 > Consts.Rejected) CalculateAndChangeLeaveBalance(ref leave);
        }
      }
      else
      {
        leave.IsAccepted2 = IsAccepted2;
        leave.Remarks2 = Remarks2;
        leave.ResponseDate2 = DateTime.Now;
        if (leave.IsAccepted2 > Consts.Rejected) CalculateAndChangeLeaveBalance(ref leave);
      }
      //---------------------save and send emails------------------------------------------------
      if (ModelState.IsValid) 
      {
        db.Entry(leave).State = EntityState.Modified;

        await db.SaveChangesAsync();
        if (IsLineManager1 == "True")
        {
          // Sending email to LineManager2 only if LineManager1Id is not equal to LineManager2Id
          if (leave.LineManager1Id != leave.LineManager2Id)
          {
            //sending email to 2nd admin for request of second acceptance
            AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
            SendEmail.SendEmailUsingLeavON(leave,  leave.AspNetUser, admin2, "LeaveRequest");
          }

          //sending eamil to employee from first admin //approved or disapproved
          AspNetUser admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
          SendEmail.SendEmailUsingLeavON(leave,  admin, leave.AspNetUser, "LeaveResponse");

          //if (leave.LineManager1Id == leave.LineManager2Id)
          //{
          //  admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
          //  SendEmail.SendEmailUsingLeavON(leave, SendEmail.LeavON_Email, SendEmail.LeavON_Password, admin, leave.AspNetUser, "LeaveResponse");
          //}

        }
        else
        {
          AspNetUser admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
          SendEmail.SendEmailUsingLeavON(leave,   admin, leave.AspNetUser, "LeaveResponse");
        }

        //AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
        //SendEmail.SendEmailUsingLeavON(SendEmail.LeavON_Email, SendEmail.LeavON_Password, leave.AspNetUser, admin2, "LeaveRequest");

        return RedirectToAction("Index");
      }
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      return View(leave);
    }
    // GET: Leaves/Edit/5
    public async Task<ActionResult> EditCompensatoryQuotaResponse(decimal id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      Leave leave = await db.Leaves.FindAsync(id);
      if (leave == null)
      {
        return HttpNotFound();
      }
      if (User.Identity.GetUserId() == leave.LineManager1Id || User.Identity.GetUserId() == leave.LineManager2Id)
      {

      }
      else
      {
        return RedirectToAction("Index");
      }
      //ViewBag.LineManager1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id).UserName;
      //ViewBag.LineManager2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id).UserName;
      List<AspNetUser> Seniors = GetSeniorStaff();
      bool IsLineManager1 = false;
      if (User.Identity.GetUserId() == leave.LineManager1Id)
      {
        IsLineManager1 = true;
      }
      else
      {
        IsLineManager1 = false;
      }
      ViewBag.IsLineManager1 = IsLineManager1;

      ViewBag.LineManagers = new SelectList(Seniors, "Id", "UserName");
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      var user = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);

      ViewBag.JoiningDate = user != null ? (user.JoiningDate.HasValue ? String.Format( LeaveON.UtilityClasses.Constants.DateFormatDayMonthYear, user.JoiningDate.Value)  : String.Empty ): String.Empty;

      ViewBag.ApplicantName = user.UserName;
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      ViewBag.UserLeavePolicyId = leave.UserLeavePolicyID;
      ViewBag.LeaveUserId = leave.AspNetUser.Id;
      return View(leave);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditCompensatoryQuotaResponse([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,ResponseDate1,ResponseDate2,IsAccepted1,IsAccepted2,LineManager1Id,LineManager2Id,Remarks1,Remarks2,DateCreated,DateModified,UserLeavePolicyId")] Leave leave, string IsLineManager1)
    {
      //----------------------------get new value----------------------------------------------
      //assign values to variable as we will reassing these values to the object
      //leave.IsQuotaRequest = true; no need to assing ture. when we get old leave few line ahead there is ture in IsQotaRequest
      Nullable<int> IsAccepted1 = null;
      Nullable<int> IsAccepted2 = null;
      string Remarks1 = string.Empty;
      string Remarks2 = string.Empty;
      //DateTime startDate = leave.StartDate;
      //DateTime endDate = leave.EndDate;
      //decimal totalDays;
      //if (leave.TotalDays == null)
      //{
      //  totalDays = (decimal)(endDate - startDate).TotalDays + 1;
      //}
      //else
      //{
      //  totalDays = leave.TotalDays.Value;
      //}

      if (IsLineManager1 == "True")
      {
        IsAccepted1 = leave.IsAccepted1;
        //if (IsAccepted1 == Consts.ApprovedWithComments) Remarks1 = (leave.Remarks1 == null) ? string.Empty : leave.Remarks1.Trim();
        Remarks1 = leave.Remarks1;
      }
      else
      {
        IsAccepted2 = leave.IsAccepted2;
        //if (IsAccepted2 == Consts.ApprovedWithComments) Remarks2 = (leave.Remarks2 == null) ? string.Empty : leave.Remarks2.Trim();
        Remarks2 = leave.Remarks2;
      }
      //--------------------------get old leave and put new values to it------------------------
      Leave leaveOld = db.Leaves.FirstOrDefault(x => x.Id == leave.Id);
      leave = leaveOld;
      //leave.TotalDays = totalDays;
      leave.TotalDays = (decimal)(leave.EndDate - leave.StartDate).TotalDays + 1;
      if (IsLineManager1 == "True")
      {
        leave.IsAccepted1 = IsAccepted1;
        leave.Remarks1 = Remarks1;
        //if (!(string.IsNullOrEmpty(Remarks1))) leave.TotalDays = decimal.Parse(Remarks1);
        leave.ResponseDate1 = DateTime.Now;
        //if (IsAccepted1 == Consts.ApprovedWithComments)
        //{
        //  leave.StartDate = startDate;
        //  leave.EndDate = endDate;
        //  leave.TotalDays = totalDays;
        //}

        if (leave.LineManager1Id == leave.LineManager2Id)
        {
          leave.IsAccepted2 = IsAccepted1;
          leave.Remarks2 = leave.Remarks1;
          leave.ResponseDate2 = DateTime.Now;
          // calculatin will perform when linemanager 2 will aprove so it is in if condition
          if (leave.IsAccepted2 > Consts.Rejected) CalculateAndChangeLeaveBalanceQuota(ref leave);
        }

      }
      else
      {
        leave.IsAccepted2 = IsAccepted2;
        leave.Remarks2 = Remarks2;
        leave.ResponseDate2 = DateTime.Now;
        //if (IsAccepted2 == Consts.ApprovedWithComments)
        //{
        //  leave.StartDate = startDate;
        //  leave.EndDate = endDate;
        //  leave.TotalDays = totalDays;
        //}
        if (leave.IsAccepted2 > Consts.Rejected) CalculateAndChangeLeaveBalanceQuota(ref leave);

      }

      if (ModelState.IsValid)
      {
        db.Entry(leave).State = EntityState.Modified;

        await db.SaveChangesAsync();
        //------------------------------sending mail----------------------------------------------
        if (IsLineManager1 == "True")
        {
          if (leave.IsAccepted1 > Consts.Rejected && leave.LineManager1Id != leave.LineManager2Id)
          {
            //sending email to 2nd admin for request of second acceptance
            AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
            SendEmail.SendEmailUsingLeavON(leave,  leave.AspNetUser, admin2, "LeaveRequest");
          }
          //sending eamil to employee from first admin //approved or disapproved
          AspNetUser admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
          SendEmail.SendEmailUsingLeavON(leave, admin, leave.AspNetUser, "LeaveResponse");
          //if (leave.LineManager1Id == leave.LineManager2Id)
          //{
          //  admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
          //  SendEmail.SendEmailUsingLeavON(leave, SendEmail.LeavON_Email, SendEmail.LeavON_Password, admin, leave.AspNetUser, "LeaveResponse");
          //}
        }
        else
        {
          AspNetUser admin = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
          SendEmail.SendEmailUsingLeavON(leave,  admin, leave.AspNetUser, "LeaveResponse");
        }

        //AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
        //SendEmail.SendEmailUsingLeavON(SendEmail.LeavON_Email, SendEmail.LeavON_Password, leave.AspNetUser, admin2, "LeaveRequest");

        return RedirectToAction("QuotaResponseHistory");
      }
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      return View(leave);
    }
    public void CalculateAndChangeLeaveBalance(ref Leave leave)
    {
      // Calculate the leave balance
      LeaveBalance leaveBalance = CalculateLeaveBalance(ref leave);

      if (leave.LeaveTypeId != 7 && leave.LeaveTypeId != 8)
      {
        // Update the attendance records to reflect the leave
        // Extract necessary data into local variables
        var empNum = leave.AspNetUser.BioStarEmpNum;
        var startDate = leave.StartDate;
        var endDate = leave.EndDate;
        var leaveType = leave.LeaveType.Name;
        var leaveTypeID = leave.LeaveTypeId;

        //var attendanceRecordsToUpdate = db.AttendanceDatas
        //    .Where(ad => ad.EmployeeID == empNum
        //                 && ad.CreatedDate >= startDate
        //                 && ad.CreatedDate <= endDate)
        //    .ToList();

        //foreach (var record in attendanceRecordsToUpdate)
        //{
        //  record.IsLeave = true;
        //  record.LeaveType = leaveType;
        //  record.LeaveTypeID = leaveTypeID;
        //}

        db.SaveChanges();
      }

      if (leaveBalance == null)
      {
        //new
        int userLeavePolicyId = (int)leave.UserLeavePolicyID;
        leaveBalance = new LeaveBalance(ref leave);
        leaveBalance.Taken = leave.TotalDays;
        //leaveBalance.Balance -= leave.TotalDays;

        var userPolicy = db.UserLeavePolicies.FirstOrDefault(x => x.Id == userLeavePolicyId);
        if (leave.AspNetUser.JoiningDate.HasValue && leave.AspNetUser.JoiningDate > userPolicy.FiscalYearStart)
        {
          int joiningYear = leave.AspNetUser.JoiningDate.Value.Year;
          int joiningMonth = leave.AspNetUser.JoiningDate.Value.Month;
          int workedMonths = ((userPolicy.FiscalYearEnd.Value.Year - joiningYear) * 12) + userPolicy.FiscalYearEnd.Value.Month - joiningMonth + 1;

          int? assignedLeaveQuota = db.UserLeavePolicyDetails
              .Where(lb => lb.UserLeavePolicyId == userLeavePolicyId &&
                           (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
              .Select(lb => (int?)lb.Allowed)
              .Sum() ?? 0;

          double proratedLeaves = (workedMonths / 12.0) * (assignedLeaveQuota ?? 0);

          int finalProrated = proratedLeaves % 1 >= 0.5
              ? (int)Math.Ceiling(proratedLeaves)
              : (int)Math.Floor(proratedLeaves);
          // Split the prorated value into casual and annual
          int half = finalProrated / 2;
          int casualLeave = half + (finalProrated % 2 != 0 ? 1 : 0);
          int annualLeave = half;

          if (leave.LeaveType.Id == 2)
          {
            leaveBalance.Balance = annualLeave;
            leaveBalance.Balance -= leave.TotalDays;
          }
          if (leave.LeaveType.Id == 1)
          {
            leaveBalance.Balance = casualLeave;
            leaveBalance.Balance -= leave.TotalDays;
          }
        }
        else
        {
          leaveBalance.Balance -= leave.TotalDays;
        }

        leaveBalance.UserId = leave.UserId;
        leaveBalance.LeaveTypeId = leave.LeaveTypeId;
        leaveBalance.UserLeavePolicyId = leave.UserLeavePolicyID;
        if (leave.LeaveTypeId == 7)
        {
          leaveBalance.HoursTaken = (int)(leave.EndDate - leave.StartDate).TotalHours;
        }
        db.LeaveBalances.Add(leaveBalance);
      }
      else
      {
        //old
        leaveBalance.Taken += leave.TotalDays;
        leaveBalance.Balance -= leave.TotalDays;
        if (leave.LeaveTypeId == 7) // Assuming 7 is the ID for casual short day
        {
          int hoursToAdd = (int)(leave.EndDate - leave.StartDate).TotalHours;
          var leaveUserID = leave.UserId;
          var leavePolicyID = leave.UserLeavePolicyID;
          leaveBalance.Balance = 0;
          if (leaveBalance.HoursTaken + hoursToAdd >= 8)
          {
            int excessHours = (int)(hoursToAdd + leaveBalance.HoursTaken - 8);
            leaveBalance.HoursTaken = excessHours > 0 ? excessHours : 0; // Reset or set to 0
            // Deduct one day from sick casual leave
            var sickCasualLeave = db.LeaveBalances
                .FirstOrDefault(lb => lb.UserId == leaveUserID &&
                                      lb.LeaveTypeId == 1 && //1 is the ID for sick casual leave
                                      lb.UserLeavePolicyId == leavePolicyID);

            if (sickCasualLeave != null && sickCasualLeave.Balance > 0)
            {
              sickCasualLeave.Balance--;
              sickCasualLeave.Taken++;
            }
            else
            {
              leaveBalance = new LeaveBalance(ref leave);
              leaveBalance.Taken = 1;
              leaveBalance.Balance = (db.UserLeavePolicyDetails.Where(leaveDetail => leaveDetail.LeaveTypeId == 1 &&
                    leaveDetail.UserLeavePolicyId == leavePolicyID).Select(detail => detail.Allowed).FirstOrDefault()) - 1;
              leaveBalance.UserId = leaveUserID;
              leaveBalance.LeaveTypeId = 1;
              leaveBalance.UserLeavePolicyId = leave.UserLeavePolicyID;
              db.LeaveBalances.Add(leaveBalance);
              db.SaveChanges();
            }
          }
          else
          {
            if(leaveBalance.HoursTaken != null)
            {
              leaveBalance.HoursTaken += hoursToAdd;
            }
            else
            {
              leaveBalance.HoursTaken = hoursToAdd;
            }
          }
        }
        db.Entry(leaveBalance).State = EntityState.Modified;
        db.SaveChanges();
      }
      ///////////////
    }
  
    public void CalculateAndChangeLeaveBalanceQuota(ref Leave leave)
    {
      LeaveBalance leaveBalance = CalculateLeaveBalanceQuota(ref leave);

      if (leaveBalance == null)
      {
        //new
        leaveBalance = new LeaveBalance(ref leave);
        leaveBalance.UserId = leave.UserId;
        leaveBalance.LeaveTypeId = leave.LeaveTypeId;
        leaveBalance.UserLeavePolicyId = leave.UserLeavePolicyID;
        db.LeaveBalances.Add(leaveBalance);
      }
      else
      {
        leaveBalance.Balance += leave.TotalDays;
        db.Entry(leaveBalance).State = EntityState.Modified;
      }
    }
    public LeaveBalance CalculateLeaveBalance(ref Leave leave)
    {
      string UserId = leave.UserId;
      int LeaveTypeId = leave.LeaveTypeId;
      int userLeavePolicyId = (int)leave.UserLeavePolicyID;
      LeaveBalance leaveBalance = leave.AspNetUser.LeaveBalances.FirstOrDefault(x => x.UserId == UserId && x.LeaveTypeId == LeaveTypeId && x.UserLeavePolicyId == userLeavePolicyId );
      return leaveBalance;
    }
    public LeaveBalance CalculateLeaveBalanceQuota(ref Leave leave)
    {
      string UserId = leave.UserId;
      int LeaveTypeId = leave.LeaveTypeId;
      //List<int> weeklyOffDays = leave.AspNetUser.UserLeavePolicy.WeeklyOffDays.Split(',').Select(int.Parse).ToList();

      LeaveBalance lb = leave.AspNetUser.LeaveBalances.FirstOrDefault(x => x.UserId == UserId && x.LeaveTypeId == LeaveTypeId);
      return lb;

    }
    public static int CalculateLeaveDays(DateTime startDate, DateTime endDate, List<DateTime> excludeDates)
    {
      int count = 0;
      for (DateTime DateIdx = startDate; DateIdx < endDate; DateIdx = DateIdx.AddDays(1))
      {
        if (DateIdx.DayOfWeek != DayOfWeek.Sunday && DateIdx.DayOfWeek != DayOfWeek.Saturday)
        {
          bool excluded = false;
          for (int i = 0; i < excludeDates.Count; i++)
          {
            if (DateIdx.Date.CompareTo(excludeDates[i].Date) == 0)
            {
              excluded = true;
              break;
            }
          }

          if (!excluded)
          {
            count++;
          }
        }
      }

      return count;
    }

    // GET: Leaves/Delete/5
    public async Task<ActionResult> Delete(decimal id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      Leave leave = await db.Leaves.FindAsync(id);
      if (leave == null)
      {
        return HttpNotFound();
      }
      return View(leave);
    }

    // POST: Leaves/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirmed(decimal id)
    {
      Leave leave = await db.Leaves.FindAsync(id);
      db.Leaves.Remove(leave);
      await db.SaveChangesAsync();
      return RedirectToAction("Index");
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        db.Dispose();
      }
      base.Dispose(disposing);
    }

    private Task<List<Leave>> GetLeavesReport(string formattedStartDate, string formattedEndDate, List<string> UserIds)
    {
      DateTime startDate = DateTime.ParseExact(formattedStartDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      List<Leave> LstLeavesData = new List<Leave>();
    //  TimeSpan totalWorkingHoursAllUsers = TimeSpan.Zero;
      //TimeSpan totalTimeAllUsers = TimeSpan.Zero;

      // Fetch Leave data for each user
      foreach (string UserId in UserIds)
      {
        // Fetch leaves records for this user within the provided date range
        var leaveRecords = db.Leaves
        .Where(a => a.UserId == UserId && a.StartDate >= startDate && a.StartDate <= endDate)
        .OrderBy(a => a.StartDate)
        .ToList();


        if (!leaveRecords.Any()) continue; // If no records found, skip to the next user

        // Process each leave record
        foreach (var record in leaveRecords)
        {
          var user = db.AspNetUsers.Find(record.UserId);
          string username = user != null
              ? user.UserName.Split('@')[0].Replace(".", " ")
              : "N/A";
          // Map the data directly from leave table
          LstLeavesData.Add(new Leave()
          {
            DateCreated = record.DateCreated ?? DateTime.MinValue,
            StartDate = record.StartDate,
            EndDate = record.EndDate,
            UserId = username,
            //LeaveTypeName = record.LeaveTypeId != 0 ? db.LeaveTypes.Find(record.LeaveTypeId)?.Name : "N/A",
            TotalDays = record.TotalDays ?? 0,
            Reason = record.Reason ?? null,
            IsAccepted1 = record.IsAccepted1,
            IsAccepted2 = record.IsAccepted2,
            Remarks1 = record.Remarks1,
            Remarks2 = record.Remarks2,
            IsShortLeave = record.IsShortLeave,
            LineManager1Id = record.LineManager1Id != null ? db.AspNetUsers.Find(record.LineManager1Id).UserName : "N/A",
            LineManager2Id = record.LineManager2Id != null ? db.AspNetUsers.Find(record.LineManager2Id).UserName : "N/A",
          });
        }
      }



      return Task.FromResult(LstLeavesData);
    }


    public async Task<ActionResult> LeaveReport(string StartDate, string EndDate, List<string> UserIds)
    {
      try
      {
        //ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime startDate, endDate;

        string userId = User.Identity.GetUserId();
        //IQueryable<Leave> LstAttendances = new IQueryable<Leave>();

        List<Leave> LstLeaves = new List<Leave>();

        // Set default date if ReqMonthYear is empty
        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {

          startDate = DateTime.ParseExact(StartDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
          endDate = DateTime.ParseExact(EndDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
          // In case of empty parameters or first time
          startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
          endDate = DateTime.Now;

          ViewBag.Employees = new SelectList(db.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);

        }

        //StartDate and EndDate for display
        ViewBag.StartDate = startDate.ToString("dd-MMM-yyyy");
        ViewBag.EndDate = endDate.ToString("dd-MMM-yyyy");

        // Role-based data population
        if (User.IsInRole("Admin"))
        {
          var departments = db.AspNetUsers
                 .Where(u => !string.IsNullOrEmpty(u.DepartmentName) && u.IsActive == true && u.BioStarEmpNum.HasValue)
                 .Select(u => u.DepartmentName)
                 .Distinct()
                 .Select(d => new SelectListItem { Value = d, Text = d })
                 .ToList();
          ViewBag.Departments = departments;
          ViewBag.Employees = new SelectList(db.AspNetUsers, "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("Manager") || User.IsInRole("User"))
        else if (User.IsInRole("Manager") || User.IsInRole("User"))
        {

          var managerDepartment = db.AspNetUsers.FirstOrDefault(u => u.Id == userId && u.IsActive == true && u.BioStarEmpNum.HasValue).DepartmentName;
          ViewBag.Departments = new SelectList(new List<string> { managerDepartment });

          var employeesUnderManager = db.AspNetUsers.Where(u => (u.ManagerID == userId || u.Manager2ID == userId)).ToList();
          ViewBag.Employees = new SelectList(employeesUnderManager, "Id", "UserName");
          //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {
          // Format the date range for querying
          string formattedStartDate = startDate.ToString("dd-MM-yyyy");
          string formattedEndDate = endDate.ToString("dd-MM-yyyy");
          //var User_Ids = UserIds.Select(id => int.Parse(id)).ToList();
          var User_Ids = UserIds.Select(id => id).ToList();

          LstLeaves = await GetLeavesReport(formattedStartDate, formattedEndDate, User_Ids);


        }
        //return View(await db.UD_TB_AccessTime_Data.ToListAsync());
        if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate))
        {
          return View();
        }
        else
        {
          return PartialView("_LeaveReport", LstLeaves.OrderBy(i => i.DateCreated).ToList());
        }
      }
      catch (Exception ex)
      {
        throw (ex);
      }

    }


    public async Task<ActionResult> LeaveAbsentReport()
    {
      try
      {
        //ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime startDate, endDate;

        string userId = User.Identity.GetUserId();
        //IQueryable<Leave> LstAttendances = new IQueryable<Leave>();

        List<Leave> LstLeaves = new List<Leave>();
 
          // In case of empty parameters or first time
          startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
          endDate = DateTime.Now;

          //ViewBag.Employees = new SelectList(db.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);

        

        //StartDate and EndDate for display
        ViewBag.StartDate = startDate.ToString("dd-MMM-yyyy");
        ViewBag.EndDate = endDate.ToString("dd-MMM-yyyy");

        // Role-based data population
        if (User.IsInRole("Admin"))
        {
          var departments = db.AspNetUsers
                 .Where(u => !string.IsNullOrEmpty(u.DepartmentName) && u.IsActive==true && u.BioStarEmpNum.HasValue && u.BioStarEmpNum.Value>0 && u.IsDeleted != true)
                 .Select(u => u.DepartmentName)
                 .Distinct()
                 .Select(d => new SelectListItem { Value = d, Text = d })
                 .ToList();
          ViewBag.Departments = new SelectList(departments, "Value", "Text");// departments;

          var user = new List<AspNetUser>(); //db.AspNetUsers.Where(k=>k.IsActive == true && k.BioStarEmpNum.HasValue).ToList();
          var userlist = user.Select(k => new AspNetUser
          {
            UserName = k.UserName.Split('@')[0].Replace('.', ' '),
            BioStarEmpNum = k.BioStarEmpNum
          }).ToList();

          ViewBag.Employees = userlist;
          // ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("Manager") || User.IsInRole("User"))
        else if (User.IsInRole("Manager") || User.IsInRole("User"))
        {

          var managerDepartment = db.AspNetUsers
     .Where(u => u.Id == userId)
     .Select(u => u.DepartmentName)
     .FirstOrDefault();

          ViewBag.Departments = new SelectList(
              new List<SelectListItem>
              {
        new SelectListItem
        {
            Value = managerDepartment,
            Text = managerDepartment
        }
              },
              "Value",
              "Text"
          );

          ViewBag.SelectedDepartments = managerDepartment;
          var employeesUnderManager = db.AspNetUsers.Where(u => (u.ManagerID == userId  &&   u.IsDeleted != null) && u.BioStarEmpNum.HasValue && u.BioStarEmpNum.Value>0 ).ToList();

          //db.AspNetUsers.Where(k=>k.IsActive == true && k.BioStarEmpNum.HasValue).ToList();
          var userlist = employeesUnderManager.Select(k => new AspNetUser
          {
            UserName = k.UserName.Split('@')[0].Replace('.', ' '),
            BioStarEmpNum = k.BioStarEmpNum
          }).ToList();

          ViewBag.Employees = userlist;

          //;new SelectList(employeesUnderManager, "Id", "UserName");
          //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
          //ViewBag.SelectedEmployees = UserIds;
        }
        //if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        //{
        //  // Format the date range for querying
        //  string formattedStartDate = startDate.ToString("dd-MM-yyyy");
        //  string formattedEndDate = endDate.ToString("dd-MM-yyyy");
        //  //var User_Ids = UserIds.Select(id => int.Parse(id)).ToList();
        //  var User_Ids = UserIds.Select(id => id).ToList();

        //  LstLeaves = await GetLeavesReport(formattedStartDate, formattedEndDate, User_Ids);


        //}
        //return View(await db.UD_TB_AccessTime_Data.ToListAsync());
       

        //if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate))
        //{
        //  return View();
        //}
        //else
        //{
        //  return PartialView("_LeaveReport", LstLeaves.OrderBy(i => i.DateCreated).ToList());
        //}
      }
      catch (Exception ex)
      {
        throw (ex);
      }
      return View();

    }

    [HttpPost]
    public async Task<ActionResult>  GetLeaveAbsentReport(string startDate, string endDate, List<string> userIds)
    {
      var sstartDate = new SqlParameter("@StartDate", startDate);
      var sendDate = new SqlParameter("@EndDate", endDate);
      var suserIds = new SqlParameter("@UserIds", userIds);
      List<LeaveAbsentModel> list = new List<LeaveAbsentModel>();
      try
      {
        string userId = string.Join(",", userIds);

        var result = db.Database.SqlQuery<LeaveAbsentModel>(
    "EXEC GetAbsentAttendanceReport @StartDate, @EndDate, @UserIds",
    new SqlParameter("@StartDate", startDate),
    new SqlParameter("@EndDate", endDate),
    new SqlParameter("@UserIds", userId)
).ToList();
         list = result.OrderBy(i => i.DateCreated).ToList();
      }

      catch (Exception ex)
      {
        //throw (ex);
      }


     

      return PartialView("_LeaveAbsentReport", list);
    }

    public ActionResult GetUsersByDepartments(List<string> departmentNames)
    {
      if (departmentNames == null || !departmentNames.Any())
      {
        return Json(new List<SelectListItem>(), JsonRequestBehavior.AllowGet);
      }

      var users = db.AspNetUsers
          .Where(u => departmentNames.Contains(u.DepartmentName))
          .AsEnumerable()
          .Select(u => new SelectListItem
          {
            Value = u.Id,
            /* Text = u.UserName*/
            Text = u.UserName.Split('@')[0].Replace('.', ' ')
          }).OrderBy(i => i.Text).ToList();
      //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => departmentNames.Contains(u.DepartmentName)), "BioStarEmpNum", "UserName");
      return Json(users, JsonRequestBehavior.AllowGet);
    }
  }
}
