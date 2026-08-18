using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models
{
  public class UserModel
  {

    public string Id { get; set; }
    public Nullable<int> BioStarEmpNum { get; set; }
    public string Hometown { get; set; }
    public string Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; }
    public string SecurityStamp { get; set; }
    public string PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public Nullable<System.DateTime> LockoutEndDateUtc { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public string UserName { get; set; }
    public Nullable<System.DateTime> DateCreated { get; set; }
    public Nullable<System.DateTime> DateModified { get; set; }
    public string Remarks { get; set; }
    public Nullable<int> UserLeavePolicyId { get; set; }
    public Nullable<bool> IsActive { get; set; }
    public string DepartmentName { get; set; }
    public string CntryName { get; set; }
    public string CntryNameTemp { get; set; }
    public bool IsRelocated { get; set; }
    public string EmpolyeeName { get; set; }
    public string HRBPID { get; set; }
    public bool Gender { get; set; }
    public string ManagerID { get; set; }
    public string ManagerName { get; set; }
    public string ManagerEmail { get; set; }
    public string Manager2ID { get; set; }
    public string Manager2Name { get; set; }
    public string Manager2Email { get; set; }
    public Nullable<System.DateTime> JoiningDate { get; set; }
    public Nullable<bool> IsDeleted { get; set; }
    public Nullable<bool> IsNew { get; set; }

   
    public virtual Repository.Models.CountryName CountryName { get; set; }
    
  }
}
