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
using LeaveON.UtilityClasses;
using System.Globalization;
using LeaveON.Models;
using LeaveON.Models.DatatableVmModel;

namespace LeaveON.Controllers
{

  [Authorize(Roles = "Admin,Manager")]
  public class AspNetUsersController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: AspNetUsers
    public ActionResult Index()
    {
      return View();
    }

    [HttpPost]
    public async Task<JsonResult> GetUsers(DataTableRequest request)
    {
      var query = db.AspNetUsers
          .Where(x => x.IsDeleted != true &&
                      x.BioStarEmpNum > 0);

      // Total records
      var recordsTotal = await query.CountAsync();

      // Search
      if (request.Search != null &&
          !string.IsNullOrWhiteSpace(request.Search.Value))
      {
        var search = request.Search.Value.Trim();

        query = query.Where(x =>
            x.UserName.Contains(search) ||
            x.ManagerID.Contains(search) ||
            x.ManagerName.Contains(search) ||
            x.Manager2Name.Contains(search) ||
            x.DepartmentName.Contains(search) ||
            x.CntryName.Contains(search) ||
            x.CntryNameTemp.Contains(search) ||
            x.Remarks.Contains(search)
        );
      }

      // Filtered records
      var recordsFiltered = await query.CountAsync();

      // Sorting
      if (request.Order != null && request.Order.Count > 0)
      {
        var order = request.Order[0];

        switch (order.Column)
        {
          case 0:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.BioStarEmpNum)
                : query.OrderBy(x => x.BioStarEmpNum);
            break;

          case 1:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.UserName)
                : query.OrderBy(x => x.UserName);
            break;

          case 2:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.ManagerName)
                : query.OrderBy(x => x.ManagerName);
            break;

          case 3:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.ManagerName)
                : query.OrderBy(x => x.ManagerName);
            break;

          case 4:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.Manager2Name)
                : query.OrderBy(x => x.Manager2Name);
            break;

          case 5:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.DepartmentName)
                : query.OrderBy(x => x.DepartmentName);
            break;

          case 6:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.CntryName)
                : query.OrderBy(x => x.CntryName);
            break;

          case 7:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.CntryNameTemp)
                : query.OrderBy(x => x.CntryNameTemp);
            break;

          case 8:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.DateCreated)
                : query.OrderBy(x => x.DateCreated);
            break;

          case 9:
            query = order.Dir == "desc"
                ? query.OrderByDescending(x => x.DateModified)
                : query.OrderBy(x => x.DateModified);
            break;

          default:
            query = query.OrderBy(x => x.UserName);
            break;
        }
      }
      else
      {
        query = query.OrderBy(x => x.UserName);
      }

      // Paging
      var users = await query
          .Skip(request.Start)
          .Take(request.Length)
          .Select(x => new UserDataTableModel
          {
            Id = x.Id,
            BioStarEmpNum = x.BioStarEmpNum,
            UserName = x.UserName,
            ManagerID = x.ManagerID,
            ManagerName = x.ManagerName,
            Manager2Name = x.Manager2Name,
            DepartmentName = x.DepartmentName,
            CntryName = x.CntryName,
            CntryNameTemp = x.CntryNameTemp,

            LeavePolicy = x.UserLeavePolicy != null
                  ? x.UserLeavePolicy.Description
                  : "",

            DateCreated = x.DateCreated,
            DateModified = x.DateModified,
            Remarks = x.Remarks
          })
          .ToListAsync();
      //return Json(new
      //{
      //  draw = request.Draw,
      //  recordsTotal = recordsTotal,
      //  recordsFiltered = recordsFiltered,
      //  data = users
      //}, JsonRequestBehavior.AllowGet);

      return Json(new DataTableResponse<UserDataTableModel>
      {
        draw = request.Draw,
        recordsTotal = recordsTotal,
        recordsFiltered = recordsFiltered,
        data = users
      });
    }

    // GET: AspNetUsers/Details/5
    public async Task<ActionResult> Details(string id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(id);
      if (aspNetUser == null)
      {
        return HttpNotFound();
      }
      return View(aspNetUser);
    }

    // GET: AspNetUsers/Create
    public ActionResult Create()
    {
      ViewBag.DepartmentId = new SelectList(db.DepartmentNames, "Id", "Name");
      return View();
    }

    // POST: AspNetUsers/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([Bind(Include = "Id,Hometown,Email,EmailConfirmed,PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName,DateCreated,DateModified,Remarks,DepartmentName, ManagerID, ManagerName, ManagerEmail, Manager2ID, Manager2Name, Manager2Email")] AspNetUser aspNetUser)
    {
      if (ModelState.IsValid)
      {
        db.AspNetUsers.Add(aspNetUser);
        await db.SaveChangesAsync();
        return RedirectToAction("Index");
      }

      //ViewBag.DepartmentId = new SelectList(db.DepartmentNames, "Id", "Name", aspNetUser.DepartmentId);
      return View(aspNetUser);
    }

    // GET: AspNetUsers/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
      UserModel model = new UserModel();
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(id);
      BindUserModelData(aspNetUser,model);
      tblHRBP hrbp = db.tblHRBPs.Where(j => j.UserID == id).FirstOrDefault();
      var userlist = db.AspNetUsers
     .Where(u => u.Email != null && u.IsActive == true).AsEnumerable();

      var hrbplist = db.tblHRBPs.Where(u => u.IsActive == true).AsEnumerable();
      var hrbpEmails = hrbplist.Select(u => new SelectListItem { Value = u.ID.ToString(), Text = u.HRBPName }).ToList();
      if (aspNetUser.HRBPID.HasValue)
      {
        ViewBag.HRBPEmailList = new SelectList(hrbpEmails, "Value", "Text", aspNetUser.HRBPID.ToString());
      }
      else
      {
        ViewBag.HRBPEmailList = new SelectList(hrbpEmails, "Value", "Text", "Select HRBP");
      }

      if (aspNetUser == null)
      {
        return HttpNotFound();
      }

      var genderList = new List<SelectListItem>
      {
        new SelectListItem { Value = "true", Text = "Male" },
        new SelectListItem { Value = "false", Text = "Female" }
      };

      // Check if the Gender value is not null and assign it to the ViewBag
      if (aspNetUser.Gender != null)
      {
        ViewBag.Gender = new SelectList(genderList, "Value", "Text", aspNetUser.Gender.ToString());
      }
      else
      {
        ViewBag.Gender = new SelectList(genderList, "Value", "Text", "Select Gender");
      }
      // aspNetUser.RoleId = aspNetUser.AspNetRoles.ToList()[0].Id;

      ViewBag.CountryNames = new SelectList(db.CountryNames, "Name", "Name", aspNetUser.CountryName);
      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      var currentEditUser = aspNetUser.UserName.Split('@')[0].Replace('.', ' ').ToLower();
      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName.ToLower() != currentEditUser)
        .OrderBy(x => x.UserName), "Id", "UserName", null);
      ViewBag.Departments = new SelectList(db.DepartmentNames.OrderBy(x => x.Name), "Name", "Name");
      ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "Description", aspNetUser.UserLeavePolicyId);
      //   ViewBag.Role = new SelectList(db.AspNetRoles.ToHashSet(), "Id", "Name", aspNetUser.RoleId);


      Int32 currentPolycyID = aspNetUser.UserLeavePolicyId.HasValue ? aspNetUser.UserLeavePolicyId.Value : 0;

      if (currentPolycyID > 0)
      {


        List<LeaveBalance> balance = db.LeaveBalances
      .Where(k => k.UserId == aspNetUser.Id
               && k.LeaveTypeId == 2 && k.UserLeavePolicyId == currentPolycyID).ToList();

        if (balance != null && balance.Count() > 0)
        {
          ViewBag.UserAnnualLeaveBalance = balance.Sum(k => k.Balance);
        }
        else
        {
          var policy = db.UserLeavePolicyDetails.FirstOrDefault(l => l.UserLeavePolicyId == currentPolycyID && l.LeaveTypeId == 2);
          if (policy != null)
          {
            ViewBag.UserAnnualLeaveBalance = policy.Allowed;
          }
        }


      }
      return View(model);
    }

    public void BindUserModelData(AspNetUser user, UserModel model)
    {
      model.Id = user.Id;




      model.Hometown = user.Hometown;
      model.Email = user.Email;
      model.EmailConfirmed = user.EmailConfirmed;
      model.PasswordHash = user.PasswordHash;
      model.SecurityStamp = user.SecurityStamp;
      model.PhoneNumber = user.PhoneNumber;
      model.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
      model.TwoFactorEnabled = user.TwoFactorEnabled;
      model.LockoutEndDateUtc = user.LockoutEndDateUtc;
      model.LockoutEnabled = user.LockoutEnabled;
      model.AccessFailedCount = user.AccessFailedCount;
      model.UserName = user.UserName;
      model.BioStarEmpNum = user.BioStarEmpNum;
      model.UserLeavePolicyId = user.UserLeavePolicyId;
      model.Remarks = user.Remarks;
      model.DepartmentName = user.DepartmentName;
      model.CntryName = user.CntryName;
      model.CntryNameTemp = user.CntryNameTemp;
      model.IsRelocated = user.IsRelocated;
      model.EmpolyeeName = user.EmpolyeeName;

      model.Gender = user.Gender;
      model.ManagerID = user.ManagerID;
      model.ManagerName = user.ManagerName;
      model.ManagerEmail = user.ManagerEmail;
      model.Manager2ID = user.Manager2ID;
      model.Manager2Name = user.Manager2Name;
      model.Manager2Email = user.Manager2Email;
      model.HRBPID = user.HRBPID;



    }

    // POST: AspNetUsers/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,Hometown,Email,EmailConfirmed, Gender, PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName,DateCreated,DateModified,Remarks,DepartmentName,CountryId,UserLeavePolicyId,BioStarEmpNum,CntryName,CntryNameTemp,IsRelocated, ManagerID, ManagerName, ManagerEmail, Manager2ID, Manager2Name, Manager2Email,HRBPID")] UserModel aspNetUser)
    {
      aspNetUser.DateModified = DateTime.Now;

      AspNetUser olduser = db.AspNetUsers.Where(u => u.Id == aspNetUser.Id).FirstOrDefault();

      var managerEmail = db.AspNetUsers
                           .Where(u => u.Id == aspNetUser.ManagerID)
                           .Select(u => u.UserName)
                           .FirstOrDefault();

      var manager2Email = db.AspNetUsers
                     .Where(u => u.Id == aspNetUser.Manager2ID)
                     .Select(u => u.UserName)
                     .FirstOrDefault();
      aspNetUser.ManagerEmail = managerEmail;
      aspNetUser.Manager2Email = manager2Email;


      if (!string.IsNullOrEmpty(managerEmail))
      {
        var namePart = managerEmail.Split('@')[0].Replace('.', ' ');
        aspNetUser.ManagerName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(namePart);
      }
      else
      {
        aspNetUser.ManagerName = "No Manager";
      }
      if (!string.IsNullOrEmpty(manager2Email))
      {
        var namePart = manager2Email.Split('@')[0].Replace('.', ' ');
        aspNetUser.Manager2Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(namePart);
      }
      else
      {
        aspNetUser.Manager2Name = "No Manager";
      }



       

       
      if (ModelState.IsValid)
      {
        //db.AspNetUsers.Attach(aspNetUser);
        olduser.DateModified      = aspNetUser.DateModified     ;
        olduser.Remarks           = aspNetUser.Remarks          ;
        olduser.Gender            = aspNetUser.Gender           ;
        olduser.ManagerID         = aspNetUser.ManagerID        ;
        olduser.ManagerName       = aspNetUser.ManagerName      ;
        olduser.ManagerEmail      = aspNetUser.ManagerEmail     ;
        olduser.Manager2ID        = aspNetUser.Manager2ID       ;
        olduser.Manager2Name      = aspNetUser.Manager2Name     ;
        olduser.Manager2Email     = aspNetUser.Manager2Email    ;
        olduser.DepartmentName    = aspNetUser.DepartmentName   ;
        olduser.UserLeavePolicyId = aspNetUser.UserLeavePolicyId;
        olduser.CntryNameTemp     = aspNetUser.CntryNameTemp    ;
        olduser.IsRelocated       = aspNetUser.IsRelocated;
        olduser.HRBPID = aspNetUser.HRBPID;


        try
        {
          await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
          Console.WriteLine("Hello", ex);
        }
        await Utility.AdjustLeaveBalance((decimal)aspNetUser.UserLeavePolicyId);
        return RedirectToAction("Index");
      }
      return View(aspNetUser);
    }

    // GET: AspNetUsers/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(id);
      if (aspNetUser == null)
      {
        return HttpNotFound();
      }
      return View(aspNetUser);
    }

    // POST: AspNetUsers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirmed(string id)
    {
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(id);
      db.AspNetUsers.Remove(aspNetUser);
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
    [HttpPost]
    public async Task<JsonResult> SaveCarryAmount(string userId, decimal carryAmount)
    {
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(userId);

      if (aspNetUser == null)
      {
        return Json(new
        {
          success = false,
          message = "User not found."
        });
      }
      Int32 currentPolycyID = aspNetUser.UserLeavePolicyId.HasValue ? aspNetUser.UserLeavePolicyId.Value : 0;

      if (currentPolycyID > 0)
      {
        LeaveBalance balance = new LeaveBalance();

        balance = await db.LeaveBalances
      .FirstOrDefaultAsync(k => k.UserLeavePolicyId == currentPolycyID && k.UserId == aspNetUser.Id
               && k.LeaveTypeId == 2);
        if (balance != null)
        {
          balance.AnnualAmountAjustmesnt += carryAmount;
          balance.Balance += carryAmount;
          balance.AnnualAmountAjustmesntModifyDate = DateTime.Now;

        }
        else
        {
          var detail = await db.UserLeavePolicyDetails.FirstOrDefaultAsync(k => k.UserLeavePolicyId == currentPolycyID && k.LeaveTypeId == 2);
          if (detail != null)
          {
            balance = new LeaveBalance();
            balance.Balance = detail.Allowed + carryAmount;
            balance.UserLeavePolicyId = currentPolycyID;
            balance.UserId = aspNetUser.Id;
            balance.AnnualAmountAjustmesnt = carryAmount;
            balance.AnnualAmountAjustmesntModifyDate = DateTime.Now;
            balance.Taken = 0;
            balance.LeaveTypeId = 2;// annual typedb

          }
          else
          {
            balance = new LeaveBalance();
            balance.Balance = carryAmount;
            balance.UserLeavePolicyId = currentPolycyID;
            balance.UserId = aspNetUser.Id;
            balance.AnnualAmountAjustmesnt = carryAmount;
            balance.AnnualAmountAjustmesntModifyDate = DateTime.Now;
            balance.Taken = 0;
            balance.LeaveTypeId = 2;// annual typedb
          }
          db.LeaveBalances.Add(balance);
        }


      }

      await db.SaveChangesAsync();

      return Json(new
      {
        success = true,
        message = "Annual leave balance saved successfully."
      });
    }
  }
}
