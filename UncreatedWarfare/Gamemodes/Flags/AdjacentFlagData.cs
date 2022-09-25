using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Configuration;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public struct AdjacentFlagData : IJsonReadWrite
    {
        public int flag_id;
        public float weight;
        public AdjacentFlagData(int flagId, float weight = 1f)
        {
            this.flag_id = flagId;
            this.weight = weight;
        }

        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString()!;
                    if (!reader.Read()) return;
                    switch (prop)
                    {
                        case nameof(flag_id):
                            this.flag_id = reader.GetInt32();
                            break;
                        case nameof(weight):
                            this.weight = (float)reader.GetDecimal();
                            break;
                    }
                }
            }
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(flag_id), flag_id);
            writer.WriteProperty(nameof(weight), weight);
        }
    }
}
