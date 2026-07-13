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

namespace LeaveON.Controllers
{
  [Authorize(Roles = "Admin")]
  public class DepartmentNamesController : Controller
    {
        private LeaveONEntities db = new LeaveONEntities();

        // GET: DepartmentNames
        public async Task<ActionResult> Index()
        {
            return View(await db.DepartmentNames.ToListAsync());
        }

        // GET: DepartmentNames/Details/5
        public async Task<ActionResult> Details(decimal id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepartmentName departmentName = await db.DepartmentNames.FindAsync(id);
            if (departmentName == null)
            {
                return HttpNotFound();
            }
            return View(departmentName);
        }

        // GET: DepartmentNames/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DepartmentNames/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name")] DepartmentName departmentName)
        {
            if (ModelState.IsValid)
            {
                db.DepartmentNames.Add(departmentName);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(departmentName);
        }

        // GET: DepartmentNames/Edit/5
        public async Task<ActionResult> Edit(decimal id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepartmentName departmentName = await db.DepartmentNames.FindAsync(id);
            if (departmentName == null)
            {
                return HttpNotFound();
            }
      var userlist = db.AspNetUsers
      .Where(u => u.Email != null && u.IsActive == true).AsEnumerable();

          // Fetch available HRBP emails from AspNetUsers
      var hrbpEmails = userlist
          .Select(u => new SelectListItem
          {
            Value = u.Email,
            Text = u.EmpolyeeName,//System.Globalization.CultureInfo.CurrentCulture.TextInfo
                        //.ToTitleCase(u.Email.Split('@')[0].Replace(".", " "))
          })
          .ToList();

        var departmenUser= userlist.Where(k => k.DepartmentName.Trim().ToLower() == departmentName.Name.Trim().ToLower()).Select(u => new SelectListItem
        {
          Value = u.BioStarEmpNum.ToString(),
          Text = u.EmpolyeeName,//System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                //.ToTitleCase(u.Email.Split('@')[0].Replace(".", " "))
        })
          .ToList();  


      // Pass list to ViewBag
      ViewBag.HRBPEmailList = new SelectList(hrbpEmails, "Value", "Text", departmentName.HRBPEmail);
       ViewBag.departmenUserList = new SelectList(departmenUser, "Value", "Text", departmentName.HODID.ToString());

          return View(departmentName);
        }

        // POST: DepartmentNames/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,HRBPEmail,HODID")] DepartmentName departmentName)
        {
            if (ModelState.IsValid)
            {
                var user = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Email == departmentName.HRBPEmail);

                if (user != null)
                {
                  // Assuming HRBPBiostarEmpID is a column in DepartmentName table
                  departmentName.HRBPBiostarEmpNum = user.BioStarEmpNum; 
                  //departmentName.HODID = user.HODID; 
                }
       // departmentName.Id = 0;

         //db.DepartmentNames.Add(departmentName);

               // db.Entry(departmentName).State = EntityState.Modified;
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                    return View(departmentName);
                }

        // GET: DepartmentNames/Delete/5
        public async Task<ActionResult> Delete(decimal id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepartmentName departmentName = await db.DepartmentNames.FindAsync(id);
            if (departmentName == null)
            {
                return HttpNotFound();
            }
            return View(departmentName);
        }

        // POST: DepartmentNames/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(decimal id)
        {
            DepartmentName departmentName = await db.DepartmentNames.FindAsync(id);
            db.DepartmentNames.Remove(departmentName);
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
