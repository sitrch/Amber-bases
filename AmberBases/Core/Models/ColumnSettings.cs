using System.Collections.Generic;

namespace AmberBases.Core.Models;

public class ColumnConfig
{
    public string Name { get; set; }
    public double WidthPercent { get; set; }
}

public class ColumnConfigList
{
    public List<ColumnConfig> Columns { get; set; } = new List<ColumnConfig>();
}