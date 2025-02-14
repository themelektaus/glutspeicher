namespace Glutspeicher.Server;

public struct ApiResultOptions<T>
{
    public ApiListInfo Apply(IEnumerable<T> data)
    {
        var now = Now;

        var list = data.ToList();

        return new()
        {
            Total = list.Count,
            List = list.Cast<object>().ToList(),
            Benchmark = Now - now
        };
    }
}
