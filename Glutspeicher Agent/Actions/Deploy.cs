﻿using Renci.SshNet;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

namespace Glutspeicher.Agent;

public class Deploy
{
    public string sshHostname;
    public string sshUsername;
    public string sshPassword;

    public string targetPath;

    public string solutionName = "Glutspeicher";
    public string dockerContainerName = "glutspeicher";

    public void Run()
    {
        if (string.IsNullOrEmpty(sshHostname))
        {
            throw new($"{nameof(sshHostname)} is null or empty");
        }

        if (string.IsNullOrEmpty(sshUsername))
        {
            throw new($"{nameof(sshUsername)} is null or empty");
        }

        if (string.IsNullOrEmpty(sshPassword))
        {
            throw new($"{nameof(sshPassword)} is null or empty");
        }

        var targetFolder = new DirectoryInfo(targetPath);
        if (!targetFolder.Exists)
        {
            throw new($"{nameof(targetFolder)} not exists");
        }

        FileInfo solutionFile = null;
        DirectoryInfo folder = new(Environment.CurrentDirectory);

        while (solutionFile is null)
        {
            if (folder is null)
            {
                break;
            }

            solutionFile = folder.EnumerateFiles("*.sln").FirstOrDefault(x => x.Name == $"{solutionName}.sln");

            if (solutionFile is null)
            {
                folder = folder.Parent;
            }
        }

        if (solutionFile is null)
        {
            throw new($"{nameof(solutionFile)} not found");
        }

        var ok = false;

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

        var dialog = new Dialog { Text = $"{nameof(Deploy)} to {sshHostname}" };

        dialog.AddButton($"Yes", 100, Color.DarkGreen).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            ok = true;
            dialog.Close();
        };

        dialog.AddButton($"No", 100, Color.DarkRed).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            dialog.Close();
        };

        dialog.BeforeShow();
        dialog.ShowDialog();
        dialog.Dispose();

        if (!ok)
        {
            return;
        }

        DirectoryInfo linuxServerBuild = new(Path.Combine(solutionFile.DirectoryName, $"{solutionName} Server", "Build", "Linux"));
        if (!linuxServerBuild.Exists)
        {
            throw new DirectoryNotFoundException(linuxServerBuild.FullName);
        }

        DirectoryInfo windowsServerBuild = new(Path.Combine(solutionFile.DirectoryName, $"{solutionName} Server", "Build", "Windows"));
        if (!windowsServerBuild.Exists)
        {
            throw new DirectoryNotFoundException(windowsServerBuild.FullName);
        }

        DirectoryInfo agentBuild = new(Path.Combine(solutionFile.DirectoryName, $"{solutionName} Agent", "Build"));
        if (!agentBuild.Exists)
        {
            throw new DirectoryNotFoundException(agentBuild.FullName);
        }

        DirectoryInfo tempFolder = new(Path.Combine(solutionFile.DirectoryName, "Temp"));

        if (tempFolder.Exists)
        {
            tempFolder.Delete(recursive: true);
        }

        tempFolder.Create();

        CopyFiles(linuxServerBuild, tempFolder);

        Zip(
            Path.Combine(tempFolder.FullName, "wwwroot", "static", "glutspeicher-server-for-linux.zip"),
            linuxServerBuild
        );

        Zip(
            Path.Combine(tempFolder.FullName, "wwwroot", "static", "glutspeicher-server-for-windows.zip"),
            windowsServerBuild
        );

        Zip(
            Path.Combine(tempFolder.FullName, "wwwroot", "static", "glutspeicher-agent-for-windows.zip"),
            agentBuild
        );

        using var sshClient = new SshClient(sshHostname, sshUsername, sshPassword);

        sshClient.Connect();
        sshClient.RunCommand($"docker stop {dockerContainerName}").Dispose();

        var wwwroot = new DirectoryInfo(Path.Combine(targetFolder.FullName, "wwwroot"));
        if (wwwroot.Exists)
        {
            wwwroot.Delete(recursive: true);
        }

        CopyFiles(tempFolder, targetFolder);

        sshClient.RunCommand($"docker start {dockerContainerName}").Dispose();
        sshClient.Disconnect();

        tempFolder.Delete(recursive: true);
    }

    public static void CopyFiles(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo folder in source.GetDirectories())
        {
            CopyFiles(folder, target.CreateSubdirectory(folder.Name));
        }

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), overwrite: true);
        }
    }

    static void Zip(string archiveFileName, DirectoryInfo folder)
    {
        using var zip = ZipFile.Open(archiveFileName, ZipArchiveMode.Create);

        foreach (var file in folder.EnumerateFiles("*.*", SearchOption.AllDirectories))
        {
            zip.CreateEntryFromFile(
                file.FullName,
                Path.GetRelativePath(folder.FullName, file.FullName)
            );
        }
    }
}
