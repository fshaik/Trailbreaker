using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace Trailbreaker.MainApplication
{
    internal class GUI : TrailbreakerReceiverForm
    {
        private const int GuiMargin = 10;
        private const int GuiSeparator = 25;
        private readonly List<UserAction> actions = new List<UserAction>();
        private readonly MenuItem enterTestName = new MenuItem("Enter Test Name...");
        private readonly CheckBox openFiles = new CheckBox();
        private readonly Button exportToOutputFolder = new Button();
//        private readonly Button exportToVisualStudio = new Button();
        private readonly MenuItem fileMenu = new MenuItem("File");
        private readonly FolderNode head = Exporter.LoadPageObjectTree();
        private readonly ListView list = new ListView();

        private readonly MainMenu menu = new MainMenu();
        private readonly Label metaLabel = new Label();
        private readonly MenuItem newTest = new MenuItem("New Test...");
        private readonly List<UserAction> ractions = new List<UserAction>();
        private readonly Button record = new Button();
        private readonly Button remove = new Button();
        private readonly ListView rlist = new ListView();
//        private readonly MenuItem selectSolution = new MenuItem("Use Another Solution...");

//        private readonly Label solutionLabel = new Label();
        private readonly Label testNameLabel = new Label();
//        private readonly TreeView tree = new TreeView();

        private readonly string[] userActionData = {"Label", "Entered Text"};
        private readonly List<TextBox> userActionFields = new List<TextBox>();
        private readonly List<Label> userActionLabels = new List<Label>();
        private UserAction recentAction;
        private bool recording;
        public static string testName = "MyDescriptiveTestName";

        public GUI()
        {
            EnterTestName(null, null);

            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = false;
            worker.DoWork += StartReceivingActions;
            worker.RunWorkerAsync();

            SuspendLayout();

            Text = "Trailbreaker";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ClientSize = new Size(800, 730);

            newTest.Click += NewTest;
//            selectSolution.Click += SelectSolution;
            enterTestName.Click += EnterTestName;
            fileMenu.MenuItems.Add(newTest);
//            fileMenu.MenuItems.Add(selectSolution);
            fileMenu.MenuItems.Add(enterTestName);
            menu.MenuItems.Add(fileMenu);
            Menu = menu;

//            solutionLabel.Text = "Using Solution: " + Exporter.solutionPath;
//            solutionLabel.Width = 800;
//            solutionLabel.Location = new Point(GuiMargin, GuiMargin);

            testNameLabel.Text = "Creating Test: " + testName;
            testNameLabel.Width = 300;
            testNameLabel.Location = new Point(GuiMargin, GuiMargin);

            metaLabel.Text = "Useful Element Metadata: (Setting a nice, valid label is a good idea!)";
            metaLabel.Width = 400;
            metaLabel.Location = new Point(GuiMargin*2 + 400, GuiMargin*2 + GuiSeparator);

            list.Location = new Point(GuiMargin, GuiMargin*2 + GuiSeparator);
            list.Size = new Size(400, 300);
            list.MultiSelect = false;
            list.Columns.Add("Recorded Clicked Elements", 390);
            //            list.Scrollable = true;
            list.View = View.Details;
            //            list.HeaderStyle = ColumnHeaderStyle.None;
            list.SelectedIndexChanged += ListSelect;

            remove.Text = "Remove Selected";
            remove.Width = 200;
            remove.Location = new Point(GuiMargin*2 + 400, GuiMargin + 300);
            remove.Click += Remove;

            record.Text = "Start Recording";
            record.Width = 100;
            record.Location = new Point(GuiMargin, GuiMargin*3 + GuiSeparator + 300);
            record.Click += Record;

            rlist.Location = new Point(GuiMargin, GuiMargin*4 + GuiSeparator*2 + 300);
            rlist.Size = new Size(400, 300);
            rlist.MultiSelect = false;
            rlist.Columns.Add("Other Clicked Elements", 390);
//            rlist.Scrollable = true;
            rlist.View = View.Details;
//            rlist.HeaderStyle = ColumnHeaderStyle.None;
            rlist.SelectedIndexChanged += RListSelect;

            int offset = GuiMargin*5 + GuiSeparator;
            foreach (string data in userActionData)
            {
                var l = new Label();
                l.Text = data + ": ";
                l.Location = new Point(GuiMargin*2 + 400, offset + 3);
                userActionLabels.Add(l);

                var t = new TextBox();
                t.Location = new Point(GuiMargin*2 + 400 + 100, offset);
                t.Width = 200;
                if (data.ToLower() == "label")
                {
                    t.LostFocus += UpdateSelectedName;
                }
                else
                {
                    t.LostFocus += UpdateSelected;
                }
                userActionFields.Add(t);

                offset += GuiSeparator;
            }
//            userActionFields[0].KeyUp += KeyUpHandler;

            openFiles.Text = "Open Files After Export";
            openFiles.Location = new Point(GuiMargin, GuiMargin * 5 + GuiSeparator * 2 + 300 * 2);
            openFiles.Width = 150;
            openFiles.Checked = true;

            exportToOutputFolder.Text = "Export to Text Files";
            exportToOutputFolder.Location = new Point(GuiMargin*2 + 150, GuiMargin*5 + GuiSeparator*2 + 300*2);
            exportToOutputFolder.Width = 150;
            exportToOutputFolder.Click += ExportToOutputFolder;

//            exportToVisualStudio.Text = "Export to Visual Studio";
//            exportToVisualStudio.Location = new Point(GuiMargin*2 + 150, GuiMargin*7 + GuiSeparator*2 + 300*2);
//            exportToVisualStudio.Width = 150;
//            exportToVisualStudio.Click += ExportToVisualStudio;

            //            Controls.Add(solutionLabel);
            Controls.Add(testNameLabel);
            Controls.Add(metaLabel);
            Controls.Add(list);
            Controls.Add(remove);
            Controls.Add(record);
            Controls.Add(rlist);
            Controls.Add(openFiles);
            Controls.Add(exportToOutputFolder);
//            Controls.Add(exportToVisualStudio);

//            Controls.Add(tree);

            foreach (Label l in userActionLabels)
            {
                Controls.Add(l);
            }
            foreach (TextBox t in userActionFields)
            {
                Controls.Add(t);
            }

            FormClosed += EndApplication;

            ResumeLayout();

            Show();

            Activate();

            UpdateListView();
        }

        private void Remove(object o, EventArgs e)
        {
            if (list.SelectedIndices.Count > 0)
            {
                actions.RemoveAt(list.SelectedIndices[0]);
                UpdateListView();
            }
        }

        private void KeyUpHandler(object o, KeyEventArgs e)
        {
            int index;
            Debug.WriteLine("KeyUp!");
            if (e.KeyCode == Keys.Enter)
            {
                UpdateSelectedName(null, null);

                Debug.WriteLine("ENTERUp!");
                if (list.SelectedIndices.Count > 0)
                {
                    index = list.SelectedIndices[0];
                    if (index == list.Items.Count - 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        do
                        {
                            index++;
                            list.Items[index].Selected = true;
                            list.Select();
                        } while (!actions[index].IsLabeled && !ActionsAreNamed());
                    }
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        if (i == index)
                        {
                            list.Items[i].Selected = true;
                        }
                        else
                        {
                            list.Items[i].Selected = false;
                        }
                    }
                    list.Select();
                    userActionFields[0].Select();
                }
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
            testNameLabel.Text = "Creating Test: " + testName;
        }

        private void NewTest(object sender, EventArgs e)
        {
            actions.Clear();
            ractions.Clear();
            UpdateListView();

            foreach (TextBox t in userActionFields)
            {
                t.Clear();
            }

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
            userAction.Print();
            if (recording)
            {
                if (!ActionExists(userAction))
                {
                    actions.Add(userAction);
                }
            }
            else
            {
                ractions.Insert(0, userAction);
            }
            UpdateListView();
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
//                exportToVisualStudio.Enabled = true;
                exportToOutputFolder.Enabled = true;
                for (int i = 0; i < actions.Count; i++)
                {
                    if (i > 0)
                    {
//                        if (actions[i].Text != "")
//                        {
//                            actions[i - 1].Text = actions[i].Text;
//                            actions[i].Text = "";
//                        }
                        actions[i - 1].ToPage = actions[i].Page;
                        actions[i].ToPage = actions[i].Page;
                    }

                    UserAction action = actions[i];
//                    head.UpdateAction(ref action);
                    actions[i] = action;

                    if (!actions[i].IsLabeled)
                    {
                        list.Items[i].BackColor = Color.Red;
                    }
                    else
                    {
                        list.Items[i].BackColor = Color.White;
                    }
                }
                record.Text = "Start Recording";
                recording = false;
            }
            else
            {
                if (actions.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you would like to record over your previous actions?",
                                        "Recording New Actions",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        actions.Clear();
                        UpdateListView();
                        EnterTestName(null, null);
                    }
                    else
                    {
                        return;
                    }
                }
                //                exportToVisualStudio.Enabled = false;
                exportToOutputFolder.Enabled = false;
                record.Text = "Stop Recording";
                recording = true;
            }
        }

        private void UpdateSelectedName(Object sender, EventArgs e)
        {
            UserAction current = actions[list.SelectedIndices[0]];
            current.IsLabeled = true;
            for (int i = 0; i < actions.Count; i++)
            {
                if (!actions[i].IsLabeled)
                {
                    list.Items[i].BackColor = Color.Red;
                }
                else
                {
                    list.Items[i].BackColor = Color.White;
                }
            }
            current.Label = userActionFields[0].Text;
//            current.Page = userActionFields[1].Text;
//            current.Node = userActionFields[2].Text;
//            current.Type = userActionFields[3].Text;
//            current.Path = userActionFields[4].Text;
            current.Text = userActionFields[1].Text;
        }

        private void UpdateSelected(Object sender, EventArgs e)
        {
            UserAction current = actions[list.SelectedIndices[0]];
            current.Label = userActionFields[0].Text;
//            current.Page = userActionFields[1].Text;
//            current.Node = userActionFields[2].Text;
//            current.Type = userActionFields[3].Text;
//            current.Path = userActionFields[4].Text;
            current.Text = userActionFields[1].Text;
        }

        private void UpdateListView()
        {
            list.Items.Clear();
            foreach (UserAction act in actions)
            {
                list.Items.Add(act.ToString());
            }
            rlist.Items.Clear();
            foreach (UserAction act in ractions)
            {
                rlist.Items.Add(act.ToString());
            }
        }

        private void ListSelect(Object sender, EventArgs e)
        {
            if (list.SelectedItems.Count > 0)
            {
                UserAction current = actions[list.SelectedIndices[0]];
                userActionFields[0].Text = current.Label;
//                userActionFields[1].Text = current.Page;
//                userActionFields[2].Text = current.Node;
//                userActionFields[3].Text = current.Type;
//                userActionFields[4].Text = current.Path;
                userActionFields[1].Text = current.Text;
            }
        }

        private void RListSelect(Object sender, EventArgs e)
        {
            if (rlist.SelectedItems.Count > 0)
            {
                UserAction current = ractions[rlist.SelectedIndices[0]];
                userActionFields[0].Text = current.Label;
//                userActionFields[1].Text = current.Page;
//                userActionFields[2].Text = current.Node;
//                userActionFields[3].Text = current.Type;
//                userActionFields[4].Text = current.Path;
                userActionFields[1].Text = current.Text;
            }
        }

        private bool ActionsAreNamed()
        {
            foreach (UserAction action in actions)
            {
                if (!action.IsLabeled)
                {
                    MessageBox.Show(
                        "You must define a unique label for each action! Unnamed actions are highlighted red.",
                        "Incomplete Naming Scheme",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }
            return true;
        }

        private void ExportToOutputFolder(Object sender, EventArgs e)
        {
            if (ActionsAreNamed())
            {
                Exporter.ExportToOutputFolder(actions, head, testName, openFiles.Checked);
            }
        }
    }
}