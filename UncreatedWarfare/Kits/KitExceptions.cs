using System;

namespace Uncreated.Warfare.Kits;

public class KitNotFoundException : Exception
{
    public string KitId { get; set; }

    public KitNotFoundException(string kitId) : base($"Kit not found: \"{kitId}\".")
    {
        KitId = kitId;
    }
}