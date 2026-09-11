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
using LMS.Constants;

using LeaveON.Migrations;
using LeaveON.EmailSender;
using LeaveON.UtilityClasses;
using System.Globalization;
using LeaveON.Models;
using LeaveON.Models.DatatableVmModel;

namespace LeaveON.Controllers
{

  [Authorize(Roles = "Admin,Manager,User")]
  //[Authorize(Roles = "DOMAIN\\Admin,DOMAIN\\Manager,DOMAIN\\User")]
  public class LeavesRequestController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: Leaves
    //public async Task<ActionResult> Index(string country)

    public async Task<ActionResult> Index()
    {
      //var leaves = db.Leaves.Include(l => l.LeaveType).Include(l => l.UserLeavePolicy);
      //string LoggedInUserId = User.Identity.GetUserId();
      //IQueryable<Leave> leaves = db.Leaves.Where(x => x.UserId == LoggedInUserId && (x.IsQuotaRequest == false || x.IsQuotaRequest == null)).AsQueryable<Leave>();
      //return View(await leaves.ToListAsync());
      return View();
    }
    [HttpPost]
    public async Task<JsonResult> GetLeaveHistory(DataTableRequest request)
    {
      string loggedInUserId = User.Identity.GetUserId();

      // -----------------------------------------
      // 1. Base Query
      // -----------------------------------------
      var query = db.Leaves
          .Where(x =>
              x.UserId == loggedInUserId &&
              (x.IsQuotaRequest == false || x.IsQuotaRequest == null)
          );

      // -----------------------------------------
      // 2. Total Records
      // -----------------------------------------
      var recordsTotal = await query.CountAsync();

      // -----------------------------------------
      // 3. Search
      // -----------------------------------------
      if (request.Search != null &&
          !string.IsNullOrWhiteSpace(request.Search.Value))
      {
        string search = request.Search.Value.Trim();

        query = query.Where(x =>
            (x.LeaveType != null &&
             x.LeaveType.Name.Contains(search))

            ||

            (x.Reason != null &&
             x.Reason.Contains(search))

            ||

            (x.Remarks1 != null &&
             x.Remarks1.Contains(search))

            ||

            (x.Remarks2 != null &&
             x.Remarks2.Contains(search))

            ||

            (search == "Approved" &&
             ((x.IsAccepted1.HasValue && x.IsAccepted1 > 0) ||
              (x.IsAccepted2.HasValue && x.IsAccepted2 > 0)))

            ||

            (search == "Refused" &&
             ((x.IsAccepted1.HasValue && x.IsAccepted1 == 0) ||
              (x.IsAccepted2.HasValue && x.IsAccepted2 == 0)))
        );
      }

      // -----------------------------------------
      // 4. Filtered Records
      // -----------------------------------------
      var recordsFiltered = await query.CountAsync();

      // -----------------------------------------
      // 5. Sorting
      // -----------------------------------------
      if (request.Order != null && request.Order.Count > 0)
      {
        var order = request.Order[0];

        switch (order.Column)
        {
          case 0:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.DateCreated)
                : query.OrderBy(x => x.DateCreated);
            break;

          case 1:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.StartDate)
                : query.OrderBy(x => x.StartDate);
            break;

          case 2:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.EndDate)
                : query.OrderBy(x => x.EndDate);
            break;

          case 3:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.LeaveType.Name)
                : query.OrderBy(x => x.LeaveType.Name);
            break;

          case 4:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.TotalDays)
                : query.OrderBy(x => x.TotalDays);
            break;

          case 5:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.Reason)
                : query.OrderBy(x => x.Reason);
            break;

          case 6:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.IsAccepted1)
                : query.OrderBy(x => x.IsAccepted1);
            break;

          case 7:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.IsAccepted2)
                : query.OrderBy(x => x.IsAccepted2);
            break;

          default:
            query = query.OrderByDescending(x => x.DateCreated);
            break;
        }
      }
      else
      {
        query = query.OrderByDescending(x => x.DateCreated);
      }

      // -----------------------------------------
      // 6. Get records from database
      // -----------------------------------------
      var data = await query
          .Skip(request.Start)
          .Take(request.Length)
          .ToListAsync();

      // -----------------------------------------
      // 7. Convert to ViewModel AFTER ToListAsync
      //    NO DbFunctions here
      // -----------------------------------------
      var leavehistory = data.Select(x => new LeaveListViewModel
      {
        Id = Convert.ToInt32(x.Id),

        DateCreated = x.DateCreated,

        StartDate = x.StartDate,

        EndDate = x.EndDate,

        LeaveTypeName = x.LeaveType != null
              ? x.LeaveType.Name
              : "",

        IsShortLeave = x.IsShortLeave ?? false,

        TotalDays = x.TotalDays,

        // IMPORTANT:
        // This is normal C# calculation.
        TotalHours = x.IsShortLeave == true
              ? (x.EndDate - x.StartDate).TotalHours
              : 0,

        Reason = x.Reason,

        IsAccepted1 = x.IsAccepted1,

        Remarks1 = x.Remarks1,

        IsAccepted2 = x.IsAccepted2,

        Remarks2 = x.Remarks2,

        ApprovalStatus1 =
              x.IsAccepted1.HasValue && x.IsAccepted1 > 0
                  ? "Approved"
                  : x.IsAccepted1.HasValue && x.IsAccepted1 == 0
                      ? "Refused"
                      : "",

        ApprovalStatus2 =
              x.IsAccepted2.HasValue && x.IsAccepted2 > 0
                  ? "Approved"
                  : x.IsAccepted2.HasValue && x.IsAccepted2 == 0
                      ? "Refused"
                      : ""

      }).ToList();

      // -----------------------------------------
      // 8. Return DataTable response
      // -----------------------------------------
      return Json(new DataTableResponse<LeaveListViewModel>
      {
        draw = request.Draw,
        recordsTotal = recordsTotal,
        recordsFiltered = recordsFiltered,
        data = leavehistory
      });
    }
    public async Task<ActionResult> QuotaRequestHistory()
    {
      //var leaves = db.Leaves.Include(l => l.LeaveType).Include(l => l.UserLeavePolicy);
      string LoggedInUserId = User.Identity.GetUserId();
      IQueryable<Leave> leaves = db.Leaves.Where(x => x.UserId == LoggedInUserId && x.IsQuotaRequest == true).AsQueryable<Leave>();
      return View(await leaves.ToListAsync());

    }

    public async Task<ActionResult> LeaveReport(string StartDate, string EndDate, List<string> UserIds)
    {
      try
      {
        //ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime startDate, endDate;

        string userId = User.Identity.GetUserId();  

        List<TimeData> LstAttendances = new List<TimeData>();

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
                 .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
                 .Select(u => u.DepartmentName)
                 .Distinct()
                 .Select(d => new SelectListItem { Value = d, Text = d })
                 .ToList();
          ViewBag.Departments = departments;
          ViewBag.Employees = new SelectList(db.AspNetUsers, "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("Manager") || User.IsInRole("User"))
        else if (User.IsInRole("Manager"))
        {

          var managerDepartment = db.AspNetUsers.FirstOrDefault(u => u.Id == userId).DepartmentName;
          ViewBag.Departments = new SelectList(new List<string> { managerDepartment });

          var employeesUnderManager = db.AspNetUsers.Where(u => (u.ManagerID == userId || u.Manager2ID == userId)).ToList();
          ViewBag.Employees = new SelectList(employeesUnderManager, "BioStarEmpNum", "UserName");
          //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        else if (User.IsInRole("User"))
        {
          var currentUser = db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
          ViewBag.Departments = new SelectList(new List<string> { currentUser.DepartmentName });
          ViewBag.Employees = new SelectList(new List<AspNetUser> { currentUser }, "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = new List<string> { currentUser.BioStarEmpNum.ToString() }; // Populate selected employee for User
        }

        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {
          // Format the date range for querying
          string formattedStartDate = startDate.ToString("dd-MM-yyyy");
          string formattedEndDate = endDate.ToString("dd-MM-yyyy");
          var User_Ids = UserIds.Select(id => int.Parse(id)).ToList();
          //   LstAttendances = await ConnectToDBandReturnAttendanceReport(formattedStartDate, formattedEndDate, User_Ids);
        //  LstAttendances = await GetAttendanceSummary(formattedStartDate, formattedEndDate, User_Ids);

        }
        //return View(await db.UD_TB_AccessTime_Data.ToListAsync());
        if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate))
        {
          //in case of null param or first time
          if (!(LstAttendances is null))
          {
            return View(LstAttendances.OrderBy(i => i.Date).ToList());

          }
          else
          {
            return View();
          }
        }
        else
        {
          return PartialView("_LeaveReport", LstAttendances.OrderBy(i => i.Date).ToList());
        }
      }
      catch (Exception ex)
      {
        throw (ex);
      }

    }


    // GET: Leaves/Create
    public ActionResult Create()
    {

      //ViewBag.UserId = "d0c9d0b1-d0e8-4d56-a410-72e74af3ced8";
      //ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name");
      string userId = User.Identity.GetUserId();
      var currentUser = db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
      int policyId = currentUser.UserLeavePolicyId.GetValueOrDefault();

   ;

      var filtereLeaves = new SelectList(Utility.FilteredLeavesTaken(userId, policyId), "Id", "Name", "1");


      ViewBag.LeaveTypeIdd = filtereLeaves;
    

      if (policyId > 0)
      {
        ViewBag.UserLeavePolicyId = policyId;
      }
      else
      {
        TempData["ErrorMessage"] = "It looks like this policy hasn't been assigned to your profile. Please get in touch with our support team for help.";
        return RedirectToAction("General", "Error");
      }

      

      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      
   // ViewBag.JoiningDate = currentUser.JoiningDate.Value.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
      ViewBag.JoiningDate = currentUser.JoiningDate.HasValue
             ? currentUser.JoiningDate.Value.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)
             : "Joining date not available";

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

      //if (currentUser.JoiningDate.HasValue)
      //{
      //  //DateTime joiningDate = currentUser.JoiningDate.Value;

      //  if(joiningDate.Year == currentYear)
      //  {
      //    workedMonths = 12 - joiningDate.Month + 1;
      //  }
      //}


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
      string hrbpEmail = String.Empty;
      if (currentUser.HRBPID.HasValue)
      {
        hrbpEmail = currentUser.tblHRBP != null ?currentUser.tblHRBP.HRBPEmail:string.Empty;
      }
      else
      {
        hrbpEmail=db.DepartmentNames.FirstOrDefault(j => j.Name == currentUser.DepartmentName && j.tblHRBP != null).tblHRBP.HRBPEmail;
      }


      ViewBag.HRBPEmail = !string.IsNullOrEmpty(hrbpEmail) ? hrbpEmail : "HRBP email not available";



      //if (string.IsNullOrEmpty(hrbpEmail))
      //{
      //  hrbpEmail = db.AnnualLeaveManagers
      //          .Select(m => m.ManagerEmail)
      //          .FirstOrDefault();
      //}



      ViewBag.proratedLeave = proratedLeave;

      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", null);

      ViewBag.LeaveUserId = userId;
      ViewBag.FiscalYearStart = db.UserLeavePolicies.FirstOrDefault(x => x.Id == policyId).FiscalYearStart;
      ViewBag.FiscalYearEnd = db.UserLeavePolicies.FirstOrDefault(x => x.Id == policyId).FiscalYearEnd;
      ViewBag.ShortLeaveMessage = "test message";
      return View();
    }

    // GET: Leaves/CreateCompensatory
    public ActionResult CreateCompensatoryQuotaRequest()
    {
      //ViewBag.UserId = "d0c9d0b1-d0e8-4d56-a410-72e74af3ced8";
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", Consts.CompensatoryLeaveTypeId);
      ViewBag.CompensatoryLeaveTypeId = Consts.CompensatoryLeaveTypeId;
      string userId = User.Identity.GetUserId();

      // Reload user from DB fresh, ignoring cache
      // Reload user from DB fresh, ignoring cache
      var user = db.AspNetUsers.AsNoTracking()
                               .FirstOrDefault(x => x.Id == userId);

      int policyId = user?.UserLeavePolicyId ?? 0;

      int policyId1 = db.AspNetUsers.FirstOrDefault(x => x.Id == userId).UserLeavePolicyId.GetValueOrDefault();
      //bool found=false;
      //foreach (AspNetUser user in db.AspNetUsers.ToList<AspNetUser>())
      //{
      //  if (user.Id == userId)
      //  {
      //    policyId = user.UserLeavePolicyId;
      //    break;
      //  }
      //}

      // db.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == policyId);
      //ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes.Where(x=>x.UserLeavePolicyDetails.Where(y=>y.UserLeavePolicyId== policyId)), "Id", "Name");
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId");
      if (policyId > 0)
      {
        ViewBag.UserLeavePolicyId = policyId;
      }
      else
      {
        ViewBag.PolicyAlert = "No Policy is implemented for your account, Contact Admin";
      }
      //List<AspNetUser> Seniors = GetSeniorStaff();
      //ViewBag.LineManagers = new SelectList(Seniors, "Id", "UserName");
      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      var currentUser = db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
      //ViewBag.JoiningDate = currentUser.JoiningDate.Value.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
      ViewBag.JoiningDate = currentUser.JoiningDate.HasValue
            ? currentUser.JoiningDate.Value.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)
            : "Joining date not available";

      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", "");

      ViewBag.LeaveUserId = userId;
      //UserLeavePoliciesController UserLeavePolicies = new UserLeavePoliciesController();//.FileUploadMsgView("some string");
      //var result= UserLeavePolicies.Edit(7);
      //ViewBag.UserLeavePolicy= UserLeavePolicies.Edit(7);
      return View();
    }

    public List<AspNetUser> GetSeniorStaff()
    {
      List<AspNetUser> Seniors = new List<AspNetUser>();
      foreach (AspNetUser user in db.AspNetUsers.ToList<AspNetUser>())
      {
        foreach (AspNetRole role in user.AspNetRoles.ToList<AspNetRole>())
        {
          if (role.Name == "Admin" || role.Name == "Manager")
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
    public LeaveBalance CalculateLeaveBalance(ref Leave leave)
    {
      //check there is some weakend


      //check there is some anual leaves/public holidays


      //add sbubtract leave and add or update to leaveBalnce table


      List<int> weeklyOffDays = leave.AspNetUser.UserLeavePolicy.WeeklyOffDays.Split(',').Select(int.Parse).ToList();
      List<AnnualOffDay> AnnualOffDays = leave.AspNetUser.UserLeavePolicy.AnnualOffDays.ToList<AnnualOffDay>();

      double TotalOffDays = (leave.EndDate - leave.StartDate).TotalDays + 1;
      double NaturalOffDays = 0;
      bool found = false;
      for (DateTime DateIdx = leave.StartDate; DateIdx <= leave.EndDate; DateIdx = DateIdx.AddDays(1))
      {

        foreach (int d in weeklyOffDays)
        {
          if (d == (int)DateIdx.DayOfWeek)
          {
            //weekend off days
            NaturalOffDays += 1;
            found = true;
            break;
          }
        }
        //this condition is due to: example: if national holiday comes on sunday. then count it one day of. if this statement is not here it will conunt two days. which is wrong
        if (found == true) { found = false; continue; }

        for (int i = 0; i < AnnualOffDays.Count; i++)
        {
          if (DateIdx.Date.CompareTo(AnnualOffDays[i].OffDay) == 0)
          {
            //annual off days
            NaturalOffDays += 1;
            break;
          }
        }
      }
      leave.TotalDays = (int)(TotalOffDays - NaturalOffDays);
      string UserId = leave.UserId;
      int LeaveTypeId = leave.LeaveTypeId;
      //List<int> weeklyOffDays = leave.AspNetUser.UserLeavePolicy.WeeklyOffDays.Split(',').Select(int.Parse).ToList();

      LeaveBalance lb = leave.AspNetUser.LeaveBalances.FirstOrDefault(x => x.UserId == UserId && x.LeaveTypeId == LeaveTypeId);
      return lb;

    }
    public LeaveBalance CalculateLeaveBalanceQuota(ref Leave leave)
    {
      string UserId = leave.UserId;
      int LeaveTypeId = leave.LeaveTypeId;
      //List<int> weeklyOffDays = leave.AspNetUser.UserLeavePolicy.WeeklyOffDays.Split(',').Select(int.Parse).ToList();

      LeaveBalance lb = leave.AspNetUser.LeaveBalances.FirstOrDefault(x => x.UserId == UserId && x.LeaveTypeId == LeaveTypeId);
      return lb;

    }
    // POST: Leaves/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,LineManager1Id,LineManager2Id, UserLeavePolicyID")] Leave leave, string StartDate)
    {
      leave.UserId = User.Identity.GetUserId();
      leave.AspNetUser = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);
      leave.DateCreated = DateTime.Now;
      leave.IsQuotaRequest = false;
      leave.LeaveType = db.LeaveTypes.FirstOrDefault(x => x.Id == leave.LeaveTypeId);
      var shortLeaveMessage = "";
      leave.UserLeavePolicyID = leave.AspNetUser.UserLeavePolicyId;

      if (leave.LeaveTypeId == 7 && leave.StartDate.TimeOfDay.TotalSeconds != 0 && leave.EndDate.TimeOfDay.TotalSeconds != 0) //7 causal short leave
      {
        leave.IsShortLeave = true;

        var balanceCheck = db.LeaveBalances
          .Where(leaveBalance =>
            leaveBalance.UserLeavePolicyId == leave.AspNetUser.UserLeavePolicyId &&
            leaveBalance.UserId == leave.UserId &&
            leaveBalance.LeaveTypeId == 1)//sick casual leave
          .Select(detail => detail.Balance).FirstOrDefault();

        shortLeaveMessage = "Your Remannig Sick/Casual Leave Balance is" + balanceCheck;


        if (balanceCheck <= 0)
        {
          TempData["ErrorMessage"] = "Your leave request exceeds the available balance.";
          return PartialView("Error", "Shared");
        }

        if (balanceCheck == 1)
        {
          var hoursTakenCheck = db.LeaveBalances
              .Where(leaveBalance =>
                  leaveBalance.UserLeavePolicyId == leave.AspNetUser.UserLeavePolicyId &&
                  leaveBalance.UserId == leave.UserId &&
                  leaveBalance.LeaveTypeId == 7) // Casual Short Leave
              .Select(detail => detail.HoursTaken)
              .FirstOrDefault();

          if(hoursTakenCheck + (int)(leave.EndDate - leave.StartDate).TotalHours > 8)
          {
            TempData["ErrorMessage"] = "Your leave request exceeds the available balance.";
            return PartialView("Error", "Shared");
          }
        }
      }
      else
      {
        leave.IsShortLeave = false;
      }
       if (ModelState.IsValid)
      {
        //check leave Balance
        TimeSpan duration = leave.EndDate - leave.StartDate;
        int daysCount = duration.Days + 1; //total days including weekends
        var balanceCheck = db.LeaveBalances
          .Where(leaveBalance =>
            leaveBalance.UserLeavePolicyId == leave.UserLeavePolicyID &&
            leaveBalance.UserId == leave.UserId &&
            leaveBalance.LeaveTypeId == leave.LeaveTypeId)
          .Select(detail => detail.Balance).FirstOrDefault();

        if (balanceCheck == null)
        {
          var userPolicy = db.UserLeavePolicies.FirstOrDefault(x => x.Id == leave.AspNetUser.UserLeavePolicyId);
          DateTime fiscalStart = (DateTime)userPolicy.FiscalYearStart;
          DateTime fiscalEnd = (DateTime)userPolicy.FiscalYearEnd;
          int workedMonths = 12;
          if (leave.AspNetUser.JoiningDate.HasValue && leave.AspNetUser.JoiningDate > fiscalStart)
          {
            int joiningYear = leave.AspNetUser.JoiningDate?.Year ?? 0;
            int joiningMonth = leave.AspNetUser.JoiningDate?.Month ?? 0;

            workedMonths = ((fiscalEnd.Year - joiningYear) * 12) + fiscalEnd.Month - joiningMonth + 1;
            int? assignedLeaveQuota = leave.AspNetUser.UserLeavePolicyId != null
              ? db.UserLeavePolicyDetails
                  .Where(lb => lb.UserLeavePolicyId == leave.AspNetUser.UserLeavePolicyId &&
                               (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
                  .Select(lb => (int?)lb.Allowed)
                  .Sum() ?? 0
              : 0;
            // Prorated Leave Calculation
            double proratedLeaves = (workedMonths / 12.0) * (assignedLeaveQuota ?? 0);

            int finalProrated = proratedLeaves % 1 >= 0.5
                     ? (int)Math.Ceiling(proratedLeaves)
                     : (int)Math.Floor(proratedLeaves);

            // Split the prorated value into casual and annual
            int half = finalProrated / 2;
            int casualLeave = half + (finalProrated % 2 != 0 ? 1 : 0);
            int annualLeave = half;
            // Custom rounding logic

            if (leave.LeaveType.Id == 2)
            {
              balanceCheck = annualLeave;
            } 
            if (leave.LeaveType.Id == 1)
            {
              balanceCheck = casualLeave;
            }

            
          }
          else
          {
            balanceCheck = db.UserLeavePolicyDetails.Where(leaveDetail => leaveDetail.LeaveTypeId == leave.LeaveTypeId &&
            leaveDetail.UserLeavePolicyId == leave.AspNetUser.UserLeavePolicyId).Select(detail => detail.Allowed).FirstOrDefault();
          }
        }
        var daysForFinalCompare = 0;
        AspNetUser admin1 = null;
        if (leave.LeaveType.Id == 2) //2 is for annual leave. if this is selected then send leave request first to umar.nazir
        {
          int validDaysCount = Enumerable.Range(0, daysCount)
            .Select(offset => leave.StartDate.AddDays(offset))
            .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday); //total days excluding weekends

          
          if (leave.AspNetUser.CntryName == "Pakistan")
          {
            daysForFinalCompare = validDaysCount;
            leave.TotalDays = daysForFinalCompare;
          }
          else
          {
            daysForFinalCompare = daysCount;
            leave.TotalDays = daysForFinalCompare;
          }
          if (daysForFinalCompare <= balanceCheck && daysForFinalCompare > 0)
          { 
            //for annual leaves, LineManager1Id will be that of Annual Leave Manager

            var userDepartment = leave.AspNetUser.DepartmentName;

            var department = db.DepartmentNames.FirstOrDefault(d => d.Name == userDepartment);

            if (department != null && !string.IsNullOrEmpty(department.HRBPEmail))
            {
              // HRBP Email found, get user with that email
              admin1 = db.AspNetUsers.FirstOrDefault(u => u.Email == department.HRBPEmail);

              if (admin1 != null)
              {
                leave.LineManager1Id = admin1.Id;
              }
              else
              {
                // fallback to annual leave manager if HRBPEmail not matching any user
                admin1 = db.AspNetUsers.FirstOrDefault(user => user.BioStarEmpNum ==
                    db.AnnualLeaveManagers.FirstOrDefault().BioStarEmpNum);
                leave.LineManager1Id = admin1?.Id;
              }
            }
            else
            {
              // fallback if no HRBPEmail
              admin1 = db.AspNetUsers.FirstOrDefault(user => user.BioStarEmpNum ==
                  db.AnnualLeaveManagers.FirstOrDefault().BioStarEmpNum);
              leave.LineManager1Id = admin1?.Id;
            }


          }
          else
          {
            TempData["ErrorMessage"] = "Your leave request exceeds the available balance.";
            return PartialView("Error", "Shared");
          }
        }
        else
        {
          if (leave.LeaveType.Id != 10 && leave.LeaveType.Id != 9 && leave.LeaveType.Id != 7 && leave.LeaveType.Id != 8) // Casual Short Day, Official Short Day, Official Full Day, and Work From Home
          {
            if (leave.LeaveType.Id == 5) // Marriage Leave
            {
              int validDaysCount = Enumerable.Range(0, daysCount)
                  .Select(offset => leave.StartDate.AddDays(offset))
                  .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);
              admin1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
              leave.TotalDays = validDaysCount; // Set TotalDays for marriage leave
            }
           else if (leave.LeaveType.Id == 1) // Sick Leave
            {
              int validDaysCount = Enumerable.Range(0, daysCount)
                  .Select(offset => leave.StartDate.AddDays(offset))
                  .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);
              admin1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
              if (validDaysCount > balanceCheck)
              {
                TempData["ErrorMessage"] = "Your leave request exceeds the available balance.";
                return PartialView("Error", "Shared");
              }
              else
              {
                leave.TotalDays = validDaysCount; // Set TotalDays for Sick leave
              }
            }
            else if (daysCount <= balanceCheck)
            {
              admin1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
              leave.TotalDays = daysCount;
            }
            else
            {
              TempData["ErrorMessage"] = "Your leave request exceeds the available balance.";
              return PartialView("Error", "Shared");
            }
          }
          else
          {
            admin1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);
            leave.TotalDays = daysCount;
          }
        }
        
          try
          {
            ViewBag.BalanceCheck = balanceCheck;
            db.Leaves.Add(leave);
            await db.SaveChangesAsync();
            SendEmail.SendEmailUsingLeavON(leave,  leave.AspNetUser, receiver: admin1, MessageType: "LeaveRequest");
          }
          catch (Exception ex)
          {
            Console.WriteLine("exception occured", ex);
          }

        //AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
        //if (admin1.UserName != admin2.UserName) SendEmail.SendEmailUsingLeavON(leave, SendEmail.LeavON_Email, SendEmail.LeavON_Password, leave.AspNetUser, admin2, "LeaveRequest");
        return RedirectToAction("Index");
      }

      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);

      var itmes = db.AspNetUsers.Include(x => x.AspNetRoles.Select(rl => rl.Name)).ToList();

      ViewBag.LineManagers = new SelectList(db.AspNetUsers.OrderBy(x => x.UserName), "Id", "UserName");
      
      
      return View(leave);
    }
    // POST: Leaves/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateCompensatoryQuotaRequest([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,LineManager1Id,LineManager2Id")] Leave leave, string StartDate, int CompensatoryLeaveTypeId)
    {
      leave.UserId = User.Identity.GetUserId();
      leave.AspNetUser = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);
      leave.DateCreated = DateTime.Now;
      leave.LeaveTypeId = CompensatoryLeaveTypeId;
      leave.LeaveType = db.LeaveTypes.FirstOrDefault(x => x.Id == CompensatoryLeaveTypeId);
      leave.UserLeavePolicyID = leave.AspNetUser.UserLeavePolicyId;
      TimeSpan duration = leave.EndDate - leave.StartDate;

      leave.TotalDays = duration.Days + 1; //total days including weekends
      leave.IsQuotaRequest = true;

      if (ModelState.IsValid)
      {
        db.Leaves.Add(leave);
        ///////////////
        //LeaveBalance leaveBalance = CalculateLeaveBalanceQuota(ref leave);

        //if (leaveBalance == null)
        //{
        //  //new
        //  leaveBalance = new LeaveBalance(ref leave);

        //  leaveBalance.UserId = leave.UserId;
        //  leaveBalance.LeaveTypeId = leave.LeaveTypeId;
        //  //leaveBalance.UserLeavePolicyId = leave.AspNetUser.UserLeavePolicyId;
        //  db.LeaveBalances.Add(leaveBalance);
        //}
        //else
        //{
        //  leaveBalance.Balance += leave.TotalDays;
        //  db.Entry(leaveBalance).State = EntityState.Modified;
        //}
        ///////////////

        await db.SaveChangesAsync();

        AspNetUser admin1 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager1Id);

        SendEmail.SendEmailUsingLeavON(leave,   leave.AspNetUser, admin1, "LeaveRequest");

        //AspNetUser admin2 = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.LineManager2Id);
        //SendEmail.SendEmailUsingLeavON(leave, SendEmail.LeavON_Email, SendEmail.LeavON_Password, leave.AspNetUser, admin2, "LeaveRequest");
        return RedirectToAction("QuotaRequestHistory");
      }

      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);


      var itmes = db.AspNetUsers.Include(x => x.AspNetRoles.Select(rl => rl.Name)).ToList();

      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", "7baffeb6-7cad-46ad-9418-493d86e1da75");

      return View(leave);
    }

    // GET: Leaves/Edit/5
    public async Task<ActionResult> Edit(decimal id)
    {
      return RedirectToAction("Index");

      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      Leave leave = await db.Leaves.FindAsync(id);
      if (leave == null)
      {
        return HttpNotFound();
      }

      if (!(leave.IsAccepted1 == null || leave.IsAccepted2 == null))
      {
        return RedirectToAction("Index");
      }
      List<AspNetUser> Seniors = GetSeniorStaff();

      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", "7baffeb6-7cad-46ad-9418-493d86e1da75");

      string userId = User.Identity.GetUserId();
      int policyId = db.AspNetUsers.FirstOrDefault(x => x.Id == userId).UserLeavePolicyId.GetValueOrDefault();
      //List<UserLeavePolicyDetail> customLeaveTypes = db.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == policyId).ToList<UserLeavePolicyDetail>();
      //ViewBag.LeaveTypeId = new SelectList(customLeaveTypes, "Id", "Name");
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes.Where(x => x.UserLeavePolicyDetails.Any(y => y.UserLeavePolicyId == policyId) || x.Id == Consts.CompensatoryLeaveTypeId), "Id", "Name");
      if (policyId > 0)
      {
        ViewBag.UserLeavePolicyId = policyId;
      }
      else
      {
        ViewBag.PolicyAlert = "No Policy is implemented for your account, Contact Admin";
      }

      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      return View(leave);
    }
    //GET
    public async Task<ActionResult> EditCompensatoryQuotaRequest(decimal id)
    {
      return RedirectToAction("Index");
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      Leave leave = await db.Leaves.FindAsync(id);
      if (leave == null)
      {
        return HttpNotFound();
      }
      if (!(leave.IsAccepted1 == null || leave.IsAccepted2 == null))
      {
        return RedirectToAction("Index");
      }
      List<AspNetUser> Seniors = GetSeniorStaff();
      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", "7baffeb6-7cad-46ad-9418-493d86e1da75");
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      string userId = User.Identity.GetUserId();
      int policyId = db.AspNetUsers.FirstOrDefault(x => x.Id == userId).UserLeavePolicyId.GetValueOrDefault();
      return View(leave);
    }

    // POST: Leaves/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,ResponseDate1,ResponseDate2,IsAccepted1,IsAccepted2,LineManager1Id,LineManager2Id,Remarks1,Remarks2,DateCreated,DateModified,UserLeavePolicyId")] Leave leave, decimal LastLeaveDaysCount)
    {
      return RedirectToAction("Index");
      leave.UserId = User.Identity.GetUserId();
      leave.AspNetUser = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);
      leave.DateModified = DateTime.Now;
      if (ModelState.IsValid)
      {
        db.Entry(leave).State = EntityState.Modified;
        ///////////////
        LeaveBalance leaveBalance = CalculateLeaveBalance(ref leave);

        if (leaveBalance == null)
        {
          //new
          //leaveBalance = new LeaveBalance(ref leave);
          //leaveBalance.Taken = leave.TotalDays;
          //leaveBalance.Balance -= leave.TotalDays;
          //leaveBalance.UserId = leave.UserId;
          //leaveBalance.LeaveTypeId = leave.LeaveTypeId;
          //leaveBalance.UserLeavePolicyId = leave.AspNetUser.UserLeavePolicyId;
          //db.LeaveBalances.Add(leaveBalance);
        }
        else
        {
          //first reseting Balance to its correct state
          leaveBalance.Balance = leaveBalance.Balance + LastLeaveDaysCount;//leave.TotalDays;
          decimal Allowed = (decimal)leaveBalance.UserLeavePolicy.UserLeavePolicyDetails.FirstOrDefault(x => x.UserLeavePolicyId == leave.AspNetUser.UserLeavePolicyId && x.LeaveTypeId == leave.LeaveTypeId).Allowed;
          leaveBalance.Taken = Allowed- leaveBalance.Balance;
          /////////////
          //old
          leaveBalance.Taken += leave.TotalDays;
          leaveBalance.Balance -= leave.TotalDays;
          db.Entry(leaveBalance).State = EntityState.Modified;
        }
        ///////////////
        await db.SaveChangesAsync();
        return RedirectToAction("Index");
      }
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      return View(leave);
    }

    // POST: Leaves/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditCompensatoryQuotaRequest([Bind(Include = "Id,UserId,LeaveTypeId,Reason,StartDate,EndDate,TotalDays,EmergencyContact,ResponseDate1,ResponseDate2,IsAccepted1,IsAccepted2,LineManager1Id,LineManager2Id,Remarks1,Remarks2,DateCreated,DateModified,UserLeavePolicyId")] Leave leave, decimal LastLeaveDaysCount)
    {
      return RedirectToAction("Index");
      leave.UserId = User.Identity.GetUserId();
      leave.AspNetUser = db.AspNetUsers.FirstOrDefault(x => x.Id == leave.UserId);
      leave.DateModified = DateTime.Now;
      leave.IsQuotaRequest = true;
      if (ModelState.IsValid)
      {
        db.Entry(leave).State = EntityState.Modified;
        ///////////////
        LeaveBalance leaveBalance = CalculateLeaveBalanceQuota(ref leave);


        if (leaveBalance == null)
        {
          //new
          //leaveBalance = new LeaveBalance(ref leave);
          //leaveBalance.Taken = leave.TotalDays;
          //leaveBalance.Balance -= leave.TotalDays;
          //leaveBalance.UserId = leave.UserId;
          //leaveBalance.LeaveTypeId = leave.LeaveTypeId;
          //leaveBalance.UserLeavePolicyId = leave.AspNetUser.UserLeavePolicyId;
          //db.LeaveBalances.Add(leaveBalance);
        }
        else
        {
          //first reseting Balance to its correct state
          leaveBalance.Balance -=  LastLeaveDaysCount;//leave.TotalDays;
          leaveBalance.Balance += leave.TotalDays;
          db.Entry(leaveBalance).State = EntityState.Modified;
        }
        ///////////////
        await db.SaveChangesAsync();
        return RedirectToAction("QuotaRequestHistory");
      }
      ViewBag.LeaveTypeId = new SelectList(db.LeaveTypes, "Id", "Name", leave.LeaveTypeId);
      //ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "UserId", leave.UserLeavePolicyId);
      return View(leave);
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
  }
}
