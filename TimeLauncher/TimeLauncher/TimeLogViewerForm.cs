using System;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting;
using System.Windows.Forms;
using TimeLauncher.Models;
using TimeLauncher.Services;

namespace TimeLauncher
{
    public class TimeLogViewerForm : Form
    {
        private RadioButton rbToday, rbWeek, rbAll, rbCustom;
        private DateTimePicker dtFrom, dtTo;
        private Button btnFilter;
        private ListView lvSummary;
        private Label lblWarning;

        private TextBox txtEditDuration;
        private int editingIndex = -1;

        public TimeLogViewerForm()
        {
            this.Text = "Time Log Summary";
            this.Size = new Size(1100, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            rbToday = new RadioButton { Text = "Today", Location = new Point(20, 20) };
            rbWeek = new RadioButton { Text = "This Week", Location = new Point(140, 20) };
            rbAll = new RadioButton { Text = "Entire History", Location = new Point(260, 20) };
            rbCustom = new RadioButton { Text = "Custom Range", Location = new Point(390, 20) };

            dtFrom = new DateTimePicker { Location = new Point(520, 20), Width = 120 };
            dtTo = new DateTimePicker { Location = new Point(650, 20), Width = 120 };

            btnFilter = new Button { Text = "Apply Filter", Location = new Point(780, 20) };
            btnFilter.Click += (s, e) => LoadFilteredLogs();

            lvSummary = new ListView
            {
                Location = new Point(20, 60),
                Size = new Size(1040, 440),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            lvSummary.Columns.Add("Project", 200);
            lvSummary.Columns.Add("Task", 150);
            lvSummary.Columns.Add("Clock-In", 150);
            lvSummary.Columns.Add("Clock-Out", 150);
            lvSummary.Columns.Add("Duration (hh:mm)", 120);
            lvSummary.Columns.Add("Notes", 250);

            lblWarning = new Label
            {
                ForeColor = Color.Red,
                Location = new Point(20, 520),
                Size = new Size(1040, 30)
            };

            this.Controls.AddRange(new Control[]
            {
                rbToday, rbWeek, rbAll, rbCustom,
                dtFrom, dtTo,
                btnFilter, lvSummary, lblWarning
            });

            txtEditDuration = new TextBox { Visible = false };
            txtEditDuration.Leave += TxtEditDuration_Leave;
            txtEditDuration.KeyDown += TxtEditDuration_KeyDown;
            this.Controls.Add(txtEditDuration);

            lvSummary.MouseDoubleClick += LvSummary_MouseDoubleClick;

            rbToday.Checked = true;
            LoadFilteredLogs();
        }

        private void LoadFilteredLogs()
        {
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MaxValue;

            if (rbToday.Checked)
            {
                start = DateTime.Today;
                end = DateTime.Today.AddDays(1).AddTicks(-1);
            }
            else if (rbWeek.Checked)
            {
                int diff = (int)DateTime.Today.DayOfWeek;
                start = DateTime.Today.AddDays(-diff);
                end = start.AddDays(7).AddTicks(-1);
            }
            else if (rbCustom.Checked)
            {
                start = dtFrom.Value.Date;
                end = dtTo.Value.Date.AddDays(1).AddTicks(-1);
            }

            var logs = LogService.LoadAllLogs(start, end);

            lvSummary.Items.Clear();
            lblWarning.Text = "";

            var overlaps = logs.OrderBy(l => l.ClockIn)
                .Select((log, i) => (log, i))
                .Where(pair =>
                {
                    var current = pair.log;
                    if (pair.i == 0) return false;
                    var previous = logs[pair.i - 1];
                    return current.ClockIn < previous.ClockOut;
                }).ToList();

            if (overlaps.Any())
            {
                lblWarning.Text = $"⚠️ Detected {overlaps.Count} overlapping time entries.";
            }

            foreach (var log in logs.OrderByDescending(l => l.ClockIn))
            {
                string task = string.IsNullOrWhiteSpace(log.TaskName) ? "(No Task)" : log.TaskName;
                string durationStr = $"{(int)log.Duration.TotalHours:D2}:{log.Duration.Minutes % 60:D2}";
                string clockInDisplay = log.ClockIn.ToString("g") + (log.IsEdited ? " (edited)" : "");

                string[] row = {
                    log.ProjectName,
                    task,
                    clockInDisplay,
                    log.ClockOut.ToString("g"),
                    durationStr,
                    log.Notes ?? ""
                };

                var item = new ListViewItem(row) { Tag = log };
                lvSummary.Items.Add(item);
            }
        }

        private void LvSummary_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var info = lvSummary.HitTest(e.Location);
            if (info.Item != null && info.SubItem != null)
            {
                int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);
                if (subItemIndex == 4) // Duration column
                {
                    Rectangle bounds = info.SubItem.Bounds;
                    txtEditDuration.Bounds = bounds;
                    txtEditDuration.Text = info.SubItem.Text;
                    txtEditDuration.Tag = info.Item;
                    txtEditDuration.Visible = true;
                    txtEditDuration.BringToFront();
                    txtEditDuration.Focus();
                    editingIndex = info.Item.Index;
                }
            }
        }

        private void TxtEditDuration_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyDurationEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        private TimeSpan RoundToMinutes(TimeSpan ts)
        {
            return TimeSpan.FromMinutes(Math.Round(ts.TotalMinutes));
        }

        private void TxtEditDuration_Leave(object sender, EventArgs e)
        {
            ApplyDurationEdit();
        }

        private void ApplyDurationEdit()
        {
            if (editingIndex < 0 || !(txtEditDuration.Tag is ListViewItem))
                return;

            var item = (ListViewItem)txtEditDuration.Tag;

            string input = txtEditDuration.Text.Trim();
            if (!TimeSpan.TryParse(input, out TimeSpan newDuration))
            {
                MessageBox.Show("Invalid duration format. Use HH:MM.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEditDuration.Visible = false;
                return;
            }

            var entry = item.Tag as TimeLogEntry;
            if (entry == null)
            {
                txtEditDuration.Visible = false;
                return;
            }

            // Adjust ClockIn based on new duration (keeping ClockOut fixed)
            // Calculate current duration
            TimeSpan currentDuration = entry.ClockOut - entry.ClockIn;

            // Only update if duration actually changed
            if (RoundToMinutes(currentDuration) != RoundToMinutes(newDuration))

            {
                entry.ClockIn = entry.ClockOut - newDuration;
                entry.IsEdited = true;

                // Update row values
                item.SubItems[2].Text = entry.ClockIn.ToString("g") + " (edited)";
                item.SubItems[4].Text = $"{(int)newDuration.TotalHours:D2}:{newDuration.Minutes % 60:D2}";

                LogService.SaveTimeEntry(entry);
            }


          

            // Persist change (optional: implement save or defer)
            LogService.SaveTimeEntry(entry); // You’ll need to ensure this method exists

            txtEditDuration.Visible = false;
            editingIndex = -1;
        }
    }
}
