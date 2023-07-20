using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Punishments;

namespace Uncreated.Warfare.Moderation.Appeals;
[ModerationEntry(ModerationEntryType.Appeal)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Appeal : ModerationEntry
{
    /// <summary>
    /// Unique ID of the ticket.
    /// </summary>
    [JsonPropertyName("ticket_guid")]
    public Guid TicketId { get; set; }

    /// <summary>
    /// <see langword="null"/> if the appeal hasn't been resolved yet, otherwise whether or not the appeal was accepted.
    /// </summary>
    [JsonPropertyName("appeal_state")]
    public bool? AppealState { get; set; }

    /// <summary>
    /// Punishments being appealed.
    /// </summary>
    [JsonPropertyName("punishments_detail")]
    public Punishment?[] Punishments { get; set; } = Array.Empty<Punishment>();

    /// <summary>
    /// Keys to the punishments being appealed.
    /// </summary>
    [JsonPropertyName("punishments")]
    public PrimaryKey[] PunishmentKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// Responses to the asked questions.
    /// </summary>
    [JsonPropertyName("responses")]
    public AppealResponse[] Responses { get; set; }

    internal override async Task FillDetail(DatabaseInterface db)
    {
        if (Punishments.Length != PunishmentKeys.Length)
            Punishments = new Punishment[PunishmentKeys.Length];
        for (int i = 0; i < PunishmentKeys.Length; ++i)
        {
            PrimaryKey key = PunishmentKeys[i];
            if (db.Cache.TryGet<Punishment>(key.Key, out Punishment? p, DatabaseInterface.DefaultInvalidateDuration))
                Punishments[i] = p;
            else
            {
                p = await db.ReadOne<Punishment>(key).ConfigureAwait(false);
                Punishments[i] = p;
            }
        }
    }

    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        TicketId = reader.ReadGuid();
        AppealState = reader.ReadNullableBool();
        PunishmentKeys = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < PunishmentKeys.Length; ++i)
            PunishmentKeys[i] = reader.ReadInt32();
        Responses = new AppealResponse[reader.ReadInt32()];
        for (int i = 0; i < Responses.Length; ++i)
            Responses[i] = new AppealResponse(reader.ReadString(), reader.ReadString());
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(TicketId);
        writer.WriteNullable(AppealState);
        writer.Write(PunishmentKeys.Length);
        for (int i = 0; i < PunishmentKeys.Length; ++i)
            writer.Write(PunishmentKeys[i].Key);
        writer.Write(Responses.Length);
        for (int i = 0; i < Responses.Length; ++i)
        {
            ref AppealResponse response = ref Responses[i];
            writer.Write(response.Question);
            writer.Write(response.Response);
        }
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("ticket_guid", StringComparison.InvariantCultureIgnoreCase))
            TicketId = reader.GetGuid();
        else if (propertyName.Equals("appeal_state", StringComparison.InvariantCultureIgnoreCase))
            AppealState = reader.TokenType == JsonTokenType.Null ? new bool?() : reader.GetBoolean();
        else if (propertyName.Equals("punishments_detail", StringComparison.InvariantCultureIgnoreCase))
            Punishments = JsonSerializer.Deserialize<Punishment?[]>(ref reader, options) ?? Array.Empty<Punishment>();
        else if (propertyName.Equals("punishments", StringComparison.InvariantCultureIgnoreCase))
            PunishmentKeys = JsonSerializer.Deserialize<PrimaryKey[]>(ref reader, options) ?? Array.Empty<PrimaryKey>();
        else if (propertyName.Equals("responses", StringComparison.InvariantCultureIgnoreCase))
            Responses = JsonSerializer.Deserialize<AppealResponse[]>(ref reader, options) ?? Array.Empty<AppealResponse>();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WriteString("ticket_guid", TicketId);

        writer.WritePropertyName("appeal_state");
        if (AppealState.HasValue)
            writer.WriteBooleanValue(AppealState.Value);
        else
            writer.WriteNullValue();

        if (Punishments.Length > 0 && Punishments.Length == PunishmentKeys.Length && Punishments.All(x => x != null))
        {
            writer.WritePropertyName("punishments_detail");
            JsonSerializer.Serialize(writer, Punishments, options);
        }
        writer.WritePropertyName("punishments");
        JsonSerializer.Serialize(writer, PunishmentKeys, options);

        writer.WritePropertyName("responses");
        JsonSerializer.Serialize(writer, Responses, options);
    }
}

public readonly struct AppealResponse
{
    [JsonPropertyName("question")]
    public string Question { get; }

    [JsonPropertyName("response")]
    public string Response { get; }

    [JsonConstructor]
    public AppealResponse(string question, string response)
    {
        Question = question;
        Response = response;
    }
}