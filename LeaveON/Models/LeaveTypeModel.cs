using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LeaveON.Models
{
  public class LeaveTypeModel
  {
   
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }

    [Required]
    public List<int> CountryIDs { get; set; }

    public int? HrBPID { get; set; }
    [Required]
    public bool IsCalenderDays { get; set; }

  }

  
}
