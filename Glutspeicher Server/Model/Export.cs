using Csv.Annotations;

namespace Glutspeicher.Server.Model;

[CsvObject(keyAsPropertyName: true)]
public partial class Export
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Uri { get; set; }

    public string Script { get; set; }
}
