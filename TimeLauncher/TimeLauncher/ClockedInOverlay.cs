using System;
using System.Drawing;
using System.Windows.Forms;

namespace TimeLauncher
{
    public class ClockedInOverlay : Form
    {
        private Label lblMessage;

        public ClockedInOverlay(string projectName)
        {
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(200, 40);

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 10, screen.Bottom - this.Height - 10);

            lblMessage = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMessage);

            UpdateProject(projectName);
        }

        public void UpdateProject(string projectName)
        {
            lblMessage.Text = $"Clocked in: {projectName}";
        }
    }
}
