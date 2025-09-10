using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;

namespace TimeLauncher
{
    public class TaskEntryDialog : Form
    {
        private TextBox txtTaskName;
        private TextBox txtTaskNumber;
        private Button btnOK;
        private List<TaskItem> existingTasks;

        public TaskItem CreatedTask { get; private set; }

        public TaskEntryDialog(List<TaskItem> existingTasks)
        {
            this.existingTasks = existingTasks;

            this.Text = "Add Task";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblName = new Label { Text = "Task Name (optional)", Location = new Point(20, 20), Size = new Size(150, 20) };
            txtTaskName = new TextBox { Location = new Point(20, 45), Width = 300 };

            Label lblNumber = new Label { Text = "Task Number*", Location = new Point(20, 80), Size = new Size(150, 20) };
            txtTaskNumber = new TextBox { Location = new Point(20, 105), Width = 300 };

            btnOK = new Button { Text = "Add Task", Location = new Point(20, 150), Size = new Size(100, 30) };
            btnOK.Click += BtnOK_Click;

            this.Controls.AddRange(new Control[] {
                lblName, txtTaskName,
                lblNumber, txtTaskNumber,
                btnOK
            });
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string name = txtTaskName.Text.Trim();
            string number = txtTaskNumber.Text.Trim();

            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show("Task number is required.");
                return;
            }

            if (existingTasks.Any(t => t.TaskNumber == number))
            {
                MessageBox.Show("A task with this number already exists.");
                return;
            }

            CreatedTask = new TaskItem
            {
                TaskName = name,
                TaskNumber = number
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
