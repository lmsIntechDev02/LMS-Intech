using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models.DatatableVmModel
{
  public class DataTableRequest
  {
    public int Draw { get; set; }

    public int Start { get; set; }

    public int Length { get; set; }

    public DataTableSearch Search { get; set; }

    public List<DataTableOrder> Order { get; set; }

    public List<DataTableColumn> Columns { get; set; }
  }


  public class DataTableSearch
  {
    public string Value { get; set; }

    public bool Regex { get; set; }
  }

  public class DataTableOrder
  {
    public int Column { get; set; }

    public string Dir { get; set; }
  }

  public class DataTableColumn
  {
    public string Data { get; set; }

    public string Name { get; set; }

    public bool Searchable { get; set; }

    public bool Orderable { get; set; }

    public DataTableSearch Search { get; set; }
  }
  public class DataTableResponse<T>
  {
    public int draw { get; set; }

    public int recordsTotal { get; set; }

    public int recordsFiltered { get; set; }

    public IEnumerable<T> data { get; set; }
  }
}
