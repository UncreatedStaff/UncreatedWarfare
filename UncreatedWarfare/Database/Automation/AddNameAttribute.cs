using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property)]

sealed class AddNameAttribute : Attribute
{
    public string? ColumnName { get; }
    public int MaxLength { get; set; } = 48;
    public AddNameAttribute() { }
    public AddNameAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}