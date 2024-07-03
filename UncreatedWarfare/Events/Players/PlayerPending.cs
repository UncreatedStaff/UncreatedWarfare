using SDG.Unturned;
using Steamworks;
using System.Reflection;
using Uncreated.Warfare.Players;
using UnityEngine;

namespace Uncreated.Warfare.Events.Players;

/// <summary>
/// Event listener args which handles a patch on when the player gets put in the queue.
/// </summary>
public sealed class PlayerPending : CancellableEvent
{
    private string _rejectReason;
    private static FieldInfo? _faceInfo;
    private static FieldInfo? _hairInfo;
    private static FieldInfo? _beardInfo;
    private static FieldInfo? _hairColorInfo;
    private static FieldInfo? _markerColorInfo;
    private static FieldInfo? _handInfo;
    private static FieldInfo? _skinColorInfo;
    private static FieldInfo? _isProInfo;
    private static FieldInfo? _skillsetInfo;
    private static FieldInfo? _languageInfo;

    /// <summary>
    /// Previous save data for the player.
    /// </summary>
    public required PlayerSave? SaveData { get; init; }

    /// <summary>
    /// The pending player.
    /// </summary>
    public required SteamPending PendingPlayer { get; init; }

    /// <summary>
    /// Cached data that will be transferred to the <see cref="PlayerJoined"/> event.
    /// </summary>
    public required PendingAsyncData AsyncData { get; init; }

    /// <summary>
    /// Rejection reason type if the player is rejected. Defaults to <see cref="ESteamRejection.PLUGIN"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ESteamRejection Rejection { get; set; } = ESteamRejection.PLUGIN;

    /// <summary>
    /// Rejection reason if the player is rejected. Defaults to a generic error message.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required string RejectReason
    {
        get => _rejectReason;
        set => _rejectReason = value ?? string.Empty;
    }

    /// <summary>
    /// Public name of the player's character.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public string CharacterName
    {
        get => PendingPlayer.playerID.characterName;
        set => PendingPlayer.playerID.characterName = value;
    }

    /// <summary>
    /// Name of the player's character visible to their group.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public string NickName
    {
        get => PendingPlayer.playerID.nickName;
        set => PendingPlayer.playerID.nickName = value;
    }

    /// <summary>
    /// The player's group ID.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public CSteamID GroupId
    {
        get => PendingPlayer.playerID.group;
        set => PendingPlayer.playerID.group = value;
    }

    /// <summary>
    /// If the player is a vanilla admin when they join.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public bool IsAdmin
    {
        get => PendingPlayer.assignedAdmin;
        set => PendingPlayer.assignedAdmin = value;
    }

    /// <summary>
    /// Which character the player has selected.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public byte CharacterIndex
    {
        get => PendingPlayer.playerID.characterID;
        set => PendingPlayer.playerID.characterID = value;
    }

    /// <summary>
    /// The Steam name of the pending player.
    /// </summary>
    public string PlayerName => PendingPlayer.playerID.playerName;

    /// <summary>
    /// If this is the first time the player has joined.
    /// </summary>
    public bool IsNewPlayer => SaveData == null;

    /// <summary>
    /// The Steam ID of the pending player.
    /// </summary>
    public CSteamID Steam64 => PendingPlayer.playerID.steamID;

    /// <summary>
    /// The player's starting face style.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public byte FaceIndex
    {
        get => PendingPlayer.face;
        set
        {
            _faceInfo ??= typeof(SteamPending).GetField("_face", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_faceInfo == null)
            {
                L.LogError("Face field (SteamPending._face) not found.");
                return;
            }

            _faceInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's hair style.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public byte HairIndex
    {
        get => PendingPlayer.hair;
        set
        {
            _hairInfo ??= typeof(SteamPending).GetField("_hair", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_hairInfo == null)
            {
                L.LogError("Hair field (SteamPending._hair) not found.");
                return;
            }

            _hairInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's beard style.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public byte BeardIndex
    {
        get => PendingPlayer.beard;
        set
        {
            _beardInfo ??= typeof(SteamPending).GetField("_beard", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_beardInfo == null)
            {
                L.LogError("Beard field (SteamPending._beard) not found.");
                return;
            }

            _beardInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's skin color.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public Color SkinColor
    {
        get => PendingPlayer.skin;
        set
        {
            _skinColorInfo ??= typeof(SteamPending).GetField("_skin", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_skinColorInfo == null)
            {
                L.LogError("Skin color field (SteamPending._skin) not found.");
                return;
            }

            _skinColorInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's hair color.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public Color HairColor
    {
        get => PendingPlayer.color;
        set
        {
            _hairColorInfo ??= typeof(SteamPending).GetField("_color", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_hairColorInfo == null)
            {
                L.LogError("Hair color field (SteamPending._color) not found.");
                return;
            }

            _hairColorInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's marker color.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public Color MarkerColor
    {
        get => PendingPlayer.color;
        set
        {
            _markerColorInfo ??= typeof(SteamPending).GetField("_markerColor", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_markerColorInfo == null)
            {
                L.LogError("Marker color field (SteamPending._markerColor) not found.");
                return;
            }

            _markerColorInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's left-handedness setting.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public bool IsLeftHanded
    {
        get => PendingPlayer.hand;
        set
        {
            _handInfo ??= typeof(SteamPending).GetField("_hand", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_handInfo == null)
            {
                L.LogError("Left-handed field (SteamPending._hand) not found.");
                return;
            }

            _handInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// If the player has Unturned Gold (Pro) when they join.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public bool HasGold
    {
        get => PendingPlayer.isPro;
        set
        {
            _isProInfo ??= typeof(SteamPending).GetField("_isPro", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_isProInfo == null)
            {
                L.LogError("Pro field (SteamPending._isPro) not found.");
                return;
            }

            _isProInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's character skillset.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public EPlayerSkillset Skillset
    {
        get => PendingPlayer.skillset;
        set
        {
            _skillsetInfo ??= typeof(SteamPending).GetField("_skillset", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_skillsetInfo == null)
            {
                L.LogError("Skillset field (SteamPending._skillset) not found.");
                return;
            }

            _skillsetInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The player's Steam language.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public string Language
    {
        get => PendingPlayer.language;
        set
        {
            _languageInfo ??= typeof(SteamPending).GetField("_language", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_languageInfo == null)
            {
                L.LogError("Language field (SteamPending._language) not found.");
                return;
            }

            _languageInfo.SetValue(PendingPlayer, value);
        }
    }

    /// <summary>
    /// The Steam marketplace instance ID of the shirt skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceShirt
    {
        get => PendingPlayer.packageShirt;
        set => PendingPlayer.packageShirt = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the pants skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplacePants
    {
        get => PendingPlayer.packagePants;
        set => PendingPlayer.packagePants = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the hat skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceHat
    {
        get => PendingPlayer.packageHat;
        set => PendingPlayer.packageHat = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the backpack skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceBackpack
    {
        get => PendingPlayer.packageBackpack;
        set => PendingPlayer.packageBackpack = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the vest skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceVest
    {
        get => PendingPlayer.packageVest;
        set => PendingPlayer.packageVest = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the mask skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceMask
    {
        get => PendingPlayer.packageMask;
        set => PendingPlayer.packageMask = value;
    }

    /// <summary>
    /// The Steam marketplace instance ID of the glassess skin equipped on the player.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong EquippedMarketplaceGlasses
    {
        get => PendingPlayer.packageGlasses;
        set => PendingPlayer.packageGlasses = value;
    }

    /// <summary>
    /// The Steam marketplace instance IDs of all equipped skins.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public ulong[] EquippedSkins
    {
        get => PendingPlayer.packageSkins;
        set => PendingPlayer.packageSkins = value;
    }

    /// <summary>
    /// Reject the player. 
    /// </summary>
    /// <returns>An exception that can be thrown without logging an error.</returns>
    public ControlException Reject(string reason, ESteamRejection rejection)
    {
        Rejection = rejection;
        return Reject(reason);
    }

    /// <summary>
    /// Reject the player. 
    /// </summary>
    /// <returns>An exception that can be thrown without logging an error.</returns>
    public ControlException Reject(string reason)
    {
        RejectReason = reason;
        Cancel();
        return new ControlException();
    }
}
