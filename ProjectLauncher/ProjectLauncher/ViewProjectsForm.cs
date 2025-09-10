using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProjectLauncher
{
    public class ViewProjectsForm : Form
    {
        private readonly string selectedProject;
        private readonly string projectFilesFolder = @"C:\Users\cbjorkhart\OneDrive - KPFF, Inc\Documents\KPFF Docs\Custom\Project Files\";

        private CheckedListBox checkedListBoxFiles;
        private Button buttonLaunchSelected;
        private Button buttonRemove;
        private Button buttonAddPath;
        private Button buttonUndo;               // NEW (replaces Save)
        private Label labelTitle;

        // Session baseline + autosave control
        private List<string> sessionBaseline = new List<string>();
        private bool suppressAutosave = false;

        // Context menu
        private ContextMenuStrip ctxMenu;
        private ToolStripMenuItem ctxOpenItem;
        private ToolStripMenuItem ctxRemoveItem;
        private int contextIndex = -1; // index under mouse on right-click

        public ViewProjectsForm(string projectName)
        {
            selectedProject = projectName;
            InitializeComponent();
            InitializeContextMenu();
            LoadFileList();
            CaptureSessionBaseline();
        }

        private string FileListPath => Path.Combine(projectFilesFolder, $"{selectedProject}.files.txt");

        private void InitializeComponent()
        {
            this.Text = "Edit Project Files";
            this.Width = 700;
            this.Height = 540;

            labelTitle = new Label()
            {
                Text = $"Editing files for project: {selectedProject}",
                Left = 10,
                Top = 10,
                Width = 650
            };
            this.Controls.Add(labelTitle);

            checkedListBoxFiles = new CheckedListBox()
            {
                Left = 10,
                Top = 40,
                Width = 660,
                Height = 360,
                CheckOnClick = true
            };
            // Right-click detection for per-item context menu
            checkedListBoxFiles.MouseDown += CheckedListBoxFiles_MouseDown;
            this.Controls.Add(checkedListBoxFiles);

            buttonLaunchSelected = new Button()
            {
                Text = "Launch Selected",
                Left = 10,
                Top = 410,
                Width = 150
            };
            buttonLaunchSelected.Click += ButtonLaunchSelected_Click;
            this.Controls.Add(buttonLaunchSelected);

            buttonRemove = new Button()
            {
                Text = "Remove Project Files",
                Left = 180,
                Top = 410,
                Width = 160
            };
            buttonRemove.Click += ButtonRemove_Click;
            this.Controls.Add(buttonRemove);

            // REPLACED Save with Undo
            buttonUndo = new Button()
            {
                Text = "Undo",
                Left = 360,
                Top = 410,
                Width = 150
            };
            buttonUndo.Click += ButtonUndo_Click;
            this.Controls.Add(buttonUndo);

            buttonAddPath = new Button()
            {
                Text = "Add File from Path",
                Left = 530,
                Top = 410,
                Width = 140
            };
            buttonAddPath.Click += ButtonAddPath_Click;
            this.Controls.Add(buttonAddPath);

            // Drag & drop (unchanged behavior)
            this.AllowDrop = true;
            this.DragEnter += ViewProjectsForm_DragEnter;
            this.DragDrop += ViewProjectsForm_DragDrop;
        }

        private void InitializeContextMenu()
        {
            ctxMenu = new ContextMenuStrip();

            ctxOpenItem = new ToolStripMenuItem("Open File");
            ctxOpenItem.Click += (s, e) => OpenItemAtContextIndex();

            ctxRemoveItem = new ToolStripMenuItem("Remove file from list");
            ctxRemoveItem.Click += (s, e) => RemoveItemAtContextIndex();

            ctxMenu.Items.AddRange(new ToolStripItem[] { ctxOpenItem, ctxRemoveItem });
        }

        private void CaptureSessionBaseline()
        {
            sessionBaseline = GetAllItems();
        }

        private void LoadFileList()
        {
            suppressAutosave = true;
            try
            {
                checkedListBoxFiles.Items.Clear();

                if (File.Exists(FileListPath))
                {
                    string[] files = File.ReadAllLines(FileListPath);
                    foreach (string file in files)
                    {
                        if (!string.IsNullOrWhiteSpace(file))
                            checkedListBoxFiles.Items.Add(file.Trim(), true);
                    }
                }
            }
            finally
            {
                suppressAutosave = false;
            }
        }

        // ===== Persistence helpers =====
        private List<string> GetAllItems()
        {
            return checkedListBoxFiles.Items.Cast<object>()
                                            .Select(o => o.ToString())
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .ToList();
        }

        private void SaveFileList()
        {
            if (suppressAutosave) return;

            var allItems = GetAllItems();
            Directory.CreateDirectory(Path.GetDirectoryName(FileListPath));
            File.WriteAllLines(FileListPath, allItems);
        }

        // ===== Context menu interactions =====
        private void CheckedListBoxFiles_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            int index = checkedListBoxFiles.IndexFromPoint(e.Location);
            if (index >= 0 && index < checkedListBoxFiles.Items.Count)
            {
                contextIndex = index;
                checkedListBoxFiles.SelectedIndex = index; // highlight the item
                ctxMenu.Show(checkedListBoxFiles, e.Location);
            }
            else
            {
                contextIndex = -1;
            }
        }

        private void OpenItemAtContextIndex()
        {
            if (contextIndex < 0 || contextIndex >= checkedListBoxFiles.Items.Count) return;
            var path = checkedListBoxFiles.Items[contextIndex].ToString();
            OpenPath(path);
        }

        private void RemoveItemAtContextIndex()
        {
            if (contextIndex < 0 || contextIndex >= checkedListBoxFiles.Items.Count) return;
            checkedListBoxFiles.Items.RemoveAt(contextIndex);
            SaveFileList(); // autosave after removal
        }

        // ===== Existing buttons (same behavior), with autosave where list changes =====
        private void ButtonLaunchSelected_Click(object sender, EventArgs e)
        {
            foreach (string path in checkedListBoxFiles.CheckedItems)
            {
                OpenPath(path);
            }
            this.Close();
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            // Existing behavior: remove UNCHECKED items
            for (int i = checkedListBoxFiles.Items.Count - 1; i >= 0; i--)
            {
                if (!checkedListBoxFiles.GetItemChecked(i))
                {
                    checkedListBoxFiles.Items.RemoveAt(i);
                }
            }

            // NEW: autosave immediately after list change
            SaveFileList();
        }

        private void ButtonAddPath_Click(object sender, EventArgs e)
        {
            string inputPath = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter full file path or URL (including SharePoint links):",
                "Add File from Path",
                "");

            if (!string.IsNullOrWhiteSpace(inputPath))
            {
                string trimmed = inputPath.Trim();
                if (!checkedListBoxFiles.Items.Contains(trimmed))
                {
                    checkedListBoxFiles.Items.Add(trimmed, true);

                    // NEW: autosave immediately after list change
                    SaveFileList();
                }
            }
        }

        // ===== Drag & Drop (unchanged behavior + autosave) =====
        private void ViewProjectsForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void ViewProjectsForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            bool changed = false;

            foreach (var path in dropped)
            {
                if (!checkedListBoxFiles.Items.Contains(path))
                {
                    checkedListBoxFiles.Items.Add(path, true);
                    changed = true;
                }
            }

            if (changed)
                SaveFileList(); // NEW: autosave after list change
        }

        // ===== Undo (session-scoped) =====
        private void ButtonUndo_Click(object sender, EventArgs e)
        {
            try
            {
                suppressAutosave = true;

                checkedListBoxFiles.BeginUpdate();
                checkedListBoxFiles.Items.Clear();

                foreach (var path in sessionBaseline)
                    checkedListBoxFiles.Items.Add(path, true);
            }
            finally
            {
                checkedListBoxFiles.EndUpdate();
                suppressAutosave = false;
            }

            // Persist restored baseline
            SaveFileList();
        }

        // ===== Open helper (unchanged semantics) =====
        private void OpenPath(string path)
        {
            try
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else if (Directory.Exists(path))
                {
                    // Fix for OneDrive / shortcut folder paths:
                    Process.Start("explorer.exe", $"/root,\"{path}\"");
                }
                else if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"Path not found:\n{path}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open:\n{path}\n\n{ex.Message}", "Open File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
