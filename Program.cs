using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Updater
{
    internal class Program
    {
        private static WebClient FirstWebClient = new WebClient();
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if (args.Length > 0)
            {
                if (args[0] == "-help")
                {
                    for (int i = 0; i < ClassData.help.Length; i++) Console.WriteLine(ClassData.help[i]);
                    Environment.Exit(0);
                }
                if (args[0] == "-test")
                {
                    if (args.Length != 4) Environment.Exit(0);
                    Console.WriteLine("DownloadLink = " + args[1].ToString());
                    Console.WriteLine("InfoLink     = " + args[2].ToString());
                    Console.WriteLine("AppPath      = " + args[3].ToString());
                    Environment.Exit(0);
                }
            }
            // 0 - ссылка для загрузки файла
            // 1 - ссылка на информацию о файле
            // 2 - путь до заменяемого файла программы на диске
            if (args.Length != 3) Environment.Exit(0);
            string DownloadLink = args[0].ToString();
            string InfoLink = args[1].ToString();
            string[] PathParts = args[2].Split('\\').ToArray();
            string ApplicationName = PathParts[PathParts.Length - 1];
            string ApplicationPath = string.Empty;
            for (int i = 0; i < PathParts.Length - 1; i++)
            {
                ApplicationPath += PathParts[i];
                if (i < PathParts.Length - 2) ApplicationPath += "\\";
            }
            if (File.Exists(ApplicationName + ".upd"))
            {
                Console.WriteLine("A file with this name already exists in the download folder");
                Console.WriteLine("type DELETE to delete this file or EXIT to exit the program");
                string type = string.Empty;
                while (true)
                {
                    type = Console.ReadLine().ToString();
                    if (type == "DELETE")
                    {
                        File.Delete(ApplicationName + ".upd");
                        break;
                    }
                    if (type == "EXIT")
                    {
                        Environment.Exit(0);
                    }
                }
            }
            DownloadFile(DownloadLink, ApplicationName + ".upd");
            string OriginalSHA256 = ExtractOriginalHash(InfoLink);
            string LocalSHA256 = CalculateSHA256(ApplicationName + ".upd");
            while (OriginalSHA256.ToLower() != LocalSHA256.ToLower())
            {
                ClassData.attempt++;
                if (ClassData.attempt > 5)
                {
                    Console.WriteLine("Error: files checksums do not match");
                    Console.WriteLine("Perhaps the file is corrupted or the hash specified in the InfoLink file is incorrect");
                    Console.WriteLine("Press any key to exit the program");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                Console.WriteLine("Error: files checksums do not match");
                Console.WriteLine(ClassData.attempt.ToString() + "/5 attempt to download the file");
                File.Delete(ApplicationName + ".upd");
                DownloadFile(DownloadLink, ApplicationName + ".upd");
                LocalSHA256 = CalculateSHA256(ApplicationName + ".upd");
            }
            Console.WriteLine("Checksums of the files match");
            File.Delete(ApplicationPath + "\\" + ApplicationName);
            File.Move(ApplicationName + ".upd", ApplicationPath + "\\" + ApplicationName);
            Console.WriteLine("Update completed successfully");
            Console.WriteLine("After 5 seconds, the application will automatically start");
            System.Threading.Thread.Sleep(5000);
            ProcessStartInfo ApplicationStartInfo = new ProcessStartInfo(ApplicationPath + "\\" + ApplicationName);
            ApplicationStartInfo.Arguments = "-firstrun";
            Process Application = new Process();
            Application.StartInfo = ApplicationStartInfo;
            Application.Start();
            Environment.Exit(0);
        }
        private static void DownloadFile(string DwnldLnk, string AppName)
        {
            Console.WriteLine("File download started");
            FirstWebClient.DownloadFile(DwnldLnk, AppName);
            Console.WriteLine("File download completed");
        }

        private static string ExtractOriginalHash(string InfLnk)
        {
            Stream FirstStream = FirstWebClient.OpenRead(InfLnk);
            StreamReader FirstSR = new StreamReader(FirstStream);
            string[] InfoLines = new string[0];
            string TempLine = string.Empty;
            while ((TempLine = FirstSR.ReadLine()) != null)
            {
                Array.Resize(ref InfoLines, InfoLines.Length + 1);
                InfoLines[InfoLines.Length - 1] = TempLine;
                if (InfoLines[InfoLines.Length - 1].Contains("SHA256"))
                {
                    FirstStream.Close();
                    return InfoLines[InfoLines.Length - 1].Split('=')[1];
                }
            }
            FirstStream.Close();
            Console.WriteLine("Error: no line found in InfoLink file containing \"SHA256=\"");
            Console.WriteLine("Press any key to exit the program");
            Console.ReadKey();
            Environment.Exit(0);
            return string.Empty;
        }

        private static string CalculateSHA256(string path)
        {
            Console.WriteLine("File integrity check started");
            using (SHA256 FirstSHA256 = SHA256.Create())
            {
                using (FileStream FirstFileStream = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        FirstFileStream.Position = 0;
                        byte[] HashValue = FirstSHA256.ComputeHash(FirstFileStream);
                        return PrintByteArray(HashValue);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"I/O Exception: {e.Message}");
                        return string.Empty;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Access Exception: {e.Message}");
                        return string.Empty;
                    }
                }
            }

        }
        private static string PrintByteArray(byte[] array)
        {
            string hash = string.Empty;
            for (int i = 0; i < array.Length; i++)
            {
                hash += $"{array[i]:X2}";
            }
            return hash;
        }
    }
}
