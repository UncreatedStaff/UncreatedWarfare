using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("skills", "skillset", "skillsets"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitSkillsetsCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    private readonly IPlayerService _playerService;
    private readonly ITranslationValueFormatter _valueFormatter;
    public required CommandContext Context { get; init; }

    public KitSkillsetsCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        // have to do this because skill data isn't static for some reason
        Context.AssertRanByPlayer();

        bool add = Context.MatchParameter(1, "add", "set");
        bool remove = !add && Context.MatchParameter(1, "delete", "remove", "clear");

        if (!Context.TryGet(0, out string? kitId) || add == remove || !Context.TryGet(2, out string? skillsetStr))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, true, set => set.Kits.Include(x => x.Skillsets));
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        await UniTask.SwitchToMainThread(token);

        int skillset = Skillset.GetSkillsetFromEnglishName(skillsetStr, out EPlayerSpeciality specialty);
        if (skillset < 0)
        {
            throw Context.Reply(_translations.KitInvalidSkillset, skillsetStr);
        }

        string specialityFormat = specialty switch
        {
            EPlayerSpeciality.DEFENSE => _valueFormatter.FormatEnum((EPlayerDefense)skillset, Context.Language),
            EPlayerSpeciality.OFFENSE => _valueFormatter.FormatEnum((EPlayerOffense)skillset, Context.Language),
            EPlayerSpeciality.SUPPORT => _valueFormatter.FormatEnum((EPlayerSupport)skillset, Context.Language),
            _ => skillset.ToString()
        };

        int maxLevel = Context.Player.Component<SkillsetPlayerComponent>().GetMaxSkillLevel(specialty, (byte)skillset);

        byte level = 0;
        if (add && (!Context.TryGet(3, out level) || level > maxLevel))
        {
            throw Context.Reply(_translations.KitInvalidSkillsetLevel, specialityFormat, maxLevel);
        }

        Skillset set = new Skillset(specialty, (byte)skillset, level);

        bool anyFound = false;
        for (int i = 0; i < kit.Skillsets.Count; ++i)
        {
            if (kit.Skillsets[i].Skillset.SkillIndex != set.SkillIndex || kit.Skillsets[i].Skillset.SpecialityIndex != set.SpecialityIndex)
                continue;

            if (add)
            {
                kit.Skillsets[i].Skillset = set;
                _dbContext.Update(kit.Skillsets[i]);
            }
            else
            {
                _dbContext.Remove(kit.Skillsets[i]);
            }

            anyFound = true;
            break;
        }

        if (!anyFound)
        {
            if (!add)
            {
                throw Context.Reply(_translations.KitSkillsetNotFound, set, kit);
            }

            KitSkillset skillsetModel = new KitSkillset
            {
                Skillset = set,
                Kit = kit,
                KitId = kit.PrimaryKey
            };

            kit.Skillsets.Add(skillsetModel);
            _dbContext.Add(skillsetModel);
        }

        _dbContext.Update(kit);
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(CancellationToken.None);

        Context.LogAction(add ? ActionLogType.AddSkillset : ActionLogType.RemoveSkillset, set + " ON " + kit.InternalName);
        Context.Reply(add ? _translations.KitSkillsetAdded : _translations.KitSkillsetRemoved, set, kit);

        // update skills for all players
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
            if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                continue;

            if (add)
                player.Component<SkillsetPlayerComponent>().EnsureSkillset(set);
            else
                player.Component<SkillsetPlayerComponent>().ResetSkillset(set.Speciality, set.SkillIndex);
        }
    }
}