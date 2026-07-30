using LeaveON.UtilityClasses;
using LMS.Constants;
using Microsoft.AspNet.Identity;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeaveON.Controllers
{
  [Authorize(Roles = "Admin,Manager,User")]
  public class DashboardController : Controller
  {
    // GET: Dashboard
    private LeaveONEntities db = new LeaveONEntities();
    public ActionResult Index()
    {
      Dashboard dashboard = new Dashboard();
      // db.Leaves.Sum(x=>x..LeaveTypeId!=0)

      string userId = User.Identity.GetUserId();
      var curr_user = db.AspNetUsers.FirstOrDefault(x => x.Id == userId);
      int policyId = curr_user.UserLeavePolicyId.GetValueOrDefault();
      UserLeavePolicy userLeavePolicy= db.UserLeavePolicies.Find(policyId);

      if (policyId == 0)
      {
        TempData["ErrorMessage"] = "It looks like this policy hasn't been assigned to your profile. Please get in touch with our support team for help.";
        return RedirectToAction("General", "Error");
      }

      int proratedLeaves = getProratedLeaves(curr_user, policyId);
      int half = proratedLeaves / 2;
      int casualLeave = half + (proratedLeaves % 2 != 0 ? 1 : 0);
      int annualLeave = half;

      List<UserLeavePolicyDetail> LstUserLeavePolicyDetail = userLeavePolicy.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == policyId && (x.LeaveTypeId == Consts.SickCasualLeaveId || x.LeaveTypeId == Consts.AnnualLeaveId || x.LeaveTypeId == Consts.CompensatoryLeaveTypeId) ).ToList();


      List<LeaveBalance> LstLeaveBalance = db.LeaveBalances.Where(x => x.UserLeavePolicyId == policyId && x.UserId== userId).ToList();
      dashboard.MyTakenLeaves = LstLeaveBalance.Sum(x => x.Taken).Value;
      int adjustLeaveBalance = Convert.ToInt32(LstLeaveBalance.Where(k => k.LeaveTypeId == Consts.AnnualLeaveId).Sum(k => k.AnnualAmountAjustmesnt.HasValue ? k.AnnualAmountAjustmesnt : 0));
      dashboard.MyAllowedLeaves = proratedLeaves + adjustLeaveBalance;


      dashboard.MyBalanceLeaves = (decimal)(dashboard.MyAllowedLeaves - LstLeaveBalance.Where(y => y.LeaveTypeId == Consts.SickCasualLeaveId || y.LeaveTypeId == Consts.AnnualLeaveId || y.LeaveTypeId == Consts.CompensatoryLeaveTypeId).Sum(y => y.Taken));

      List<Leave> LstLeaveAprovalRejected = db.Leaves.Where(x => x.UserId == userId && x.UserLeavePolicyID == policyId && x.IsAccepted1==0).ToList();
      dashboard.MyLeavesRefused = LstLeaveAprovalRejected.Count;

      List<Leave> LstLeaveAprovalPending = db.Leaves.Where(x => x.UserId == userId && x.UserLeavePolicyID == policyId &&  x.IsAccepted1 ==null ).ToList();
      dashboard.MyLeavesPending = LstLeaveAprovalPending.Count;

      List<Leave> LstLeaveAprovalApproved = db.Leaves.Where(x => x.UserId == userId && x.UserLeavePolicyID == policyId && x.IsAccepted1 > 0 ).ToList();
      dashboard.MyLeavesApproved = LstLeaveAprovalApproved.Count;

      List<UserLeavePolicyDetail> TotalAnnualLeaves = userLeavePolicy.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == policyId &&  x.LeaveTypeId == Consts.AnnualLeaveId).ToList();
      //  dashboard.TotalAnnualLeaves = TotalAnnualLeaves.Count;
      //int adjustLeaveBalance = Convert.ToInt32(LstLeaveBalance.Where(k => k.LeaveTypeId == Consts.AnnualLeaveId).Sum(k => k.AnnualAmountAjustmesnt.HasValue ? k.AnnualAmountAjustmesnt : 0));
      dashboard.TotalAnnualLeaves = annualLeave + adjustLeaveBalance;


      List<Leave> BalanceAnnualLeaves = db.Leaves.Where(x => x.UserId == userId && x.UserLeavePolicyID == policyId && x.IsAccepted1 > 0 && x.LeaveTypeId == Consts.AnnualLeaveId).ToList();
      int approvedAnnaulLeaves = (int)BalanceAnnualLeaves.Sum(x => x.TotalDays ?? 0);
      dashboard.BalanceAnnualLeaves = dashboard.TotalAnnualLeaves - approvedAnnaulLeaves;


      AspNetUser aspNetUser = db.AspNetUsers.Find(userId);
      dashboard.EmployeeNumber = aspNetUser.BioStarEmpNum.Value;
      dashboard.EmployeeName=aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
      dashboard.Policy = userLeavePolicy.Description;
      dashboard.Country = aspNetUser.CntryName;
      
      return View(dashboard);

    }

    private int getProratedLeaves(AspNetUser currentUser, int policyId)
    {
      // Prorated Leaves
      DateTime today = DateTime.Today;
      int currentYear = today.Year;

      int workedMonths = 12;


      var userPolicy = db.UserLeavePolicies.FirstOrDefault(x => x.Id == policyId);


      DateTime fiscalStart = (DateTime)userPolicy.FiscalYearStart;
      DateTime fiscalEnd = (DateTime)userPolicy.FiscalYearEnd;

      // If joining date not exists then used ficalStart
      DateTime joiningDate = currentUser.JoiningDate ?? fiscalStart;

      if (joiningDate > fiscalEnd)
      {
        workedMonths = 0;
      }
      else
      {
        DateTime effectiveStart = (joiningDate > fiscalStart) ? joiningDate : fiscalStart;

        // Total months
        workedMonths = ((fiscalEnd.Year - effectiveStart.Year) * 12 + fiscalEnd.Month - effectiveStart.Month + 1);
      }

  
      int? assignedLeaveQuota = policyId != null
                    ? db.UserLeavePolicyDetails
                        .Where(lb => lb.UserLeavePolicyId == policyId &&
                                     (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
                        .Select(lb => (int?)lb.Allowed)
                        .Sum() ?? 0
                    : 0;
      // Prorated Leave Calculation
      double proratedLeaves = (workedMonths / 12.0) * (assignedLeaveQuota ?? 0);
      int proratedLeave;


      // Round to nearest whole number or keep decimal
      //proratedLeaves = Math.Round(proratedLeaves, 2); 
      // Custom rounding logic
      if (proratedLeaves % 1 >= 0.5)
      {
        proratedLeave = (int)Math.Ceiling(proratedLeaves);
      }
      else
      {
        proratedLeave = (int)Math.Floor(proratedLeaves);
      }
      return proratedLeave;

    }
  }
}
