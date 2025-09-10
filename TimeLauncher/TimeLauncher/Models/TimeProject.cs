using System.Collections.Generic;

namespace TimeLauncher.Models
{
    public class TimeProject
    {
        public string ProjectName { get; set; }
        public string ProjectNumber { get; set; }
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public bool IsArchived { get; set; } = false;

        public override string ToString() => ProjectName;
    }

    public class TaskItem
    {
        public string TaskName { get; set; }
        public string TaskNumber { get; set; }

        public override string ToString() => TaskName;
    }
}
