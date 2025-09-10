using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using TimeLauncher; // Ensure TimeLauncherForm.cs is in the same solution

namespace ProjectLauncher
{
    public partial class Form1 : Form
    {
        private List<string> projects = new List<string>();
        private const string projectListPath = @"C:\Users\cbjorkhart\OneDrive - KPFF, Inc\Documents\KPFF Docs\Custom\Project List.txt";
        private const string projectFilesFolder = @"C:\Users\cbjorkhart\OneDrive - KPFF, Inc\Documents\KPFF Docs\Custom\Project Files\";

        private ComboBox comboBoxProjects;
        private Button buttonOpen;
        private Button buttonTimeClock;

        public Form1()
        {
            comboBoxProjects = new ComboBox() { Left = 20, Top = 20, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(comboBoxProjects);

            var buttonNewProject = new Button() { Text = "New Project", Left = 20, Top = 60, Width = 160 };
            buttonNewProject.Click += ButtonNewProject_Click;
            this.Controls.Add(buttonNewProject);

            buttonOpen = new Button() { Text = "Open All", Left = 220, Top = 60, Width = 160 };
            buttonOpen.Click += ButtonOpen_Click;
            this.Controls.Add(buttonOpen);

            var buttonViewProject = new Button() { Text = "Edit Project Files", Left = 20, Top = 120, Width = 160 };
            buttonViewProject.Click += ButtonViewProject_Click;
            this.Controls.Add(buttonViewProject);

            var buttonDeleteProject = new Button() { Text = "Delete Project", Left = 220, Top = 120, Width = 160 };
            buttonDeleteProject.Click += ButtonDeleteProject_Click;
            this.Controls.Add(buttonDeleteProject);

            buttonTimeClock = new Button() { Text = "Time Clock", Left = 20, Top = 180, Width = 360 };
            buttonTimeClock.Click += ButtonTimeClock_Click;
            this.Controls.Add(buttonTimeClock);

            this.Text = "Project Launcher";
            this.Width = 525;
            this.Height = 260;

            LoadProjectList();
        }

        private void LoadProjectList()
        {
            try
            {
                if (!File.Exists(projectListPath))
                {
                    MessageBox.Show($"Project list file not found:\n{projectListPath}");
                    return;
                }

                var lines = File.ReadAllLines(projectListPath);
                foreach (var line in lines)
                {
                    var projectName = line.Trim();
                    if (!string.IsNullOrEmpty(projectName))
                        projects.Add(projectName);
                }

                comboBoxProjects.Items.AddRange(projects.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading project list:\n{ex.Message}");
            }
        }

        private void ButtonOpen_Click(object sender, EventArgs e)
        {
            var selectedProject = comboBoxProjects.SelectedItem?.ToString();
            if (selectedProject == null)
            {
                MessageBox.Show("Please select a project.");
                return;
            }

            var fileListPath = Path.Combine(projectFilesFolder, $"{selectedProject}.files.txt");
            if (File.Exists(fileListPath))
            {
                var files = File.ReadAllLines(fileListPath);
                foreach (var file in files)
                {
                    var trimmed = file.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        TryOpen(trimmed);
                }
                this.Close();
            }

            OpenEdgeProjectTabs(selectedProject);
            this.Close();
        }

        private void ButtonViewProject_Click(object sender, EventArgs e)
        {
            var selectedProject = comboBoxProjects.SelectedItem?.ToString();
            if (selectedProject == null)
            {
                MessageBox.Show("Please select a project.");
                return;
            }

            var viewForm = new ViewProjectsForm(selectedProject);
            // Open as modeless so the main window can still be moved while editing
            viewForm.Show();
        }

        private void ButtonDeleteProject_Click(object sender, EventArgs e)
        {
            var selectedProject = comboBoxProjects.SelectedItem?.ToString();
            if (selectedProject == null)
            {
                MessageBox.Show("Please select a project.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete project \"{selectedProject}\" and all its references?",
                "Confirm Delete",
                MessageBoxButtons.YesNo);

            if (confirm == DialogResult.Yes)
            {
                projects.Remove(selectedProject);
                File.WriteAllLines(projectListPath, projects);

                var fileListPath = Path.Combine(projectFilesFolder, $"{selectedProject}.files.txt");
                if (File.Exists(fileListPath))
                    File.Delete(fileListPath);

                comboBoxProjects.Items.Clear();
                comboBoxProjects.Items.AddRange(projects.ToArray());

                MessageBox.Show($"Project \"{selectedProject}\" deleted.");
            }
        }

        private void ButtonNewProject_Click(object sender, EventArgs e)
        {
            string newProject = Interaction.InputBox("Enter new project name:", "New Project", "Project Name");
            if (string.IsNullOrWhiteSpace(newProject)) return;
            newProject = newProject.Trim();

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the base folder for the project.";
                if (folderDialog.ShowDialog() != DialogResult.OK) return;

                string baseFolder = folderDialog.SelectedPath;

                File.AppendAllText(projectListPath, newProject + Environment.NewLine);
                var fileListPath = Path.Combine(projectFilesFolder, $"{newProject}.files.txt");

                if (!File.Exists(fileListPath))
                    File.WriteAllText(fileListPath, baseFolder + Environment.NewLine); // Add base folder to project

                MessageBox.Show($"Project \"{newProject}\" created.");
                comboBoxProjects.Items.Add(newProject);
            }
        }

        private void ButtonTimeClock_Click(object sender, EventArgs e)
        {
            var timeForm = new TimeLauncherForm();
            timeForm.Show();
        }

        private void TryOpen(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                    return;
                }

                var fileName = Path.GetFileNameWithoutExtension(path);
                bool alreadyOpen = Process.GetProcesses().Any(p =>
                {
                    try { return p.MainWindowTitle.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0; }
                    catch { return false; }
                });

                if (alreadyOpen) return;

                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"File not found:\n{path}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open {path}\n{ex.Message}");
            }
        }

        private void OpenEdgeProjectTabs(string projectName)
        {
            try
            {
                var edgeBookmarksPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\User Data\Default\Bookmarks");

                if (!File.Exists(edgeBookmarksPath))
                    return;

                string json = File.ReadAllText(edgeBookmarksPath);
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var urls = new List<string>();

                    void Traverse(JsonElement node)
                    {
                        if (node.TryGetProperty("name", out var nameProp) &&
                            nameProp.GetString().Equals(projectName, StringComparison.OrdinalIgnoreCase) &&
                            node.TryGetProperty("children", out var children))
                        {
                            foreach (var child in children.EnumerateArray())
                            {
                                if (child.GetProperty("type").GetString() == "url")
                                    urls.Add(child.GetProperty("url").GetString());
                            }
                        }
                        else if (node.TryGetProperty("children", out var subChildren))
                        {
                            foreach (var child in subChildren.EnumerateArray())
                                Traverse(child);
                        }
                    }

                    var roots = root.GetProperty("roots");
                    foreach (var key in roots.EnumerateObject())
                        Traverse(key.Value);

                    if (urls.Count > 0)
                    {
                        var urlArgs = string.Join(" ", urls.Select(u => $"\"{u}\""));
                        Process.Start("msedge.exe", urlArgs);
                    }
                    else
                    {
                        foreach (var proc in Process.GetProcessesByName("msedge"))
                        {
                            try
                            {
                                PostMessage(proc.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Edge tabs:\n{ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_CLOSE = 0x0010;
    }
}
