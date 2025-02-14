using LiteDB;
using System.IO;
using System.Threading;

namespace Glutspeicher.Server.Context;

public class LiteDbContext : IDisposable
{
    static readonly SemaphoreSlim @lock = new(1, 1);

    public LiteDatabase Database { get; }

    bool disposed;

    public LiteDbContext()
    {
        Directory.CreateDirectory("Data");

        @lock.Wait();

        try
        {
            Database = new(Path.Combine("Data", "Database.litedb"));
        }
        catch
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Database?.Dispose();
            @lock.Release();
        }
    }
}
