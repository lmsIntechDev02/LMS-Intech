using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models
{
  public class DepartmentModel
  {
    public decimal Id { get; set; }
    public string Name { get; set; }
    
    public string HRBPName { get; set; }
    public string HODName { get; set; }
  }
}
