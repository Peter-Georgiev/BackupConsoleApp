using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading;

class backup
{   
    static void Main()
    {
        //-----------------------------RELEASE 1.01-----------------------------//
        List<string> logMessage = new List<string>();
        
        string[] readConfigFile = new string[0];
        string bacupConf = "backup.conf";
        string dirBackupConf = @"C:\BackupConfAndLog\";

        readConfigFile = GetCorrectConfigFile(readConfigFile, bacupConf, dirBackupConf);

        byte bacupCount = 0;

        try
        {
             bacupCount = byte.Parse(readConfigFile[7]);
        }
        catch (Exception)
        {
            Console.WriteLine($"Некоректно попълнен файл: \"{bacupConf}\".\r\n" +
                $"В случай че, незнаете какво да наравите - изтрийте файл: \"{bacupConf}\".\r\n" +
                $"Файла ще се създаде автоматично с примерно попълнени данни.");            
        }               

        string sourcePath = string.Empty;
        string targetPath = string.Empty;

        Console.WriteLine($"...................!!!...МОЛЯ, ИЗЧАКАЙТЕ...!!!...................");

        for (byte count = 0; count < bacupCount; count++)
        {
            bool hasExitApp = true;
            logMessage.Add("===============================================================\r\n");
            logMessage.Add($"Дата и час на архивиране: {DateTime.Now}\r\n");
            logMessage.Add("---------------------------------------------------------------\r\n");

            if (!CheckDirectoryAndFile(logMessage, readConfigFile, count))
            {
                hasExitApp = false;
            }

            if (readConfigFile[count * 6 + 13].Equals("*") && hasExitApp)
            {
                CopyFolderContents(logMessage, readConfigFile, count);
            }
            else if (hasExitApp)
            {
                CopyAndZipFile(logMessage, readConfigFile, bacupCount, ref sourcePath, ref targetPath, count);
            }

            logMessage.Add("---------------------------------------------------------------\r\n");
            logMessage.Add($"Дата и час на преключване на архива: {DateTime.Now}\r\n");
            logMessage.Add("===============================================================\r\n");
            //Test log file - create.
            string logFile = GetSizeLogFle(readConfigFile, dirBackupConf);

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
                readConfigFile = File.ReadAllLines(bacupConf);
                Console.Write(string.Join(" ", readConfigFile));
                return;
            }

            CopyLogFileToSourcePath(logMessage, readConfigFile, count, logFile);

            CopyLogFileToTargetPath(logMessage, readConfigFile, count, logFile);

            logMessage.Clear();
        }
    }

    private static void CopyAndZipFile(List<string> logMessage, string[] readConfigFile, byte bacupCount, ref string sourcePath, ref string targetPath, byte count)
    {
        if (readConfigFile.Length.Equals((bacupCount * 6) + 7 + 1))
        {
            sourcePath = @"" + readConfigFile[count * 6 + 9];
            targetPath = @"" + readConfigFile[count * 6 + 11];

            string[] fileFolderList = Directory.GetFiles(sourcePath, readConfigFile[count * 6 + 13]);

            foreach (string f in fileFolderList)
            {
                // Remove path from the file name.
                string fName = f.Substring(sourcePath.Length + 1);

                bool isErrase = false;
                string sourceFile = string.Empty;
                string destFile = string.Empty;

                logMessage.Add("--------------------------------------------------------------\r\n");
                logMessage.Add($"Файл за архивиране: {fName}\r\n" +
                    $"Големина на файла за архивиране:" +
                    $" {(double)new FileInfo(f).Length / 1024d / 1024d:F2} MB\r\n");

                //readConfigFile[3] - zip = Yes;
                if (readConfigFile[3].ToLower().Equals("yes"))
                {   //readConfigFile[3] - zip = Yes;
                    DirectoryInfo di = new DirectoryInfo(sourcePath);
                    foreach (FileInfo fi in di.GetFiles())
                    {
                        if (fi.ToString() == fName)
                        {
                            Compress(fi);
                        }
                    }

                    fName += ".gz";

                    sourceFile = Path.Combine(sourcePath, fName);

                    logMessage.Add($"Компресиране на файла: {fName}\r\n" +
                        $"Големина на архивен файл: " +
                        $"{(double)new FileInfo(sourceFile).Length / 1024d / 1024d:F2} MB\r\n");

                    try
                    {
                        fName = $"{DateTime.Now.ToString("dd.MM.yy")}" +
                        $"_{DateTime.Now.ToString("HH.mm")}_" +
                        $"{fName}";

                        destFile = Path.Combine(targetPath, fName);

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
                else if (readConfigFile[3].ToLower().Equals("no"))
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

                //readConfigFile[5] - RemoveOrgFileNameBackup = Yes;
                if (readConfigFile[5].ToLower().Equals("yes") && isErrase)
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

    public static void CopyFolderContents(List<string> logMessage, string[] readConfigFile, byte count)
    {
        string sourceFolder = @"" + readConfigFile[count * 6 + 9];
        string targetPath = @"" + readConfigFile[count * 6 + 11];

        logMessage.Add($"Създаване на архивна папка с дата и час в: \"{targetPath}\"\r\n");

        DelayAction(1000);

        string newDir = $"Backup_{DateTime.Now.ToString("dd.MM.yy")}" +
                        $"_{DateTime.Now.ToString("HH.mm.ss")}";

        string targetFolder = @"" + targetPath + "\\" + newDir;

        try
        {
            DirectoryInfo di = Directory.CreateDirectory(targetFolder);

            logMessage.Add($"Успешно създадена архивна папка: \"{targetFolder}\"\r\n");
        }
        catch (Exception)
        {
            logMessage.Add($"Проблем при създаване на архивна папка: \"{targetFolder}\"\r\n");
        }

        try
        {
            logMessage.Add($"Копиране на папка \"{sourceFolder}\" в \"{targetFolder}\".\r\n");

            if (Directory.Exists(sourceFolder))
            {
                // Copy folder structure
                foreach (string sourceSubFolder in Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(sourceSubFolder.Replace(sourceFolder, targetFolder));
                }
                // Copy files
                foreach (string sourceFile in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    string destinationFile = sourceFile.Replace(sourceFolder, targetFolder);
                    File.Copy(sourceFile, destinationFile, true);
                }
            }

            logMessage.Add($"Успешно копиране на папка \"{sourceFolder}\" в \"{targetFolder}\".\r\n");
        }
        catch (Exception)
        {
            logMessage.Add($"ВНИМАНИЕ! - Папката \"{targetFolder}\" не е копиран в отдалечената папка.\r\n");
        }

    }

    private static string GetSizeLogFle(string[] readConfigFile, string dirBackupConf)
    {   //readConfigFile[1] - FileLog = log.txt;
        string logFile = Path.Combine(dirBackupConf, readConfigFile[1]); 

        try
        {
            if ((double)new FileInfo(logFile).Length / 1024d / 1024d > 2.00)
            {
                string data = DateTime.Now.ToString("dd.MM.yy");
                string time = DateTime.Now.ToString("HH.mm");
                dirBackupConf = Path.Combine(dirBackupConf, data + "_" + time + "_" + readConfigFile[1]);

                File.Move(logFile, dirBackupConf);
            }
        }
        catch (Exception)
        {
            logFile = GetLogFile(readConfigFile, dirBackupConf);
        }     

        return logFile;
    }

    private static void CopyLogFileToTargetPath(List<string> logMessage, string[] readConfigFile, byte count, string logFile)
    {
        string targetPath = @"" + readConfigFile[count * 6 + 11];

        try
        {   //readConfigFile[1] - FileLog = log.txt;
            File.Copy(logFile, Path.Combine(targetPath, readConfigFile[1]), true);
        }
        catch (Exception)
        {
            logMessage.Clear();

            File.AppendAllText(logFile, $"===============================================================\r\n");
            File.AppendAllText(logFile, $"Дата и час на грешката: {DateTime.Now}\r\n");
            File.AppendAllText(logFile, $"{logFile} - не-можа да бъде копиран в \"{targetPath}\" папка.\r\n");
            File.AppendAllText(logFile, $"===============================================================\r\n");
        }
    }

    private static void CopyLogFileToSourcePath(List<string> logMessage, string[] readConfigFile, byte count, string logFile)
    {
        string sourcePath = @"" + readConfigFile[count * 6 + 9];

        try
        {   //readConfigFile[1] - FileLog = log.txt;
            File.Copy(logFile, Path.Combine(sourcePath, readConfigFile[1]), true);
        }
        catch (Exception)
        {
            logMessage.Clear();

            File.AppendAllText(logFile, $"===============================================================\r\n");
            File.AppendAllText(logFile, $"Дата и час на грешката: {DateTime.Now}\r\n");
            File.AppendAllText(logFile, $"{logFile} - не-можа да бъде копиран в \"{sourcePath}\" папка.\r\n");
            File.AppendAllText(logFile, $"===============================================================\r\n");
        }
    }

    public static bool CheckDirectoryAndFile(List<string> logMessage, string[] readConfigFile, byte count)
    {
        string sourcePath = @"" + readConfigFile[count * 6 + 9];
        string targetPath = @"" + readConfigFile[count * 6 + 11];

        bool isExit = true;

        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine($"ВНИМАНИЕ! - Директорията \"{sourcePath}\" не е намерена.");
            logMessage.Add($"ВНИМАНИЕ! - Директорията \"{sourcePath}\" не е намерена.\r\n");
            isExit = false;
        }

        if (!Directory.Exists(targetPath))
        {
            logMessage.Add($"ВНИМАНИЕ! - Директорията \"{targetPath}\" не е намерена.\r\n");
            Console.WriteLine($"ВНИМАНИЕ! - Директорията \"{targetPath}\" не е намерена.");
            isExit = false;
        }

        return isExit;
    }

    public static string GetLogFile(string[] readConfigFile, string dirBackupConf)
    {         //readConfigFile[1] - FileLog = log.txt;
        string logFile = Path.Combine(dirBackupConf, readConfigFile[1]);

        try
        {
            if (!File.Exists(logFile))
            {
                File.Create(logFile).Dispose();
                using (TextWriter tw = new StreamWriter(logFile))
                {
                    tw.WriteLine($"===============================================================\r\n" +
                        $"e-mail: peter.g.georgiev@gmail.com\r\n" +
                        $"Release 1.01\r\n" +
                        $"Create new log file {DateTime.Now}!\r\n" +
                        $"===============================================================\r\n");
                    tw.Close();
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"ВНИМАНИЕ!!! - грешка в log.txt файла. - Няма коректно въвевени данни в {logFile}");
        }        

        return logFile;
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
                    tw.WriteLine(
                        $"FileLog = log.txt;\r\n" +
                        $"zip(формат_gz) = Yes;\r\n" +
                        $"RemoveOrgFileNameBackup(Внимание!) = NO;\r\n" +
                        $"BackupCount = 2;\r\n" +
                        $"SourcePath_1 = C:\\1_source;\r\n" +
                        $"TargetPath_1 = D:\\Target;\r\n" +
                        $"FileNameBackup_1 = *.bak;\r\n" +
                        $"SourcePath_2 = C:\\2_source;\r\n" +
                        $"TargetPath_2 = D:\\Target;\r\n" +
                        $"FileNameBackup_2 = file.bak;"
                        );
                    tw.Close();
                }
            }
        }        

        return readConfigFile;
    }

    private static bool GetZipCorrect(string[] configFile)
    {
        //configFile[3] - zip = yes;
        if (configFile[3].ToLower().Equals("yes") || configFile[3].ToLower().Equals("no"))
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
                    }
                }
            }
        }
    }
    
    public static void DelayAction(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }
}