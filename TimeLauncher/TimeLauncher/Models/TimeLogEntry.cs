using System;

namespace TimeLauncher.Models
{
    public class TimeLogEntry
    {
        public string ProjectName { get; set; }
        public string ProjectNumber { get; set; }
        public string TaskName { get; set; }
        public string TaskNumber { get; set; }
        public string Notes { get; set; }  // 🆕
        public bool IsEdited { get; set; } = false;


        public DateTime ClockIn { get; set; }
        public DateTime ClockOut { get; set; }
        public TimeSpan Duration => ClockOut - ClockIn;
    }
}
