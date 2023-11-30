using System;

namespace Uncreated.Warfare.Database.Automation;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class IndexAttribute : Attribute
{
    public IndexAttribute()
    {
        
    }
}