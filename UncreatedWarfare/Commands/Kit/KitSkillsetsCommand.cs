using Microsoft.Extensions.DependencyInjection;
using System;
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
    private readonly IKitDataStore _kitDataStore;
    private readonly IPlayerService _playerService;
    private readonly ITranslationValueFormatter _valueFormatter;
    public required CommandContext Context { get; init; }

    public KitSkillsetsCommand(IServiceProvider serviceProvider)
    {
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
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

        Kit? kit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Translations | KitInclude.Skillsets, token);
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

        kit = await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Skillsets | KitInclude.Translations, kit =>
        {
            KitSkillset skillsetModel;
            for (int i = 0; i < kit.Skillsets.Count; ++i)
            {
                skillsetModel = kit.Skillsets[i];
                if (skillsetModel.Skillset.SkillIndex != set.SkillIndex || skillsetModel.Skillset.SpecialityIndex != set.SpecialityIndex)
                    continue;

                if (add)
                {
                    skillsetModel.Skillset = set;
                }
                else
                {
                    kit.Skillsets.RemoveAt(i);
                }

                anyFound = true;
                break;
            }

            if (anyFound || !add)
                return;

            skillsetModel = new KitSkillset
            {
                Skillset = set,
                KitId = kit.PrimaryKey
            };

            kit.Skillsets.Add(skillsetModel);

        }, Context.CallerId, token).ConfigureAwait(false);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        if (!anyFound && !add)
        {
            throw Context.Reply(_translations.KitSkillsetNotFound, set, kit);
        }

        // todo: Context.LogAction(add ? ActionLogType.AddSkillset : ActionLogType.RemoveSkillset, set + " ON " + kit.Id);
        Context.Reply(add ? _translations.KitSkillsetAdded : _translations.KitSkillsetRemoved, set, kit);

        // update skills for all players
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
            if (!activeKit.HasValue || activeKit.Value != kit.Key)
                continue;

            if (add)
                player.Component<SkillsetPlayerComponent>().EnsureSkillset(set);
            else
                player.Component<SkillsetPlayerComponent>().ResetSkillset(set.Speciality, set.SkillIndex);
        }
    }
}