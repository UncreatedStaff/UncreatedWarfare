using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Web;

[Table("loadout_purchases")]
public class LoadoutPurchase
{
    [NotMapped, EditorBrowsable(EditorBrowsableState.Never)]
    public object? FormObject { get; set; }

    [Required, Key, ForeignKey(nameof(Kit))]
    [Column("CreatedKit")]
    public uint KitId { get; set; }

    public KitModel? Kit { get; set; }

    /// <summary>
    /// Number 1-inf mapping to the alphabet. (1 = A, 28 = AB)
    /// </summary>
    /// <remarks>Use <see cref="LoadoutIdHelper"/> to convert between IDs, Steam64s, and kit IDs.</remarks>
    public int LoadoutId { get; set; }

    [Required, ForeignKey(nameof(User))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    public WarfareUserData? User { get; set; }

    [DefaultValue(false)]
    public bool Paid { get; set; }

    public RequestStatus Status { get; set; }
    public EditStatus Edit { get; set; }

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset FormModified { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public int Season { get; set; }

    [Column("FormYaml", TypeName = "TEXT"), StringLength(ushort.MaxValue)]
    public string? FormYaml
    {
        get;
        set
        {
            FormObject = null;
            field = value;
            FormObject = null;
        }
    }

    [StringLength(512)]
    public string? AdminChangeRequest { get; set; }

    [Column("AdminChangeRequester")]
    public ulong? AdminChangeRequesterId { get; set; }

    [ForeignKey("AdminChangeRequesterId")]
    public WarfareUserData? AdminChangeRequester { get; set; }
    public DateTimeOffset? AdminChangeRequestDate { get; set; }

    [StringLength(256)]
    public string? PlayerChangeRequest { get; set; }
    public DateTimeOffset? PlayerChangeRequestDate { get; set; }

    [StringLength(70)]
    public string? StripeSessionId { get; set; }

    [NotMapped]
    public string LoadoutKitId => LoadoutIdHelper.GetLoadoutName(new CSteamID(Steam64), LoadoutId);

    [NotMapped]
    public bool PlayerEditDenied => Status != RequestStatus.ChangesRequested && Edit == EditStatus.None && AdminChangeRequest != null && AdminChangeRequestDate != null && (DateTime.UtcNow - AdminChangeRequestDate).Value.TotalDays <= 3;

    public LoadoutPurchase() { }

    public LoadoutPurchase(int id, ulong steam64, uint kit, int season, string formYaml)
    {
        LoadoutId = id;
        Steam64 = steam64;
        KitId = kit;
        Paid = false;
        Status = RequestStatus.AwaitingApproval;
        Edit = EditStatus.None;
        Created = DateTime.UtcNow;
        FormModified = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
        Season = season;
        FormYaml = formYaml;
    }

    public enum RequestStatus
    {
        AwaitingApproval,
        ChangesRequested,
        InProgress,
        Completed
    }

    public enum EditStatus
    {
        None,
        EditRequested,
        EditAllowed,
        SeasonalUpdate
    }
}