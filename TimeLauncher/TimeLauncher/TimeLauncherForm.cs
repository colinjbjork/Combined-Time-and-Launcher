using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;
using TimeLauncher.Services;



namespace TimeLauncher
{
    public class TimeLauncherForm : Form
    {
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
        private DateTime? clockInTime;
        private Label lblTrackingInfo; // 🆕
        private ClockedInOverlay clockedInOverlay; // 🆕 always-on-top reminder


        public TimeLauncherForm()
        {
            InitializeComponents();
            activeProjects = ProjectService.LoadProjects(); // <- Load persisted list
            RefreshProjectList(); // Populate UI
            LoadSessionIfAvailable();
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

            btnTimeIn = new Button { Text = "Time In", Location = new Point(240, 70), Size = new Size(90, 30) };
            btnTimeOut = new Button { Text = "Time Out", Location = new Point(340, 70), Size = new Size(90, 30) };
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
            }
        }

        // currentProject.IsArchived = true;                // ✅ Mark as archived
        //ProjectService.SaveProjects(activeProjects);     // ✅ Save updated list
          //  RefreshProjectList();

        private void LoadProjects()
        {
            activeProjects = ProjectService.LoadProjects();
            RefreshProjectList();
        }


        private void RefreshProjectList()
        {
            lstProjects.Items.Clear();
            foreach (var proj in activeProjects.Where(p => !p.IsArchived))
                lstProjects.Items.Add(proj);
        }


        private void LstProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentProject = lstProjects.SelectedItem as TimeProject;
            currentTask = null;

            cmbTasks.Items.Clear();
            cmbTasks.Items.Add(""); // empty for "no task"
            if (currentProject?.Tasks != null)
            {
                foreach (var task in currentProject.Tasks)
                    cmbTasks.Items.Add(task);
            }
            cmbTasks.SelectedIndex = 0;
        }

        private void CmbTasks_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentTask = cmbTasks.SelectedItem as TaskItem;
        }

        private void BtnTimeIn_Click(object sender, EventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("Please select a project.");
                return;
            }

            if (clockInTime != null)
            {
                MessageBox.Show("Already clocked in.");
                return;
            }

            clockInTime = DateTime.Now;
            timerElapsed.Start();
            hourlyPromptTimer.Start();

            SessionManager.SaveSession(new SessionManager.SessionData
            {
                ProjectName = currentProject.ProjectName,
                ProjectNumber = currentProject.ProjectNumber,
                TaskName = currentTask?.TaskName,
                TaskNumber = currentTask?.TaskNumber,
                ClockInTime = clockInTime.Value
            });
            UpdateTrackingLabel(); // 🆕

            if (clockedInOverlay == null || clockedInOverlay.IsDisposed)
                clockedInOverlay = new ClockedInOverlay(currentProject.ProjectName);
            else
                clockedInOverlay.UpdateProject(currentProject.ProjectName);
            clockedInOverlay.Show();

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

        private void ClockOut(bool manual)
        {
            if (clockInTime == null) return;

            var clockOutTime = DateTime.Now;

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
                ProjectName = currentProject?.ProjectName,
                ProjectNumber = currentProject?.ProjectNumber,
                TaskName = currentTask?.TaskName,
                TaskNumber = currentTask?.TaskNumber,
                ClockIn = clockInTime.Value,
                ClockOut = clockOutTime,
                Notes = notes // ✅ store notes
            };

            LogService.SaveLog(log);

            timerElapsed.Stop();
            hourlyPromptTimer.Stop();
            lblTimer.Text = "00:00:00";
            clockInTime = null;
            SessionManager.ClearSession();

            clockedInOverlay?.Close();
            clockedInOverlay = null;

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
            if (currentProject == null || clockInTime == null)
                return;

            var popup = new ReminderForm(currentProject.ProjectName);
            popup.ResponseReceived += HandleReminderResponse;
            popup.Show();
        }

        private void HandleReminderResponse(bool stillWorking)
        {
            if (!stillWorking && clockInTime != null)
            {
                Invoke(new Action(() =>
                {
                    MessageBox.Show("No response detected. Automatically clocking out.");
                    ClockOut(manual: false);
                }));
            }
        }

        private void BtnDeleteProject_Click(object sender, EventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("No project selected.");
                return;
            }

            currentProject.IsArchived = true;                // ✅ Mark as archived
            ProjectService.SaveProjects(activeProjects);     // ✅ Save updated list
            RefreshProjectList();                            // ✅ Refresh UI
        }


        private void TimeLauncherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (clockInTime != null)
            {
                this.Hide();
                e.Cancel = true;
            }
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

            currentProject = new TimeProject
            {
                ProjectName = session.ProjectName,
                ProjectNumber = session.ProjectNumber
            };
            currentTask = new TaskItem
            {
                TaskName = session.TaskName,
                TaskNumber = session.TaskNumber
            };
            clockInTime = session.ClockInTime;

            lblTimer.Text = (DateTime.Now - clockInTime.Value).ToString(@"hh\:mm\:ss");

            timerElapsed.Start();
            hourlyPromptTimer.Start();
            UpdateTrackingLabel(); // 🆕

            clockedInOverlay = new ClockedInOverlay(currentProject.ProjectName);
            clockedInOverlay.Show();

        }
        private void UpdateTrackingLabel() // 🆕
        {
            if (currentProject == null || clockInTime == null)
            {
                lblTrackingInfo.Text = "";
                return;
            }

            string text = $"Tracking: {currentProject.ProjectName}";
            if (!string.IsNullOrWhiteSpace(currentTask?.TaskName))
                text += $" - {currentTask.TaskName}";

            lblTrackingInfo.Text = text;
        }

    }
}
