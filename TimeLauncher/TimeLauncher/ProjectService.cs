using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TimeLauncher.Models;

namespace TimeLauncher.Services
{
    public static class ProjectService
    {
        private static string ProjectFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            @"KPFF Docs\Custom\TimeLauncher\projects.json");

        public static List<TimeProject> LoadProjects()
        {
            if (!File.Exists(ProjectFilePath))
                return new List<TimeProject>();

            try
            {
                var json = File.ReadAllText(ProjectFilePath);
                return JsonConvert.DeserializeObject<List<TimeProject>>(json) ?? new List<TimeProject>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load projects: " + ex.Message);
                return new List<TimeProject>();
            }
        }

        public static void SaveProjects(List<TimeProject> projects)
        {
            try
            {
                var json = JsonConvert.SerializeObject(projects, Formatting.Indented);
                File.WriteAllText(ProjectFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save projects: " + ex.Message);
            }
        }
    }
}
