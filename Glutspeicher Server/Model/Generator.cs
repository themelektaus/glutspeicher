using Csv.Annotations;

namespace Glutspeicher.Server.Model;

[CsvObject(keyAsPropertyName: true)]
public partial class Generator
{
    public long Id { get; set; }

    public string Name { get; set; }

    public int Length { get; set; }

    public string Questions { get; set; }

    public string Answers { get; set; }
}
