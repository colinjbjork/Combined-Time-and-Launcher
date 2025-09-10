using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;

namespace TimeLauncher.Services
{
    public static class LogService
    {
        private static string basePath = @"C:\Users\cbjorkhart\OneDrive - KPFF, Inc\Documents\KPFF Docs\Custom\TimeLauncher";
        private static List<TimeLogEntry> LoadLogFile(string path)
        {
            if (!File.Exists(path)) return new List<TimeLogEntry>();

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<TimeLogEntry>>(json) ?? new List<TimeLogEntry>();
        }
        private static List<TimeLogEntry> LoadLogsFromFile(string path)
        {
            if (!File.Exists(path)) return new List<TimeLogEntry>();

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<TimeLogEntry>>(json) ?? new List<TimeLogEntry>();
        }

        public static void SaveLog(TimeLogEntry entry)
        {
            string logPath = GetCurrentLogPath();
            List<TimeLogEntry> logs = LoadLogsFromFile(logPath);
            logs.Add(entry);

            string json = JsonConvert.SerializeObject(logs, Formatting.Indented);
            File.WriteAllText(logPath, json);
        }

        public static List<TimeLogEntry> LoadAllLogs()
        {
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            List<TimeLogEntry> allLogs = new List<TimeLogEntry>();
            foreach (var file in Directory.GetFiles(basePath, "TimeLog_*.json"))
            {
                var logs = LoadLogsFromFile(file);
                allLogs.AddRange(logs);
            }
            return allLogs;
        }

        public static List<TimeLogEntry> LoadAllLogs(DateTime from, DateTime to)
        {
            var result = new List<TimeLogEntry>();

            if (!Directory.Exists(basePath))
                return result;

            foreach (var file in Directory.GetFiles(basePath, "*.json"))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var logs = JsonConvert.DeserializeObject<List<TimeLogEntry>>(content);

                    if (logs != null)
                    {
                        foreach (var log in logs)
                        {
                            if (log.ClockIn >= from && log.ClockOut <= to)
                                result.Add(log);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read file {file}: {ex.Message}");
                }
            }

            return result.OrderBy(l => l.ClockIn).ToList();
        }

        

        private static void SaveLogFile(string path, List<TimeLogEntry> entries)
        {
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void SaveTimeEntry(TimeLogEntry updatedEntry)
        {
            string logFolder = GetLogFolder();
            var logFiles = Directory.GetFiles(logFolder, "*.json");

            foreach (var file in logFiles)
            {
                var entries = LoadLogFile(file);

                var match = entries.FirstOrDefault(e =>
                    e.ClockOut == updatedEntry.ClockOut &&
                    e.ProjectName == updatedEntry.ProjectName &&
                    e.TaskName == updatedEntry.TaskName);

                if (match != null)
                {
                    match.ClockIn = updatedEntry.ClockIn;
                    match.IsEdited = true;

                    SaveLogFile(file, entries);
                    return;
                }
            }

            MessageBox.Show("Time entry not found to update.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static string GetLogFolder()
        {
            string baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "KPFF Docs",
                "Custom",
                "TimeLauncher"
            );
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        public static List<TimeLogEntry> LoadAllLogsForDate(DateTime date)
        {
            var all = LoadAllLogs();
            return all.Where(l => l.ClockIn.Date == date.Date).ToList();
        }

        private static string GetCurrentLogPath()
        {
            var now = DateTime.Now;
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            string fileName = $"TimeLog_{startOfWeek:yyyy-MM-dd}_to_{endOfWeek:yyyy-MM-dd}.json";
            return Path.Combine(basePath, fileName);
        }
    }
}
