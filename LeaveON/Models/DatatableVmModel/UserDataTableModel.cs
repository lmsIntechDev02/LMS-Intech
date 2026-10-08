using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models.DatatableVmModel
{
  public class UserDataTableModel
  {
    public string Id { get; set; }

    public int? BioStarEmpNum { get; set; }

    public string UserName { get; set; }

    public string ManagerName { get; set; }
    public string ManagerID { get; set; }

    public string Manager2Name { get; set; }

    public string DepartmentName { get; set; }

    public string CntryName { get; set; }

    public string CntryNameTemp { get; set; }

    public string LeavePolicy { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public string Remarks { get; set; }
  }
}
