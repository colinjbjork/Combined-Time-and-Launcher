using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;

namespace TimeLauncher
{
    public class ClockedInOverlay : Form
    {
        private readonly int collapsedHeight = 90;
        private readonly int expandedHeight = 260;

        private Label lblMessage;
        private Button btnToggle;
        private Panel accordionPanel;
        private ListBox lstProjects;
        private bool isExpanded;

        public event Action<TimeProject> ProjectSelected;

        public ClockedInOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(240, collapsedHeight);

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 10, screen.Bottom - this.Height - 10);

            btnToggle = new Button
            {
                Dock = DockStyle.Top,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Click += (s, e) => ToggleAccordion();

            lblMessage = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            lstProjects = new ListBox
            {
                Dock = DockStyle.Fill
            };
            lstProjects.DoubleClick += (s, e) => FireSelection();
            lstProjects.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    FireSelection();
                }
            };

            accordionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            accordionPanel.Controls.Add(lstProjects);

            this.Controls.Add(accordionPanel);
            this.Controls.Add(btnToggle);
            this.Controls.Add(lblMessage);

            UpdateToggleText();
        }

        public void UpdateProject(string projectName, bool isClockedIn)
        {
            if (string.IsNullOrWhiteSpace(projectName) || !isClockedIn)
                lblMessage.Text = "Not clocked in";
            else
                lblMessage.Text = $"Clocked in: {projectName}";
        }

        public void UpdateProjects(IEnumerable<TimeProject> projects, TimeProject currentProject)
        {
            if (projects == null)
                return;

            var ordered = projects.ToList();
            lstProjects.BeginUpdate();
            lstProjects.Items.Clear();
            foreach (var project in ordered)
            {
                lstProjects.Items.Add(project);
            }

            if (currentProject != null)
                lstProjects.SelectedItem = ordered.FirstOrDefault(p => ReferenceEquals(p, currentProject) ||
                                                                       (p.ProjectName?.Equals(currentProject.ProjectName, StringComparison.OrdinalIgnoreCase) == true));

            lstProjects.EndUpdate();
        }

        public void CollapseProjects()
        {
            if (isExpanded)
                ToggleAccordion();
        }

        private void ToggleAccordion()
        {
            isExpanded = !isExpanded;
            accordionPanel.Visible = isExpanded;
            UpdateToggleText();
            AdjustHeight();
        }

        private void UpdateToggleText()
        {
            btnToggle.Text = $"{(isExpanded ? "▾" : "▸")} Projects";
        }

        private void AdjustHeight()
        {
            int newHeight = isExpanded ? expandedHeight : collapsedHeight;
            int delta = newHeight - this.Height;
            this.Height = newHeight;
            this.Location = new Point(this.Location.X, this.Location.Y - delta);
        }

        private void FireSelection()
        {
            if (lstProjects.SelectedItem is TimeProject project)
            {
                ProjectSelected?.Invoke(project);
            }
        }
    }
}
