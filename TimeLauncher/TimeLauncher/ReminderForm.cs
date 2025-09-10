using System;
using System.Drawing;
using System.Windows.Forms;

namespace TimeLauncher
{
    public class ReminderForm : Form
    {
        private Button btnYes;
        private Button btnNo;
        private Label lblMessage;
        private Timer timeoutTimer;
        public event Action<bool> ResponseReceived;

        public ReminderForm(string projectName)
        {
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.Size = new Size(300, 140);
            this.ShowInTaskbar = false;

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 10, screen.Bottom - this.Height - 10);

            lblMessage = new Label
            {
                Text = $"Are you still working on\n{projectName}?",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            btnYes = new Button { Text = "Yes", Width = 100, Location = new Point(40, 70) };
            btnNo = new Button { Text = "No", Width = 100, Location = new Point(160, 70) };

            btnYes.Click += (s, e) => HandleResponse(true);
            btnNo.Click += (s, e) => HandleResponse(false);

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);

            timeoutTimer = new Timer { Interval = 15 * 60 * 1000 }; // 15 minutes
            timeoutTimer.Tick += (s, e) => HandleResponse(false);
            timeoutTimer.Start();
        }

        private void HandleResponse(bool stillWorking)
        {
            timeoutTimer.Stop();
            this.Close();
            ResponseReceived?.Invoke(stillWorking);
        }
    }
}
