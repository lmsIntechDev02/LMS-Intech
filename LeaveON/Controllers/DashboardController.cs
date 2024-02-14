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
      int policyId = db.AspNetUsers.FirstOrDefault(x => x.Id == userId).UserLeavePolicyId.GetValueOrDefault();
      UserLeavePolicy userLeavePolicy= db.UserLeavePolicies.Find(policyId);

      if (policyId == 0)
      {
        TempData["ErrorMessage"] = "It looks like this policy hasn't been assigned to your profile. Please get in touch with our support team for help.";
        return RedirectToAction("General", "Error");
      }
     
      List<UserLeavePolicyDetail> LstUserLeavePolicyDetail = userLeavePolicy.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == policyId && (x.LeaveTypeId == Consts.SickCasualLeaveId || x.LeaveTypeId == Consts.AnnualLeaveId || x.LeaveTypeId == Consts.CompensatoryLeaveTypeId) ).ToList();
      dashboard.MyAllowedLeaves = LstUserLeavePolicyDetail.Sum(x => x.Allowed).Value;

      List<LeaveBalance> LstLeaveBalance = db.LeaveBalances.Where(x => x.UserLeavePolicyId == policyId && x.UserId== userId).ToList();
      dashboard.MyTakenLeaves = LstLeaveBalance.Sum(x => x.Taken).Value;

      dashboard.MyBalanceLeaves = (decimal)(dashboard.MyAllowedLeaves - LstLeaveBalance.Where(y => y.LeaveTypeId == Consts.SickCasualLeaveId || y.LeaveTypeId == Consts.AnnualLeaveId || y.LeaveTypeId == Consts.CompensatoryLeaveTypeId).Sum(y => y.Taken));

      List<Leave> LstLeaveAprovalRejected = db.Leaves.Where(x => x.UserId == userId && x.IsAccepted1==0).ToList();
      dashboard.MyLeavesRefused = LstLeaveAprovalRejected.Count;

      List<Leave> LstLeaveAprovalPending = db.Leaves.Where(x => x.UserId == userId &&  x.IsAccepted1 ==null ).ToList();
      dashboard.MyLeavesPending = LstLeaveAprovalPending.Count;

      List<Leave> LstLeaveAprovalApproved = db.Leaves.Where(x => x.UserId == userId && x.IsAccepted1 > 0).ToList();
      dashboard.MyLeavesApproved = LstLeaveAprovalApproved.Count;

      AspNetUser aspNetUser = db.AspNetUsers.Find(userId);
      dashboard.EmployeeNumber = aspNetUser.BioStarEmpNum.Value;
      dashboard.EmployeeName=aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
      dashboard.Policy = userLeavePolicy.Description;
      dashboard.Country = aspNetUser.CntryName;

      return View(dashboard);

    }
  }
}
