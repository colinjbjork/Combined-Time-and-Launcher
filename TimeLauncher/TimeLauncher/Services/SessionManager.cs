using System;
using System.IO;
using Newtonsoft.Json;
using TimeLauncher.Models;

namespace TimeLauncher.Services
{
    public static class SessionManager
    {
        private static string sessionPath = @"C:\Users\cbjorkhart\OneDrive - KPFF, Inc\Documents\KPFF Docs\Custom\TimeLauncher\SessionState.json";

        public class SessionData
        {
            public string ProjectName { get; set; }
            public string ProjectNumber { get; set; }
            public string TaskName { get; set; }
            public string TaskNumber { get; set; }
            public DateTime ClockInTime { get; set; }
        }

        public static void SaveSession(SessionData session)
        {
            var json = JsonConvert.SerializeObject(session, Formatting.Indented);
            File.WriteAllText(sessionPath, json);
        }

        public static SessionData LoadSession()
        {
            if (!File.Exists(sessionPath)) return null;
            var json = File.ReadAllText(sessionPath);
            return JsonConvert.DeserializeObject<SessionData>(json);
        }

        public static void ClearSession()
        {
            if (File.Exists(sessionPath))
                File.Delete(sessionPath);
        }

        public static bool IsSessionActive() => File.Exists(sessionPath);
    }
}
