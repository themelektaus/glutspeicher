using Csv.Annotations;

namespace Glutspeicher.Server.Model;

[CsvObject(keyAsPropertyName: true)]
public partial class Relay
{
    public long Id { get; set; }

    public string Hostname { get; set; }

    public ushort SshPort { get; set; } = 22;

    public string SshUsername { get; set; } = "root";

    public string SshPassword { get; set; }

    public ushort MinPort { get; set; } = 13300;

    public ushort MaxPort { get; set; } = 13309;

    public string WebCommandLine { get; set; }
}
