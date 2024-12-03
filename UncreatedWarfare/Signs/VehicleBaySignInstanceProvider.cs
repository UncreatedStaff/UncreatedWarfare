using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Costs;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Signs;

[SignPrefix("vbs_")]
public class VehicleBaySignInstanceProvider : ISignInstanceProvider, IRequestable<VehicleSpawnInfo>
{
    private static readonly StringBuilder LoadoutSignBuffer = new StringBuilder(230);

    private static readonly Color32 VbsBranchColor = new Color32(155, 171, 171, 255);
    private static readonly Color32 VbsNameColor = new Color32(255, 255, 255, 255);

    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleInfoStore _infoStore;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly VehicleBaySignTranslations _translations;
    private Guid _fallbackGuid;
    private VehicleAsset? _fallbackAsset;
    private BarricadeDrop _barricade;
    private VehicleSpawnerComponent? _component;

    public VehicleSpawnInfo? Spawn { get; private set; }
    public WarfareVehicleInfo? Vehicle { get; private set; }

    /// <inheritdoc />
    bool ISignInstanceProvider.CanBatchTranslate => Vehicle == null || Spawn == null || Vehicle.Class == Class.None;

    /// <inheritdoc />
    string ISignInstanceProvider.FallbackText => _fallbackAsset != null ? _fallbackAsset.vehicleName : _fallbackGuid.ToString("N", CultureInfo.InvariantCulture);

    public VehicleBaySignInstanceProvider(
        VehicleSpawnerStore spawnerStore,
        VehicleInfoStore infoStore,
        ITranslationValueFormatter valueFormatter,
        TranslationInjection<VehicleBaySignTranslations> translations)
    {
        _spawnerStore = spawnerStore;
        _infoStore = infoStore;
        _valueFormatter = valueFormatter;
        _translations = translations.Value;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        if (Guid.TryParseExact(extraInfo, "N", out _fallbackGuid))
            _fallbackAsset = Assets.find<VehicleAsset>(_fallbackGuid);

        _barricade = barricade;
    }

    public string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        Spawn ??= _spawnerStore.Spawns.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == _barricade.instanceID));
        Vehicle ??= Spawn == null ? null : _infoStore.Vehicles.FirstOrDefault(x => x.Vehicle.MatchAsset(Spawn.Vehicle));

        if (_component == null)
        {
            _component = Spawn?.Spawner.Model.GetComponent<VehicleSpawnerComponent>();
        }

        if (Vehicle == null || Spawn == null || _component == null)
        {
            return _fallbackAsset?.vehicleName ?? _fallbackGuid.ToString("N", CultureInfo.InvariantCulture);
        }

        try
        {
            TranslateKitSign(LoadoutSignBuffer, Vehicle, _component, language, culture, player);
            return LoadoutSignBuffer.ToString();
        }
        finally
        {
            LoadoutSignBuffer.Clear();
        }
    }

    private void TranslateKitSign(StringBuilder bldr, WarfareVehicleInfo info, VehicleSpawnerComponent component, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        string name = info.ShortName ?? info.Vehicle.GetAsset()?.FriendlyName ?? info.Vehicle.ToString();
        bldr.AppendColorized(name, VbsNameColor)
            .Append('\n')
            .AppendColorized(_valueFormatter.FormatEnum(info.Branch, language), VbsBranchColor)
            .Append('\n');

        if (info.TicketCost > 0)
        {
            bldr.Append(_translations.VBSTickets.Translate(info.TicketCost, language, culture));
        }

        bldr.Append('\n');

        bool anyUnlockReq = false;
        foreach (UnlockRequirement req in info.UnlockRequirements)
        {
            if (player != null && req.CanAccessFast(player))
                continue;

            bldr.Append(req.GetSignText(player, language, culture));
            anyUnlockReq = true;
            break;
        }

        if (info.UnlockCosts.Count > 0)
        {
            UnlockCost cost = info.UnlockCosts[0];

            if (anyUnlockReq)
                bldr.Append(' ', 4);

            cost.AppendSignText(bldr, player, language, culture);
        }

        bldr.Append('\n');

        switch (component.State)
        {
            case VehicleSpawnerState.Destroyed:
                bldr.Append(_translations.VBSStateDead.Translate(component.GetRespawnDueTime(), language, culture));
                break;

            case VehicleSpawnerState.Deployed:
                bldr.Append(_translations.VBSStateActive.Translate(component.GetLocation(), language, culture));
                break;

            case VehicleSpawnerState.Idle:
                bldr.Append(_translations.VBSStateIdle.Translate(component.GetRespawnDueTime(), language, culture));
                break;
            case VehicleSpawnerState.LayoutDisabled:
                bldr.Append(_translations.VBSLayoutDisabled.Translate(language));
                break;

            // todo delays

            default:
                bldr.Append(_translations.VBSStateReady.Translate(language));
                break;

        }
    }
}

public class VehicleBaySignTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Vehicle Bay Signs";

    [TranslationData("Displays the ticket cost on a vehicle bay sign.")]
    public readonly Translation<int> VBSTickets = new Translation<int>("<#ffffff>{0}</color> <#f0f0f0>Tickets</color>", TranslationOptions.TMProSign);

    [TranslationData("Displays the state of the sign when the vehicle is ready to be requested.")]
    public readonly Translation VBSStateReady = new Translation("<#33cc33>Ready!</color> <#aaa><b>/request</b></color>");

    [TranslationData("Displays the state of the sign when the vehicle is destroyed.", Parameters = [ "Minutes", "Seconds" ], IsPriorityTranslation = false)]
    public readonly Translation<TimeSpan> VBSStateDead = new Translation<TimeSpan>("<#ff0000>{0}</color>", arg0Fmt: TimeAddon.Create(TimeFormatType.CountdownMinutesSeconds));

    [TranslationData("Displays the state of the sign when the vehicle is in use.", Parameters = [ "Nearest location." ], IsPriorityTranslation = false)]
    public readonly Translation<string> VBSStateActive = new Translation<string>("<#ff9933>{0}</color>");

    [TranslationData("Displays the state of the sign when the vehicle was left idle on the field.", Parameters = [ "Minutes", "Seconds" ])]
    public readonly Translation<TimeSpan> VBSStateIdle = new Translation<TimeSpan>("<#ffcc00>Idle: {0}</color>", arg0Fmt: TimeAddon.Create(TimeFormatType.CountdownMinutesSeconds));

    [TranslationData("Displays the state of the sign when the vehicle spawner is disabled in the current layout.")]
    public readonly Translation VBSLayoutDisabled = new Translation("<#798082>Disabled</color>");
}