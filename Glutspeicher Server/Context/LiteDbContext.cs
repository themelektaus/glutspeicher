using LiteDB;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Glutspeicher.Server.Context;

public class LiteDbContext : IDisposable
{
    static readonly SemaphoreSlim @lock = new(1, 1);

    public LiteDatabase Database { get; }

    readonly MemoryStream stream;

    bool isDirty;

    public LiteDbContext()
    {
        Directory.CreateDirectory("Data");

        @lock.Wait();

        try
        {
            var fileEncrypted = new FileInfo(Path.Combine("Data", "Database.litedb.encrypted"));
            byte[] data;

            var file = new FileInfo(Path.Combine("Data", "Database.litedb"));

            if (file.Exists)
            {
                Convert(file, fileEncrypted);
            }

            var fileGzEncrypted = new FileInfo(Path.Combine("Data", "Database.litedb.gz.encrypted"));

            if (fileGzEncrypted.Exists)
            {
                data = File.ReadAllBytes(fileGzEncrypted.FullName);
                Decrypt(ref data);
                GUnzip(ref data);
                Encrypt(ref data);

                File.WriteAllBytes(fileEncrypted.FullName, data);
                fileEncrypted.Refresh();

                fileGzEncrypted.Delete();
            }

            if (!fileEncrypted.Exists)
            {
                var database = new LiteDatabase(file.FullName);
                database.Dispose();

                Convert(file, fileEncrypted);
            }

            data = File.ReadAllBytes(fileEncrypted.FullName);
            Decrypt(ref data);

            stream = new();

            using (var temp = new MemoryStream(data))
            {
                temp.CopyTo(stream);
            }

            Database = new(stream);
        }
        catch
        {
            Dispose();
        }
    }

    static void Convert(FileInfo file, FileInfo fileEncrypted)
    {
        var data = File.ReadAllBytes(file.FullName);
        Encrypt(ref data);

        File.WriteAllBytes(fileEncrypted.FullName, data);
        fileEncrypted.Refresh();

        file.Delete();
    }

    public void SetDirty() => isDirty = true;

    public void Dispose()
    {
        if (stream is not null)
        {
            if (Database is not null)
            {
                Database.Dispose();

                if (isDirty)
                {
                    Write(stream);
                }
            }

            stream.Dispose();
        }

        @lock.Release();
    }

    static void Write(MemoryStream stream)
    {
        var data = stream.ToArray();

        var compressedData = data;
        GZip(ref compressedData);

        Encrypt(ref compressedData);
        File.WriteAllBytes(
            Path.Combine("Data", $"Database {Now:yyyy-MM-dd HH-mm-ss}.litedb.gz.encrypted"),
            compressedData
        );

        Encrypt(ref data);
        File.WriteAllBytes(Path.Combine("Data", "Database.litedb.encrypted"), data);
    }

    static void GZip(ref byte[] data)
    {
        using var destinationStream = new MemoryStream();

        using (var gzipStream = new GZipStream(destinationStream, CompressionMode.Compress, false))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        data = destinationStream.ToArray();
    }

    static void GUnzip(ref byte[] data)
    {
        using var destinationStream = new MemoryStream();

        using (var sourceStream = new MemoryStream(data))
        {
            using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, false);
            gzipStream.CopyTo(destinationStream);
        }

        data = destinationStream.ToArray();
    }
}
