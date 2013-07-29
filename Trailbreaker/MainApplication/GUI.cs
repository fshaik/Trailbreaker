using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace Trailbreaker.MainApplication
{
    internal class GUI : TrailbreakerReceiverForm
    {
        public static string testName = "MyDescriptiveTestName";
        private readonly List<UserAction> actions = new List<UserAction>();
        private readonly MenuItem enterTestName = new MenuItem("Enter Test Name...");

        private readonly MenuItem fileMenu = new MenuItem("File");
        private readonly DataGridView grid = new DataGridView();
        private readonly FolderNode head = Exporter.LoadPageObjectTree();

        private readonly MainMenu menu = new MainMenu();
        private readonly MenuItem newTest = new MenuItem("New Test...");
        private readonly Button record = new Button();
        private UserAction recentAction;
        private bool recording;

        public GUI()
        {
            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = false;
            worker.DoWork += StartReceivingActions;
            worker.RunWorkerAsync();

            SuspendLayout();

            FormBorderStyle = FormBorderStyle.FixedSingle;

            Text = "Trailbreaker / Page Object and Test Generator - Test Name: " + testName;
            int width = 750;
            int height = 600;
            ClientSize = new Size(width, height);

            newTest.Click += NewTest;
            enterTestName.Click += EnterTestName;
            fileMenu.MenuItems.Add(newTest);
            fileMenu.MenuItems.Add(enterTestName);
            menu.MenuItems.Add(fileMenu);
            Menu = menu;

            grid.Location = new Point(0, 0);
            grid.Size = new Size(width, height - 40);
            grid.EditMode = DataGridViewEditMode.EditOnKeystroke;

            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;

            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToOrderColumns = false;

            grid.CellEndEdit += GridEdit;

            grid.Columns.Add("Label", "Label");
            grid.Columns[0].Width = width/3 - 1;
            grid.Columns.Add("Selector", "Selector");
            grid.Columns[1].Width = width/3 - 1;
            grid.Columns.Add("Text to Enter", "Text to Enter");
            grid.Columns[2].Width = width / 3 - 1;

            grid.KeyUp += KeyUpHandler;

            record.Text = "Start Recording / New Test";
            record.Width = width;
            record.Height = 40;
            record.Location = new Point(0, height - 40);
            record.Click += Record;

            Controls.Add(grid);
            Controls.Add(record);

            FormClosed += EndApplication;

            ResumeLayout();

            Show();

            Activate();

            UpdateGridView();
        }

        private void GridEdit(object o, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                actions[e.RowIndex].Label = grid.Rows[e.RowIndex].Cells[0].Value.ToString();
            }
            else if (e.ColumnIndex == 2)
            {
                actions[e.RowIndex].Text = grid.Rows[e.RowIndex].Cells[2].Value.ToString();
            }
        }

        private void KeyUpHandler(object o, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (grid.SelectedCells.Count > 0)
                {
                    actions.RemoveAt(grid.SelectedCells[0].RowIndex);
                }
                UpdateGridView();
            }
        }

        private void EndApplication(object o, EventArgs e)
        {
            Application.Exit();
        }

        private void EnterTestName(object o, EventArgs e)
        {
            testName = Interaction.InputBox("Please provide a descriptive name for a new test.", "New Test",
                                            testName);
            if (testName == "")
            {
                testName = "MyEmptyTestName";
            }

            Text = "Trailbreaker / Page Object and Test Generator - Test Name: " + testName;
        }

        private void NewTest(object sender, EventArgs e)
        {
            actions.Clear();

            UpdateGridView();

            EnterTestName(null, null);
        }

        private void StartReceivingActions(object sender, DoWorkEventArgs e)
        {
            new Receiver(this, 8055);
        }

        private bool ActionExists(UserAction action)
        {
            foreach (UserAction userAction in actions)
            {
                if (userAction.Path == action.Path)
                {
                    return true;
                }
            }
            return false;
        }

        public override void AddAction(UserAction userAction)
        {
            userAction.Label = userAction.GetBestLabel();
            recentAction = userAction;
            if (!ActionExists(userAction))
            {
                actions.Add(userAction);
            }
            UpdateGridView();
        }

        public override void AddCharacter(char c)
        {
            if (recentAction != null)
            {
                recentAction.Text += c;
            }
        }

        private void Record(object sender, EventArgs eventArgs)
        {
            if (recording)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (i > 0)
                    {
                        actions[i - 1].ToPage = actions[i].Page;
                        actions[i].ToPage = actions[i].Page;
                    }

                    UserAction action = actions[i];
                    head.UpdateAction(ref action);
                    actions[i] = action;

                    if (!Exporter.PagesToOpen.Contains(action.Page))
                    {
                        Exporter.PagesToOpen.Add(action.Page);
                    }
                }

                Exporter.ExportToOutputFolder(actions, head, testName,
                                              MessageBox.Show("Would you like to view the generated files now?",
                                                              "Convenience",
                                                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                              DialogResult.Yes);

                record.Text = "Start Recording / New Test";
                recording = false;
            }
            else
            {
                if (actions.Count > 0)
                {
                    if (MessageBox.Show("Would you like to clear the list of recorded elements?",
                                        "New Test",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        actions.Clear();
                    }
                }
                EnterTestName(null, null);

                record.Text = "Stop Recording / Export";
                recording = true;
            }
            UpdateGridView();
        }

        private void UpdateGridView()
        {
            grid.Rows.Clear();

            foreach (UserAction act in actions)
            {
                var row = new DataGridViewRow();

                var labelCell = new DataGridViewTextBoxCell();
                labelCell.Value = act.Label;
                row.Cells.Add(labelCell);

                var stringCell = new DataGridViewTextBoxCell();
                stringCell.Value = act.ToString();
                row.Cells.Add(stringCell);

                var textCell = new DataGridViewTextBoxCell();
                textCell.Value = act.Text;
                row.Cells.Add(textCell);

                row.Height = 20;
                grid.Rows.Add(row);
            }
        }
    }
}