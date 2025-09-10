using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeLauncher.Models;

namespace TimeLauncher
{
    public class CreateProjectForm : Form
    {
        private readonly List<TimeProject> existingProjects;

        private TextBox txtProjectName;
        private TextBox txtProjectNumber;
        private Button btnAddTask;
        private Button btnCreate;
        private ListBox lstTasks;

        private List<TaskItem> tasks = new List<TaskItem>();

        public TimeProject CreatedProject { get; private set; }

        public CreateProjectForm(List<TimeProject> existingProjects)
        {
            this.existingProjects = existingProjects;

            this.Text = "Create New Time Project";
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Label lblName = new Label
            {
                Text = "Project Name*",
                Location = new Point(20, 20),
                Size = new Size(150, 20)
            };

            txtProjectName = new TextBox
            {
                Location = new Point(20, 45),
                Width = 300
            };

            Label lblNumber = new Label
            {
                Text = "Project Number (optional)",
                Location = new Point(20, 80),
                Size = new Size(200, 20)
            };

            txtProjectNumber = new TextBox
            {
                Location = new Point(20, 105),
                Width = 300
            };

            btnAddTask = new Button
            {
                Text = "Add Task",
                Location = new Point(20, 150),
                Size = new Size(100, 30)
            };
            btnAddTask.Click += BtnAddTask_Click;

            lstTasks = new ListBox
            {
                Location = new Point(20, 200),
                Size = new Size(430, 180)
            };

            btnCreate = new Button
            {
                Text = "Create Project",
                Location = new Point(20, 400),
                Size = new Size(150, 35)
            };
            btnCreate.Click += BtnCreate_Click;

            this.Controls.AddRange(new Control[]
            {
                lblName, txtProjectName,
                lblNumber, txtProjectNumber,
                btnAddTask, lstTasks,
                btnCreate
            });
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            using (var dialog = new TaskEntryDialog(tasks))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    tasks.Add(dialog.CreatedTask);
                    lstTasks.Items.Add(dialog.CreatedTask);
                }
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            string name = txtProjectName.Text.Trim();
            string number = txtProjectNumber.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Project name is required.");
                return;
            }

            CreatedProject = new TimeProject
            {
                ProjectName = name,
                ProjectNumber = number,
                Tasks = tasks
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
