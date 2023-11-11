using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
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
    /// The ID of the user that opened the appeal.
    /// </summary>
    [JsonPropertyName("discord_user_id")]
    public ulong? DiscordUserId { get; set; }

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
    public AppealResponse[] Responses { get; set; } = Array.Empty<AppealResponse>();
    internal override async Task FillDetail(DatabaseInterface db, CancellationToken token = default)
    {
        if (Punishments.Length != PunishmentKeys.Length)
            Punishments = new Punishment[PunishmentKeys.Length];

        await db.ReadAll(Punishments, PunishmentKeys, true, true, false, token).ConfigureAwait(false);
        await base.FillDetail(db, token).ConfigureAwait(false);
    }

    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        TicketId = reader.ReadGuid();
        AppealState = reader.ReadNullableBool();
        DiscordUserId = reader.ReadNullableUInt64();
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
        writer.WriteNullable(DiscordUserId);
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
        else if (propertyName.Equals("discord_user_id", StringComparison.InvariantCultureIgnoreCase))
            DiscordUserId = reader.TokenType == JsonTokenType.Null ? new ulong?() : reader.GetUInt64();
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

        writer.WritePropertyName("discord_user_id");
        if (DiscordUserId.HasValue)
            writer.WriteNumberValue(DiscordUserId.Value);
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

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 2 + PunishmentKeys.Length + Responses.Length * 2;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        if (TicketId != Guid.Empty)
            workingList.Add($"Ticket: {TicketId:N}");
        
        workingList.Add($"State: {AppealState switch { true => "Accepted", false => "Denied", _ => "Undecided" }}");

        if (DiscordUserId.HasValue)
            workingList.Add($"Discord ID: <@{DiscordUserId.Value.ToString(CultureInfo.InvariantCulture)}>");
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableAppeals}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAppealsTicketId, DatabaseInterface.ColumnAppealsState,
            DatabaseInterface.ColumnAppealsDiscordId)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 3, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnAppealsTicketId}` = `t`.`{DatabaseInterface.ColumnAppealsTicketId}`," +
                       $"`{DatabaseInterface.ColumnAppealsState}` = `t`.`{DatabaseInterface.ColumnAppealsState}`," +
                       $"`{DatabaseInterface.ColumnAppealsDiscordId}` = `t`.`{DatabaseInterface.ColumnAppealsDiscordId}`;");

        args.Add(TicketId.ToString("N"));
        args.Add(AppealState.HasValue ? AppealState.Value : DBNull.Value);
        args.Add(DiscordUserId.HasValue ? DiscordUserId.Value : DBNull.Value);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableAppealPunishments}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (PunishmentKeys.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableAppealPunishments}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAppealPunishmentsPunishment)}) VALUES ");

            for (int i = 0; i < PunishmentKeys.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(PunishmentKeys[i]);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableAppealResponses}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (Responses.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableAppealResponses}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAppealResponsesQuestion, DatabaseInterface.ColumnAppealResponsesResponse)}) VALUES ");

            for (int i = 0; i < Responses.Length; ++i)
            {
                ref AppealResponse response = ref Responses[i];
                F.AppendPropertyList(builder, args.Count, 2, i, 1);
                args.Add(response.Question.MaxLength(255) ?? string.Empty);
                args.Add(response.Response.MaxLength(1024) ?? string.Empty);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public readonly struct AppealResponse
{
    [JsonPropertyName("question")]
    public string Question { get; }

    [JsonPropertyName("response")]
    public string Response { get; }

    public AppealResponse() { }
    public AppealResponse(string question, string response)
    {
        Question = question;
        Response = response;
    }
}