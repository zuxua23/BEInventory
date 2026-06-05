namespace InventoryControl.Models;

public class DataTablesModel{
    public int Draw { get; set; } // A counter to identify the draw for the request
    public int Start { get; set; } // The starting point for pagination
    public int Length { get; set; } // The number of records to fetch
    public SearchInfo Search { get; set; } // Information about global search
    public List<OrderInfo> Order { get; set; } // Sorting details
    public List<ColumnInfo> Columns { get; set; } // Column metadata
}

public class SearchInfo
{
    public string Value { get; set; } // Search value entered by the user
    public bool Regex { get; set; } // Whether the search is a regex (usually false)
}

public class OrderInfo
{
    public int Column { get; set; } // The index of the column being sorted
    public string Dir { get; set; } // Sorting direction: "asc" or "desc"
}

public class ColumnInfo
{
    public string Data { get; set; } // The data field associated with the column
    public string Name { get; set; } // The name of the column (optional)
    public bool Searchable { get; set; } // Whether the column is searchable
    public bool Orderable { get; set; } // Whether the column is orderable
    public SearchInfo Search { get; set; } // Column-specific search info
}