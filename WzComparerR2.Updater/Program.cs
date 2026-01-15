using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        string updateZipPath = args[0];
        string currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string tempUpdateFolder = Path.Combine(currentDirectory, "UpdateTemp");
        string wzComparerR2Path = Path.Combine(currentDirectory, "WzComparerR2.exe");

        try
        {
            KillProcess("WzComparerR2");
            KillProcess("WzComparerR2.anycpu");

            if (Directory.Exists(tempUpdateFolder))
            {
                Directory.Delete(tempUpdateFolder, true);
            }
            Directory.CreateDirectory(tempUpdateFolder);
            ZipFile.ExtractToDirectory(updateZipPath, tempUpdateFolder);

            string[] directoriesToDelete = { "Lib", "Plugin", "runtimes" };
            foreach (var dir in directoriesToDelete)
            {
                string dirPath = Path.Combine(currentDirectory, dir);
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
            }

            foreach (var file in Directory.GetFiles(currentDirectory, "WzComparerR2*"))
            {
                File.Delete(file);
            }

            foreach (var file in Directory.GetFiles(tempUpdateFolder))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(currentDirectory, fileName);
                if (File.Exists(destFile))
                {
                    File.Delete(destFile);
                }
                File.Move(file, destFile);
            }

            MoveDirectory(tempUpdateFolder, currentDirectory);
            Directory.Delete(tempUpdateFolder, true);

        }
        catch (Exception ex)
        {
            MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (File.Exists(updateZipPath))
            {
                File.Delete(updateZipPath);
            }
            if (Directory.Exists(tempUpdateFolder))
            {
                Directory.Delete(tempUpdateFolder, true);
            }
        }
        finally
        {

            Process.Start(wzComparerR2Path);
        }
    }

    static void KillProcess(string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            process.Kill();
            process.WaitForExit();
        }
    }

    static void MoveDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(targetDir, fileName);
            if (File.Exists(destFile))
            {
                File.Delete(destFile);
            }
            File.Move(file, destFile);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string destDir = Path.Combine(targetDir, dirName);
            MoveDirectory(subDir, destDir);
        }
    }
}