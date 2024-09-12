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

namespace LeaveON.Controllers
{

  [Authorize(Roles = "Admin,Manager")]
  public class AspNetUsersController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: AspNetUsers
    public async Task<ActionResult> Index()
    {
      var aspNetUsers = db.AspNetUsers;//.Include(a => a.Department);
      return View(await aspNetUsers.ToListAsync());
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
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      AspNetUser aspNetUser = await db.AspNetUsers.FindAsync(id);
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
        .OrderBy(x => x.UserName), "Id", "UserName", "7baffeb6-7cad-46ad-9418-493d86e1da75");
      ViewBag.Departments = new SelectList(db.DepartmentNames.OrderBy(x => x.Name), "Name", "Name");
      ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "Description", aspNetUser.UserLeavePolicyId);
   //   ViewBag.Role = new SelectList(db.AspNetRoles.ToHashSet(), "Id", "Name", aspNetUser.RoleId);
       
      return View(aspNetUser);
    }

    // POST: AspNetUsers/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,Hometown,Email,EmailConfirmed, Gender, PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName,DateCreated,DateModified,Remarks,DepartmentName,CountryId,UserLeavePolicyId,BioStarEmpNum,CntryName,CntryNameTemp,IsRelocated, ManagerID, ManagerName, ManagerEmail, Manager2ID, Manager2Name, Manager2Email")] AspNetUser aspNetUser)
    {
      aspNetUser.DateModified = DateTime.Now;


     
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
      // Ensure Manager 1 exists
      if (!string.IsNullOrEmpty(aspNetUser.ManagerID))
      {
        var managerExists = db.Managers.Any(m => m.UserID == aspNetUser.ManagerID);
        if (!managerExists)
        {
          var newManager = new Manager
          {
            UserID = aspNetUser.ManagerID,
            UserName = aspNetUser.ManagerName,
            Email = aspNetUser.ManagerEmail
          };
          db.Managers.Add(newManager);
        }
      }

      // Ensure Manager 2 exists
      if (!string.IsNullOrEmpty(aspNetUser.Manager2ID))
      {
        var manager2Exists = db.Managers.Any(m => m.UserID == aspNetUser.Manager2ID);
        if (!manager2Exists)
        {
          var newManager2 = new Manager
          {
            UserID = aspNetUser.Manager2ID,
            UserName = aspNetUser.Manager2Name,
            Email = aspNetUser.Manager2Email
          };
          db.Managers.Add(newManager2);
        }
      }
      if (ModelState.IsValid)
      {
        db.AspNetUsers.Attach(aspNetUser);
        db.Entry(aspNetUser).Property(x => x.DateModified).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Remarks).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Gender).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.ManagerID).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.ManagerName).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.ManagerEmail).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Manager2ID).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Manager2Name).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Manager2Email).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.DepartmentName).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.UserLeavePolicyId).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.CntryNameTemp).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.IsRelocated).IsModified = true;

        
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
  }
}
