using LiteDB;
using System.IO;
using System.Threading;

namespace Glutspeicher.Server.Context;

public class LiteDbContext : IDisposable
{
    public static readonly string path = Path.Combine("Data", "Database.litedb");

    static readonly SemaphoreSlim @lock = new(1, 1);

    public LiteDatabase Database { get; }

    bool disposed;

    public LiteDbContext()
    {
        Directory.CreateDirectory("Data");

        @lock.Wait();

        try
        {
            Database = new(path);
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

    public static FileStream Read()
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read);
    }
}
