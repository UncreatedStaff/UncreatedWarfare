using System;
using System.IO;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Commands.Permissions;
public class PermissionSaver : JSONSaver<PermissionSave>
{
    public static PermissionSaver Instance;

    public PermissionSaver() : base(Path.Combine(Data.Paths.BaseDirectory, "permissions.json"))
    {
        Instance = this;
    }
    protected override string LoadDefaults() => EMPTY_LIST;
    protected override void OnRead()
    {
        if (RemoveAll(x => x.PermissionLevel == EAdminType.MEMBER) > 0) Save();
    }
    public EAdminType GetPlayerPermissionLevel(ulong player, bool forceFullSearch = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!forceFullSearch && PlayerManager.FromID(player) is UCPlayer pl)
            return pl.PermissionLevel;
        else
        {
            for (int i = 0; i < Count; ++i)
            {
                PermissionSave save = this[i];
                for (int j = 0; j < save.Members.Length; ++j)
                    if (save.Members[j] == player)
                        return save.PermissionLevel;
            }
        }

        return EAdminType.MEMBER;
    }
    public void SetPlayerPermissionLevel(ulong player, EAdminType level)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (PlayerManager.FromID(player) is UCPlayer pl)
            pl.ResetPermissionLevel();
        bool set = false;
        for (int i = Count - 1; i >= 0; --i)
        {
            PermissionSave save = this[i];
            for (int j = save.Members.Length - 1; j >= 0; --j)
            {
                if (save.Members[j] == player)
                {
                    if (save.PermissionLevel == level)
                        goto c;
                    else
                        RemovePlayer(i, j);
                }
            }
            if (save.PermissionLevel == level)
            {
                AddPlayer(player, i);
                set = true;
            }
        c:;
        }
        if (!set && level > EAdminType.MEMBER)
        {
            Add(new PermissionSave() { PermissionLevel = level, Members = new ulong[1] { player } });
            set = true;
        }

        Save();
    }
    public void AddPlayer(ulong player, int save)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PermissionSave psave = this[save];
        ulong[] old = psave.Members;
        psave.Members = new ulong[old.Length + 1];
        if (old.Length > 0)
        {
            Buffer.BlockCopy(old, 0, psave.Members, 0, sizeof(ulong) * old.Length);
            psave.Members[psave.Members.Length - 1] = player;
        }
        else
            psave.Members[0] = player;
    }
    public bool RemovePlayer(int save, int index)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PermissionSave psave = this[save];
        if (psave.Members.Length == 0) return false;
        ulong[] old = psave.Members;
        psave.Members = new ulong[old.Length - 1];
        if (old.Length == 1)
        {
            Remove(psave);
            return true;
        }
        if (index != 0)
            Buffer.BlockCopy(old, 0, psave.Members, 0, sizeof(ulong) * index);
        Buffer.BlockCopy(old, (index + 1) * sizeof(ulong), psave.Members, index * sizeof(ulong), sizeof(ulong) * (old.Length - index - 1));
        return true;
    }
}

public class PermissionSave
{
    [JsonPropertyName("permission")]
    public EAdminType PermissionLevel;
    [JsonPropertyName("members")]
    public ulong[] Members;
}