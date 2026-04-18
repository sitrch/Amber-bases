using System;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnDisplayNameAttribute : Attribute
{
    public string Name { get; }
    public bool Visible { get; }
    public ColumnDisplayNameAttribute(string name, bool visible = true)
    {
        Name = name;
        Visible = visible;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnVisibleAttribute : Attribute
{
    public bool Visible { get; }
    public ColumnVisibleAttribute(bool visible = false) => Visible = visible;
}