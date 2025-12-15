using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;
using TimeLauncher.Services;
using Microsoft.Win32;



namespace TimeLauncher
{
    public class TimeLauncherForm : Form
    {
        private const string OverheadProjectName = "overhead";

        private ListBox lstProjects;
        private ComboBox cmbTasks;
        private Button btnTimeIn;
        private Button btnTimeOut;
        private Button btnCreateProject;
        private Button btnDeleteProject;
        private Button btnAddTask;
        private Label lblTimer;
        private Timer timerElapsed;
        private Timer hourlyPromptTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private List<TimeProject> activeProjects;
        private TimeProject currentProject;
        private TaskItem currentTask;
        private TimeProject clockedInProject;
        private TaskItem clockedInTask;
        private DateTime? clockInTime;
        private Label lblTrackingInfo; // 🆕
        private ClockedInOverlay clockedInOverlay; // 🆕 always-on-top reminder
        private bool clockedOutDueToSuspend;


        public TimeLauncherForm()
        {
            InitializeComponents();
            activeProjects = ProjectService.LoadProjects(); // <- Load persisted list
            EnsureOverheadProject();
            RefreshProjectList(); // Populate UI
            InitializeOverlay();
            LoadSessionIfAvailable();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void InitializeComponents()
        {
            this.Text = "TimeLauncher";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += TimeLauncherForm_FormClosing;
            this.Resize += TimeLauncherForm_Resize;

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show Time Tracker", null, (s, e) => ShowFromTray());
            trayMenu.Items.Add("Clock Out", null, (s, e) => ClockOutFromTray());
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            Button btnViewToday = new Button { Text = "View Time Logs", Location = new Point(240, 300), Size = new Size(190, 30) };
            btnViewToday.Click += (s, e) =>
            {
                var viewer = new TimeLogViewerForm();
                viewer.ShowDialog();
            };
            this.Controls.Add(btnViewToday);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                ContextMenuStrip = trayMenu,
                Text = "TimeLauncher"
            };
            trayIcon.DoubleClick += (s, e) => ShowFromTray();

            lstProjects = new ListBox { Location = new Point(20, 20), Size = new Size(200, 250) };
            lstProjects.SelectedIndexChanged += LstProjects_SelectedIndexChanged;

            cmbTasks = new ComboBox { Location = new Point(240, 20), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTasks.SelectedIndexChanged += CmbTasks_SelectedIndexChanged;

            btnTimeIn = new Button { Text = "Clock In", Location = new Point(240, 70), Size = new Size(90, 30) };
            btnTimeOut = new Button { Text = "Clock Out", Location = new Point(340, 70), Size = new Size(90, 30) };
            btnCreateProject = new Button { Text = "Create Project", Location = new Point(240, 120), Size = new Size(190, 30) };
            Button btnRetrieveProject = new Button
            {
                Text = "Retrieve Project",
                Location = new Point(440, 160),
                Size = new Size(90, 30)
            };
            btnRetrieveProject.Click += BtnRetrieveProject_Click;
            this.Controls.Add(btnRetrieveProject);

            btnDeleteProject = new Button { Text = "Delete Project", Location = new Point(240, 160), Size = new Size(190, 30) };
            btnAddTask = new Button { Text = "Add Task", Location = new Point(440, 120), Size = new Size(90, 30) };
            btnAddTask.Click += BtnAddTask_Click;
            this.Controls.Add(btnAddTask);
            btnCreateProject.Click += BtnCreateProject_Click;

            btnTimeIn.Click += BtnTimeIn_Click;
            btnTimeOut.Click += BtnTimeOut_Click;
            ;

            btnDeleteProject.Click += BtnDeleteProject_Click;

            lblTimer = new Label { Text = "00:00:00", Location = new Point(240, 210), Size = new Size(150, 40), Font = new Font("Consolas", 18, FontStyle.Bold) };
            lblTrackingInfo = new Label // 🆕
            {
                Text = "",
                Location = new Point(240,240),
                Size = new Size(240,60),
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.Black,
                

            };
            this.Controls.Add(lblTrackingInfo); // 🆕

            timerElapsed = new Timer { Interval = 1000 };
            timerElapsed.Tick += TimerElapsed_Tick;

            hourlyPromptTimer = new Timer { Interval = 60 * 60 * 1000 };
            hourlyPromptTimer.Tick += HourlyPromptTimer_Tick;

            this.Controls.AddRange(new Control[]
            {
                lstProjects, cmbTasks, btnTimeIn, btnTimeOut, btnCreateProject, btnDeleteProject, lblTimer
            });

            



        }
        private void BtnRetrieveProject_Click(object sender, EventArgs e)
        {
            var archived = activeProjects.Where(p => p.IsArchived).ToList();
            if (!archived.Any())
            {
                MessageBox.Show("No archived projects to retrieve.");
                return;
            }

            var dialog = new Form
            {
                Text = "Retrieve Archived Project",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent
            };

            var lstArchived = new ListBox
            {
                Dock = DockStyle.Fill
            };
            lstArchived.Items.AddRange(archived.ToArray());

            var btnSelect = new Button
            {
                Text = "Restore Selected",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            btnSelect.Click += (s, args) =>
            {
                if (lstArchived.SelectedItem is TimeProject selected)
                {
                    selected.IsArchived = false;
                    ProjectService.SaveProjects(activeProjects);
                    RefreshProjectList();
                    UpdateOverlay();
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Please select a project to restore.");
                }
            };

            dialog.Controls.Add(lstArchived);
            dialog.Controls.Add(btnSelect);
            dialog.ShowDialog();
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("Please select a project first.");
                return;
            }

            using (var dialog = new TaskEntryDialog(currentProject.Tasks))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    currentProject.Tasks.Add(dialog.CreatedTask);

                    cmbTasks.Items.Clear();
                    cmbTasks.Items.Add(""); // re-add empty
                    foreach (var task in currentProject.Tasks)
                        cmbTasks.Items.Add(task);
                    ProjectService.SaveProjects(activeProjects);


                    cmbTasks.SelectedIndex = cmbTasks.Items.Count - 1;
                }
            }
        }
        private void BtnCreateProject_Click(object sender, EventArgs e)
        {
            var createForm = new CreateProjectForm(activeProjects);
            if (createForm.ShowDialog() == DialogResult.OK)
            {
                var newProject = createForm.CreatedProject;
                activeProjects.Add(newProject);
                ProjectService.SaveProjects(activeProjects);
                RefreshProjectList();
                UpdateOverlay();
            }
        }

        // currentProject.IsArchived = true;                // ✅ Mark as archived
        //ProjectService.SaveProjects(activeProjects);     // ✅ Save updated list
          //  RefreshProjectList();

        private void LoadProjects()
        {
            activeProjects = ProjectService.LoadProjects();
            EnsureOverheadProject();
            RefreshProjectList();
        }


        private void RefreshProjectList()
        {
            lstProjects.Items.Clear();
            foreach (var proj in GetOrderedProjects())
                lstProjects.Items.Add(proj);

            if (currentProject != null)
            {
                lstProjects.SelectedItem = currentProject;
            }

            UpdateOverlay();
        }


        private IEnumerable<TimeProject> GetOrderedProjects()
        {
            return activeProjects
                .Where(p => !p.IsArchived)
                .OrderByDescending(IsOverhead)
                .ThenBy(p => p.ProjectName, StringComparer.OrdinalIgnoreCase);
        }

        private void EnsureOverheadProject()
        {
            var overhead = activeProjects.FirstOrDefault(p => IsOverhead(p));
            var updated = false;
            if (overhead == null)
            {
                overhead = new TimeProject
                {
                    ProjectName = OverheadProjectName,
                    ProjectNumber = "0000"
                };
                activeProjects.Insert(0, overhead);
                updated = true;
            }

            if (overhead.IsArchived)
            {
                overhead.IsArchived = false;
                updated = true;
            }

            if (updated)
            {
                ProjectService.SaveProjects(activeProjects);
            }
        }


        private void LstProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyProjectSelection(lstProjects.SelectedItem as TimeProject, false);
        }

        private void CmbTasks_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentTask = cmbTasks.SelectedItem as TaskItem;
        }

        private void BtnTimeIn_Click(object sender, EventArgs e)
        {
            StartClockInForCurrentProject(showAlreadyClockedInMessage: true);

        }

        private void BtnTimeOut_Click(object sender, EventArgs e)
        {
            ClockOut(manual: true);
            lblTrackingInfo.Text = ""; // 🆕

        }

        private void ClockOutFromTray()
        {
            if (clockInTime != null)
                ClockOut(manual: false);
        }

        private void ClockOut(bool manual, DateTime? overrideClockOutTime = null)
        {
            if (clockInTime == null) return;

            var clockOutTime = overrideClockOutTime ?? DateTime.Now;
            var trackingProject = GetTrackingProject();
            var trackingTask = GetTrackingTask();

            string notes = "";
            if (manual)
            {
                var notesForm = new ClockOutNotesForm();
                if (notesForm.ShowDialog() == DialogResult.OK)
                {
                    notes = notesForm.Notes;
                }
            }

            var log = new TimeLogEntry
            {
                ProjectName = trackingProject?.ProjectName,
                ProjectNumber = trackingProject?.ProjectNumber,
                TaskName = trackingTask?.TaskName,
                TaskNumber = trackingTask?.TaskNumber,
                ClockIn = clockInTime.Value,
                ClockOut = clockOutTime,
                Notes = notes // ✅ store notes
            };

            LogService.SaveLog(log);

            timerElapsed.Stop();
            hourlyPromptTimer.Stop();
            lblTimer.Text = "00:00:00";
            clockInTime = null;
            clockedInProject = null;
            clockedInTask = null;
            SessionManager.ClearSession();

            UpdateTrackingLabel();
            UpdateOverlay();
            clockedInOverlay?.CollapseProjects();

            if (manual)
                MessageBox.Show($"Clocked out. Duration: {log.Duration.TotalMinutes:F1} minutes");
        }


        private void TimerElapsed_Tick(object sender, EventArgs e)
        {
            if (clockInTime != null)
            {
                var elapsed = DateTime.Now - clockInTime.Value;
                lblTimer.Text = elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        private void HourlyPromptTimer_Tick(object sender, EventArgs e)
        {
            var trackingProject = GetTrackingProject();

            if (trackingProject == null || clockInTime == null)
                return;

            var popup = new ReminderForm(trackingProject.ProjectName);
            popup.ResponseReceived += HandleReminderResponse;
            popup.Show();
        }

        private void HandleReminderResponse(bool stillWorking)
        {
            if (!stillWorking && clockInTime != null)
            {
                var autoClockOutTime = DateTime.Now;
                Invoke(new Action(() =>
                {
                    ClockOut(manual: false, overrideClockOutTime: autoClockOutTime);
                    MessageBox.Show("No response detected. Automatically clocking out.");
                }));
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (clockInTime != null)
                {
                    ClockOut(manual: false, overrideClockOutTime: DateTime.Now);
                    clockedOutDueToSuspend = true;
                }
            }
            else if (e.Mode == PowerModes.Resume)
            {
                if (clockedOutDueToSuspend)
                {
                    MessageBox.Show("System resumed. You were clocked out. Please clock in again.");
                    clockedOutDueToSuspend = false;
                }
            }
        }

        private void BtnDeleteProject_Click(object sender, EventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("No project selected.");
                return;
            }

            if (IsOverhead(currentProject))
            {
                MessageBox.Show("The overhead project cannot be deleted.");
                return;
            }

            currentProject.IsArchived = true;                // ✅ Mark as archived
            ProjectService.SaveProjects(activeProjects);     // ✅ Save updated list
            RefreshProjectList();                            // ✅ Refresh UI
            UpdateOverlay();
        }


        private void TimeLauncherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (clockInTime != null)
            {
                this.Hide();
                e.Cancel = true;
                return;
            }
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        }

        private void TimeLauncherForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void LoadSessionIfAvailable()
        {
            var session = SessionManager.LoadSession();
            if (session == null) return;

            currentProject = activeProjects
                .FirstOrDefault(p => string.Equals(p.ProjectName, session.ProjectName, StringComparison.OrdinalIgnoreCase))
                ?? new TimeProject
                {
                    ProjectName = session.ProjectName,
                    ProjectNumber = session.ProjectNumber
                };

            var sessionTaskName = session.TaskName;
            var sessionTaskNumber = session.TaskNumber;
            var restoredClockInTime = session.ClockInTime;

            ApplyProjectSelection(currentProject, true);

            clockInTime = restoredClockInTime;

            if (!string.IsNullOrWhiteSpace(sessionTaskName) && currentProject?.Tasks != null)
            {
                var matchingTask = currentProject.Tasks.FirstOrDefault(t => t.TaskName == sessionTaskName) ??
                                   currentProject.Tasks.FirstOrDefault(t => t.TaskNumber == sessionTaskNumber);
                if (matchingTask != null)
                {
                    currentTask = matchingTask;
                    cmbTasks.SelectedItem = matchingTask;
                }
            }

            lblTimer.Text = (DateTime.Now - clockInTime.Value).ToString(@"hh\:mm\:ss");

            timerElapsed.Start();
            hourlyPromptTimer.Start();
            UpdateTrackingLabel(); // 🆕

            clockedInProject = currentProject;
            clockedInTask = currentTask;

            clockedInOverlay.UpdateProjects(GetOrderedProjects(), GetTrackingProject());
            clockedInOverlay.UpdateProject(GetTrackingProject()?.ProjectName ?? OverheadProjectName, true);

        }
        private void UpdateTrackingLabel() // 🆕
        {
            var trackingProject = GetTrackingProject();
            var trackingTask = GetTrackingTask();

            if (trackingProject == null || clockInTime == null)
            {
                lblTrackingInfo.Text = "";
                return;
            }

            string text = $"Tracking: {trackingProject.ProjectName}";
            if (!string.IsNullOrWhiteSpace(trackingTask?.TaskName))
                text += $" - {trackingTask.TaskName}";

            lblTrackingInfo.Text = text;
        }

        private void ApplyProjectSelection(TimeProject project, bool updateListSelection)
        {
            currentProject = project;
            currentTask = null;

            if (updateListSelection && project != null)
                lstProjects.SelectedItem = project;

            cmbTasks.Items.Clear();
            cmbTasks.Items.Add("");
            if (currentProject?.Tasks != null)
            {
                foreach (var task in currentProject.Tasks)
                    cmbTasks.Items.Add(task);
            }
            cmbTasks.SelectedIndex = 0;

            if (clockInTime != null)
            {
                PersistSession();
            }

            UpdateTrackingLabel();
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (clockedInOverlay == null || clockedInOverlay.IsDisposed)
                return;

            var trackingProject = GetTrackingProject();
            clockedInOverlay.UpdateProjects(GetOrderedProjects(), trackingProject);
            clockedInOverlay.UpdateProject(trackingProject?.ProjectName ?? OverheadProjectName, clockInTime != null);
        }

        private void InitializeOverlay()
        {
            clockedInOverlay = new ClockedInOverlay();
            clockedInOverlay.ProjectSelected += ClockedInOverlay_ProjectSelected;
            UpdateOverlay();
            clockedInOverlay.Show();
        }

        private void ClockedInOverlay_ProjectSelected(TimeProject project)
        {
            if (project == null)
                return;

            bool switchingWhileClockedIn = clockInTime != null && currentProject != null &&
                                           !string.Equals(currentProject.ProjectName, project.ProjectName, StringComparison.OrdinalIgnoreCase);

            if (switchingWhileClockedIn)
            {
                ClockOut(manual: false);
            }

            ApplyProjectSelection(project, true);

            if (switchingWhileClockedIn)
            {
                StartClockInForCurrentProject(showAlreadyClockedInMessage: false);
            }
            clockedInOverlay.CollapseProjects();
        }

        private TimeProject GetOverheadProject()
        {
            return activeProjects.FirstOrDefault(IsOverhead);
        }

        private bool IsOverhead(TimeProject project)
        {
            return project != null && project.ProjectName != null &&
                   project.ProjectName.Equals(OverheadProjectName, StringComparison.OrdinalIgnoreCase);
        }

        private void PersistSession()
        {
            SessionManager.SaveSession(new SessionManager.SessionData
            {
                ProjectName = GetTrackingProject()?.ProjectName,
                ProjectNumber = GetTrackingProject()?.ProjectNumber,
                TaskName = GetTrackingTask()?.TaskName,
                TaskNumber = GetTrackingTask()?.TaskNumber,
                ClockInTime = clockInTime.Value
            });
        }

        private void StartClockInForCurrentProject(bool showAlreadyClockedInMessage)
        {
            if (currentProject == null)
            {
                var overhead = GetOverheadProject();
                ApplyProjectSelection(overhead, true);
            }

            if (currentProject == null)
            {
                if (showAlreadyClockedInMessage)
                    MessageBox.Show("Please select a project.");
                return;
            }

            if (clockInTime != null)
            {
                if (showAlreadyClockedInMessage)
                    MessageBox.Show("Already clocked in.");
                return;
            }

            clockInTime = DateTime.Now;
            clockedInProject = currentProject;
            clockedInTask = currentTask;
            timerElapsed.Start();
            hourlyPromptTimer.Start();

            PersistSession();
            UpdateTrackingLabel(); // 🆕

            UpdateOverlay();
        }

        private TimeProject GetTrackingProject()
        {
            return clockInTime != null ? (clockedInProject ?? currentProject) : currentProject;
        }

        private TaskItem GetTrackingTask()
        {
            return clockInTime != null ? (clockedInTask ?? currentTask) : currentTask;
        }

    }
}
