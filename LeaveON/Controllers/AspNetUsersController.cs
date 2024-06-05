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
    public async Task<ActionResult> Create([Bind(Include = "Id,Hometown,Email,EmailConfirmed,PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName,DateCreated,DateModified,Remarks,DepartmentName, ManagerID, ManagerName")] AspNetUser aspNetUser)
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

      ViewBag.CountryNames = new SelectList(db.CountryNames, "Name", "Name", aspNetUser.CountryName);
      ViewBag.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name.Substring(0, User.Identity.Name.IndexOf('@')).Replace(".", " "));//"LoggedIn User";
      ViewBag.LineManagers = new SelectList(Utility.AspNetUserNames.Where(y => y.UserName != ViewBag.UserName)
        .OrderBy(x => x.UserName), "Id", "UserName", "7baffeb6-7cad-46ad-9418-493d86e1da75");
      ViewBag.Departments = new SelectList(db.DepartmentNames.OrderBy(x => x.Name), "Name", "Name");
      ViewBag.UserLeavePolicyId = new SelectList(db.UserLeavePolicies, "Id", "Description", aspNetUser.UserLeavePolicyId);
      return View(aspNetUser);
    }

    // POST: AspNetUsers/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,Hometown,Email,EmailConfirmed, Gender, PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName,DateCreated,DateModified,Remarks,DepartmentName,CountryId,UserLeavePolicyId,BioStarEmpNum,CntryName,CntryNameTemp,IsRelocated, ManagerID, ManagerName")] AspNetUser aspNetUser)
    {
      aspNetUser.DateModified = DateTime.Now;
      var managerEmail = db.AspNetUsers
                           .Where(u => u.Id == aspNetUser.ManagerID)
                           .Select(u => u.UserName)
                           .FirstOrDefault();

      // Check if the managerEmail is not null, then extract and format the name part
      if (!string.IsNullOrEmpty(managerEmail))
      {
        // Extract the name part before the '@' and replace '.' with ' '
        var namePart = managerEmail.Split('@')[0].Replace('.', ' ');

        // Use CultureInfo to properly capitalize the first letter of each name part
        aspNetUser.ManagerName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(namePart);
      }
      else
      {
        // If no managerEmail is found, default to "No Manager"
        aspNetUser.ManagerName = "No Manager";
      }
      if (ModelState.IsValid)
      {
        db.AspNetUsers.Attach(aspNetUser);
        db.Entry(aspNetUser).Property(x => x.DateModified).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Remarks).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.Gender).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.ManagerID).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.DepartmentName).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.ManagerName).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.UserLeavePolicyId).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.CntryNameTemp).IsModified = true;
        db.Entry(aspNetUser).Property(x => x.IsRelocated).IsModified = true;
        await db.SaveChangesAsync();
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
