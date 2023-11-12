using Uncreated.Warfare.Database.Abstractions;

namespace Uncreated.Warfare.Database;
public static class WarfareDatabases
{
    /*
     * The idea here is so we can use other contexts outside this assembly.
     */
#nullable disable
    public static IFactionDbContext Factions { get; set; }
    public static ILanguageDbContext Languages { get; set; }
    public static IUserDataDbContext UserData { get; set; }
    public static IKitsDbContext Kits { get; set; }
#nullable restore
    public static void LoadFromWarfareDbContext(WarfareDbContext context)
    {
        Factions = context;
        Languages = context;
        UserData = context;
        Kits = context;
    }
}
