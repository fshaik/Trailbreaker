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

            Text = "Trailbreaker / Page Object and Test Generator - Test Name: " + testName;
            ClientSize = new Size(800, 600);
            MinimumSize = new Size(640, 480);

            newTest.Click += NewTest;
            enterTestName.Click += EnterTestName;
            fileMenu.MenuItems.Add(newTest);
            fileMenu.MenuItems.Add(enterTestName);
            menu.MenuItems.Add(fileMenu);
            Menu = menu;

            grid.Columns.Add("Label (Editable)", "Label (Editable)");
            grid.Columns.Add("Detected Page", "Detected Page");
            grid.Columns.Add("Selector", "Selector");
            grid.Columns.Add("Text to Enter (Editable)", "Text to Enter (Editable)");

            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToOrderColumns = false;

            grid.EditMode = DataGridViewEditMode.EditOnKeystroke;

            grid.CellEndEdit += GridEdit;
            grid.KeyUp += KeyUpHandler;

            record.Text = "Start Recording / New Test";

            record.Click += Record;

            UpdateControlSize(null, null);

            Controls.Add(grid);
            Controls.Add(record);

            FormClosed += EndApplication;
            Resize += UpdateControlSize;

            ResumeLayout();

            Show();

            Activate();

            UpdateGridView();
        }

        private void UpdateControlSize(object o, EventArgs e)
        {
            grid.Size = new Size(ClientSize.Width, ClientSize.Height - 40);
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.Width = grid.Width/grid.ColumnCount - 20/grid.ColumnCount;
            }
            record.Location = new Point(0, ClientSize.Height - 40);
            record.Size = new Size(ClientSize.Width, 40);
        }

        private void GridEdit(object o, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                actions[e.RowIndex].Label = grid.Rows[e.RowIndex].Cells[0].Value.ToString();
            }
            else if (e.ColumnIndex == 3)
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
                if (userAction.Path == action.Path && userAction.Page == action.Page)
                {
                    return true;
                }
            }
            return false;
        }

        public override void AddAction(UserAction userAction)
        {
            //If this exact action already exists in the list then the click event is ignored.
            if (!ActionExists(userAction))
            {
//                userAction.ResolveMultipleClassNames();

                //This action will receive characters for it's Text field (Text Entered).
                recentAction = userAction;
                //It is also added to the list of actions.
                actions.Add(userAction);

                //Its label is set to the best deduced label.
                userAction.Label = userAction.GetBestLabel();
                //If the tree already contains this action, then the actions label has already been set and this method will set the label again.
                head.UpdateAction(ref userAction);

                //Updates the grid to include the new action.
                UpdateGridView();
                //The scrollbar is set to the bottom of the grid.
                grid.FirstDisplayedScrollingRowIndex = grid.RowCount - 1;
            }
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

                    if (!Exporter.PagesToOpen.Contains(actions[i].Page))
                    {
                        Exporter.PagesToOpen.Add(actions[i].Page);
                    }
                }

                Exporter.ExportToOutputFolder(actions, head, testName);
//                Exporter.ExportToOutputFolder(actions, head, testName,
//                                              MessageBox.Show("Would you like to view the generated files now?",
//                                                              "Convenience",
//                                                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
//                                              DialogResult.Yes);

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

                var pageCell = new DataGridViewTextBoxCell();
                pageCell.Value = act.Page;
                row.Cells.Add(pageCell);

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