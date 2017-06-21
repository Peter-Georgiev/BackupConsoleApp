using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;

class backup
{
    static void Main()
    {
        List<string> logMessage = new List<string>();

        logMessage.Add("====================================================\r\n");
        logMessage.Add($"Дата и час на архивиране: {DateTime.Now}\r\n");

        string[] readConfigFile = new string[0];
        string bacupConf = "backup.conf";
        string dirBackupConf = @"C:\BackupConfAndLog\";

        readConfigFile = GetCorrectConfigFile(readConfigFile, bacupConf, dirBackupConf);

        if (!CheckDirectoryAndFile(readConfigFile))
        {
            return;
        }

        string sourcePath = string.Empty;
        string targetPath = string.Empty;
        string fileName = string.Empty;

        if (readConfigFile.Length.Equals(12))
        {
            sourcePath = @"" + readConfigFile[1];
            targetPath = @"" + readConfigFile[3];
            fileName = readConfigFile[5];

            if (fileName[0].Equals('*') && fileName[1].Equals('.'))
            {
                string[] fileFolderList = Directory.GetFiles(sourcePath, fileName);

                foreach (string f in fileFolderList)
                {
                    // Remove path from the file name.
                    string fName = f.Substring(sourcePath.Length + 1);

                    bool isErrase = false;
                    string sourceFile = string.Empty;
                    string destFile = string.Empty;

                    logMessage.Add("----------------------------------------------------\r\n");
                    logMessage.Add($"Файл за архивиране: {fName}\r\n" +
                        $"Големина на файла за архивиране:" +
                        $" {new FileInfo(f).Length / 1024 / 1024} MB\r\n");

                    if (readConfigFile[9].ToLower().Equals("yes"))
                    {
                        DirectoryInfo di = new DirectoryInfo(sourcePath);
                        foreach (FileInfo fi in di.GetFiles())
                        {
                            //for specific file 
                            if (fi.ToString() == fName)
                            {
                                Compress(fi);
                            }
                        }

                        fName += ".gz";
                        fName = $"{(DateTime.Now)}_{fName})";
                        sourceFile = Path.Combine(sourcePath, fName);
                        destFile = Path.Combine(targetPath, fName);

                        logMessage.Add($"Компресиране на файла: {fName}\r\n" +
                            $"Големина на архивен файл: " +
                            $"{new FileInfo(sourceFile).Length / 1024 / 1024} MB\r\n");
                        try
                        {
                            //File.Copy(Path.Combine(sourceFile, destFile), Path.Combine(sourceFile, destFile), true);
                            File.Copy(sourceFile, destFile, true);
                            File.Delete(sourceFile);
                            logMessage.Add($"Компресирания файла \"{destFile}\" е успешно преместен в отдалечената папка.\r\n");
                            isErrase = true;
                        }
                        catch (Exception)
                        {
                            logMessage.Add("ВНИМАНИЕ! - Компресирания файла, не е преместен в отдалечената папка.\r\n");
                        }
                    }
                    else if (readConfigFile[9].ToLower().Equals("no"))
                    {
                        sourceFile = Path.Combine(sourcePath, fName);
                        destFile = Path.Combine(targetPath, fName);

                        try
                        {
                            File.Copy(sourceFile, destFile, true);
                            logMessage.Add($"Файла \"{destFile}\" е успешно копиран в отдалечената папка.\r\n");
                            isErrase = true;
                        }
                        catch (Exception)
                        {
                            logMessage.Add(
                                $"ВНИМАНИЕ! - Файла \"{destFile}\" не е копиран в отдалечената папка.\r\n");
                        }
                    }
                    else
                    {
                        logMessage.Add($"ВНИМАНИЕ! - Файла НЕ Е АРХИВИРАН!!!\r\n");
                        isErrase = false;
                    }

                    if (readConfigFile[11].ToLower().Equals("yes") && isErrase)
                    {
                        try
                        {
                            File.Delete(f);
                            logMessage.Add($"Файла \"{f}\" е успешно ИЗТРИТ от папката.\r\n");
                        }
                        catch (Exception)
                        {
                            logMessage.Add($"ВНИМАНИЕ! - Файла \"{f}\" НЕ Е ИЗТРИТ от папката.\r\n");
                        }
                    }
                }
            }
        }

        logMessage.Add("----------------------------------------------------\r\n");
        logMessage.Add($"Дата и час на преключване на архива: {DateTime.Now}\r\n");
        logMessage.Add("====================================================\r\n");
        //Test log file - create.
        string logFile = GetLogFile(readConfigFile, dirBackupConf);

        try
        {
            foreach (var item in logMessage)
            {
                File.AppendAllText(logFile, item);
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"ВНИМАНИЕ!!!-грешка в пътя до log.txt файла. - Редактирайте: {bacupConf}.");
            readConfigFile = File.ReadAllLines("backup.conf");
            Console.Write(string.Join(" ", readConfigFile));
            return;
        }

        CopyLogFileToSourcePath(logMessage, readConfigFile, sourcePath, logFile);

        CopyLogFileToTargetPath(logMessage, readConfigFile, targetPath, logFile);
    }

    private static void CopyLogFileToTargetPath(List<string> logMessage, string[] readConfigFile, string targetPath, string logFile)
    {
        try
        {
            File.Copy(logFile, Path.Combine(targetPath, readConfigFile[7]), true);
        }
        catch (Exception)
        {
            logMessage.Clear();

            File.AppendAllText(logFile, $"====================================================\r\n");
            File.AppendAllText(logFile, $"{logFile} - не-можа да бъде копиран в \"{targetPath}\" папка.\r\n");
            File.AppendAllText(logFile, $"====================================================\r\n");
        }
    }

    private static void CopyLogFileToSourcePath(List<string> logMessage, string[] readConfigFile, string sourcePath, string logFile)
    {
        try
        {
            File.Copy(logFile, Path.Combine(sourcePath, readConfigFile[7]), true);
        }
        catch (Exception)
        {
            logMessage.Clear();

            File.AppendAllText(logFile, $"====================================================\r\n");
            File.AppendAllText(logFile, $"{logFile} - не-можа да бъде копиран в \"{sourcePath}\" папка.\r\n");
            File.AppendAllText(logFile, $"====================================================\r\n");
        }
    }

    public static bool CheckDirectoryAndFile(string[] readConfigFile)
    {
        bool isExit = true;

        if (!Directory.Exists(readConfigFile[1]))
        {
            Console.WriteLine($"ВНИМАНИЕ! - Директорията \"{readConfigFile[1]}\" не е намерена.");
            isExit = false;
        }

        if (!Directory.Exists(readConfigFile[3]))
        {
            Console.WriteLine($"ВНИМАНИЕ! - Директорията \"{readConfigFile[1]}\"не е намерена.");
            isExit = false;
        }

        //if (!File.Exists(readConfigFile[7]))
        //{
        //    Console.WriteLine($"ВНИМАНИЕ! - Файле \"{readConfigFile[7]}\" не е намерен.");
        //    isExit = false;
        //}

        return isExit;
    }

    public static string GetLogFile(string[] readConfigFile, string dirBackupConf)
    {        
        string path = Path.Combine(dirBackupConf, readConfigFile[7]);

        try
        {
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine($"==============================================\r\n" +
                        $"e-mail: peter.g.georgiev@gmail.com\r\n" +
                        $"Create new log file {DateTime.Now}!\r\n" +
                        $"==============================================\r\n");
                    tw.Close();
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"ВНИМАНИЕ!!! - грешка в log.txt файла. - Няма коректно въвевени данни в {path}");
        }        

        return path;
    }

    private static string[] GetCorrectConfigFile(string[] readConfigFile, string bacupConf, string dirBacupConf)
    {
        if (!Directory.Exists(dirBacupConf))
        {
            Directory.CreateDirectory(dirBacupConf);
        }

        while (true)
        {
            try
            {
                readConfigFile = File.ReadAllText(Path.Combine(dirBacupConf, bacupConf))
                    .Split(new char[] { '"', '\r', '\n', ';', '=', ' ' },
                    StringSplitOptions.RemoveEmptyEntries)
                    .ToArray();
                break;

            }
            catch (Exception)
            {
                string path = Path.Combine(dirBacupConf, bacupConf);
                File.Create(path).Dispose();
                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine($"SourcePath = C:\\source;" +
                        $"\r\nTargetPath = D:\\Target;\r\n" +
                    "FileNameBackup = *.bak;\r\nFileLog = log.txt;" +
                    "\r\nzip = Yes;\r\nRemoveOrgFileNameBackup = Yes;");
                    tw.Close();
                }
            }
        }        

        return readConfigFile;
    }

    private static bool GetZipCorrect(string[] configFile)
    {
        if (configFile[9].ToLower().Equals("yes") || configFile[9].ToLower().Equals("no"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void Compress(FileInfo fi)
    {
        // Get the stream of the source file.
        using (FileStream inFile = fi.OpenRead())
        {
            // Prevent compressing hidden and 
            // already compressed files.
            if ((File.GetAttributes(fi.FullName)
                & FileAttributes.Hidden)
                != FileAttributes.Hidden & fi.Extension != ".gz")
            {
                // Create the compressed file.
                using (FileStream outFile =
                            File.Create(fi.FullName + ".gz"))
                {
                    using (GZipStream Compress =
                        new GZipStream(outFile,
                        CompressionMode.Compress))
                    {
                        // Copy the source file into 
                        // the compression stream.
                        inFile.CopyTo(Compress);

                        //Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                        //    fi.Name, fi.Length.ToString(), outFile.Length.ToString());
                    }
                }
            }
        }
    }
}