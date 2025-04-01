using System;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("level", "lvl"), SubCommandOf(typeof(KitSetCommand))]
internal sealed class KitSetLevelCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    public required CommandContext Context { get; init; }

    public KitSetLevelCommand(IKitDataStore kitDataStore, TranslationInjection<KitCommandTranslations> translations)
    {
        _kitDataStore = kitDataStore;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? kitId) || !Context.TryGet(1, out int level))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Translations, token);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        kitId = kit.Id;

        await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Translations | KitInclude.UnlockRequirements, kit =>
        {
            if (level <= 0)
            {
                kit.UnlockRequirements.RemoveAll(x =>
                        x.Type.Contains(nameof(LevelUnlockRequirement), StringComparison.Ordinal)
                        && ContextualTypeResolver.TryResolveType(x.Type, out Type? type, typeof(UnlockRequirement))
                        && type == typeof(LevelUnlockRequirement)
                    );
            }
            else
            {
                int index = kit.UnlockRequirements.FindIndex(x =>
                    x.Type.Contains(nameof(LevelUnlockRequirement), StringComparison.Ordinal)
                    && ContextualTypeResolver.TryResolveType(x.Type, out Type? type, typeof(UnlockRequirement))
                    && type == typeof(LevelUnlockRequirement)
                );

                string data = JsonSerializer.Serialize(new LevelUnlockRequirement
                {
                    UnlockLevel = level
                }, ConfigurationSettings.JsonCondensedSerializerSettings);

                if (index == -1)
                {
                    kit.UnlockRequirements.Add(new KitUnlockRequirement
                    {
                        Data = data,
                        Type = typeof(LevelUnlockRequirement).AssemblyQualifiedName!
                    });
                }
                else
                {
                    kit.UnlockRequirements[index].Data = data;
                }
            }
        }, Context.CallerId, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        Context.Reply(_translations.KitPropertySet, "Level", kit, level.ToString(Context.Culture));
        // todo: Context.LogAction(ActionLogType.SetKitProperty, kitId + ": LEVEL >> " + level);
    }
}