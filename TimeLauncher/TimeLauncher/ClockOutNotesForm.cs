using System;
using System.Drawing;
using System.Windows.Forms;

namespace TimeLauncher
{
    public class ClockOutNotesForm : Form
    {
        public string Notes { get; private set; }

        public ClockOutNotesForm()
        {
            this.Text = "Clock Out Notes";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;

            var lbl = new Label
            {
                Text = "Optional Notes:",
                Location = new Point(10, 10),
                AutoSize = true
            };
            var txtNotes = new TextBox
            {
                Multiline = true,
                Location = new Point(10, 30),
                Size = new Size(360, 130),
                ScrollBars = ScrollBars.Vertical
            };
            var btnOK = new Button
            {
                Text = "Submit",
                DialogResult = DialogResult.OK,
                Location = new Point(280, 170),
                Size = new Size(90, 30)
            };

            btnOK.Click += (s, e) => Notes = txtNotes.Text;

            this.Controls.Add(lbl);
            this.Controls.Add(txtNotes);
            this.Controls.Add(btnOK);
        }
    }
}
