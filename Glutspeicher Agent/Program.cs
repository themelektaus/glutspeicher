using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Glutspeicher.Agent;

public static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath);

        var options = Utils.ReadGlutLink(args);

        if (options is null)
        {
            new Register().Run();
            return;
        }

        var showLog = false;

        var log = new StringBuilder();

        try
        {
            var @namespace = $"{nameof(Glutspeicher)}.{nameof(Glutspeicher.Agent)}";
            var types = Assembly.GetExecutingAssembly().GetTypes();
            var type = types.FirstOrDefault(x => x.Namespace == @namespace && x.Name == options["type"]);

            var instance = Activator.CreateInstance(type);

            type.GetField(nameof(log))?.SetValue(instance, log);

            foreach (var option in options)
            {
                type.GetField(option.Key)?.SetValue(instance, option.Value);
            }

            log.AppendLine(
                JsonConvert.SerializeObject(new
                {
                    type = instance.GetType().Name,
                    instance,
                },
                Formatting.Indented)
            );

            var result = type.GetMethods().FirstOrDefault().Invoke(instance, null);

            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            log.AppendLine(ex.ToString());
            showLog = true;
        }

        string tempFile;

        try
        {
            tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, log.ToString());
        }
        catch
        {
            throw;
        }

        if (showLog)
        {
            try
            {
                await Utils.RunAsync("notepad", tempFile, hidden: false);
            }
            catch
            {
                throw;
            }
        }
    }
}
