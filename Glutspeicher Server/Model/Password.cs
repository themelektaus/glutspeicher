﻿using Csv.Annotations;
using LiteDB;

namespace Glutspeicher.Server.Model;

[CsvObject(keyAsPropertyName: true)]
public partial class Password
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Uri { get; set; }

    public string Username { get; set; }

    [BsonField(nameof(Password))]
    [JsonPropertyName("password")]
    public string StaticPassword { get; set; }

    public long GeneratorId { get; set; }

    public string GeneratedPassword { get; set; }

    public string Description { get; set; }

    public string Totp { get; set; }

    public string Source { get; set; }

    public string Section { get; set; }

    public long RelayId { get; set; }
}
