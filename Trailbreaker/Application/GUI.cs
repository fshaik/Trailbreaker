using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace Trailbreaker.RecorderApplication
{
    internal class GUI : Form
    {
        private const int GuiMargin = 10;
        private const int GuiSeparator = 25;
        private readonly List<UserAction> actions = new List<UserAction>();
        private readonly MenuItem enterTestName = new MenuItem("Enter Test Name...");
        private readonly Button export = new Button();
        private readonly MenuItem fileMenu = new MenuItem("File");
        private readonly FolderNode head = Exporter.LoadPageObjectTree();
        private readonly ListView list = new ListView();

        private readonly MainMenu menu = new MainMenu();
        private readonly MenuItem newTest = new MenuItem("New Test...");
        private readonly List<UserAction> ractions = new List<UserAction>();
        private readonly Button record = new Button();
        private readonly ListView rlist = new ListView();
        private readonly MenuItem selectSolution = new MenuItem("Use Another Solution...");

        private readonly Label solutionLabel = new Label();
        private readonly Label testNameLabel = new Label();

        private readonly string[] userActionData = {"Name", "Detected Page", "Node", "Type", "Path", "Text"};
        private readonly List<TextBox> userActionFields = new List<TextBox>();
        private readonly List<Label> userActionLabels = new List<Label>();
        private bool recording;
        private string testName = "MyDescriptiveTestName";

        public GUI()
        {
            EnterTestName(null, null);

            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = false;
            worker.DoWork += StartReceivingActions;
            worker.RunWorkerAsync();

            SuspendLayout();

            ClientSize = new Size(800, 750);
            Text = "Trailbreaker";

            newTest.Click += NewTest;
            selectSolution.Click += SelectSolution;
            enterTestName.Click += EnterTestName;
            fileMenu.MenuItems.Add(newTest);
            fileMenu.MenuItems.Add(selectSolution);
            fileMenu.MenuItems.Add(enterTestName);
            menu.MenuItems.Add(fileMenu);
            Menu = menu;

            solutionLabel.Text = "Using Solution: " + Exporter.solutionPath;
            solutionLabel.Width = 800;
            solutionLabel.Location = new Point(GuiMargin, GuiMargin);

//            testNameLabel.Text = "Creating Test: " + testName;
            testNameLabel.Width = 800;
            testNameLabel.Location = new Point(GuiMargin, GuiMargin + GuiSeparator);

            list.Location = new Point(GuiMargin, GuiMargin*4 + GuiSeparator);
            list.Size = new Size(250, 300);
            list.MultiSelect = false;
            list.View = View.List;
            list.SelectedIndexChanged += ListSelect;

            record.Text = "Start Recording";
            record.Width = 100;
            record.Location = new Point(GuiMargin, GuiMargin*5 + GuiSeparator + 300);
            record.Click += Record;

            rlist.Location = new Point(GuiMargin, GuiMargin*6 + GuiSeparator*2 + 300);
            rlist.Size = new Size(400, 300);
            rlist.MultiSelect = false;
            rlist.View = View.List;
            rlist.SelectedIndexChanged += RListSelect;

            int offset = GuiMargin*5 + GuiSeparator;
            foreach (string data in userActionData)
            {
                var l = new Label();
                l.Text = data + ": ";
                l.Location = new Point(GuiMargin*2 + 250, offset + 3);
                userActionLabels.Add(l);

                var t = new TextBox();
                t.Location = new Point(GuiMargin*2 + 250 + 100, offset);
                t.Width = 250;
                if (data.ToLower() == "name")
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

            export.Text = "Export";
            export.Location = new Point(GuiMargin, GuiMargin*7 + GuiSeparator*2 + 300*2);
            export.Click += doExport;
            offset += 30;

            Controls.Add(solutionLabel);
            Controls.Add(testNameLabel);
            Controls.Add(list);
            Controls.Add(record);
            Controls.Add(rlist);
            Controls.Add(export);

            foreach (Label l in userActionLabels)
            {
                Controls.Add(l);
            }
            foreach (TextBox t in userActionFields)
            {
                Controls.Add(t);
            }

            this.FormClosed += EndApplication;

            ResumeLayout();

            Show();

            Activate();

            UpdateBox();
        }

        private void EndApplication(object o, EventArgs e)
        {
            Application.Exit();
        }

        private void SelectSolution(object o, EventArgs e)
        {
            Exporter.workspace = null;
            Start.FindSolution();
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
            UpdateBox();

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

        public void AddAction(UserAction userAction)
        {
            if (recording)
            {
                actions.Add(userAction);
            }
            else
            {
                ractions.Insert(0, userAction);
            }
            UpdateBox();
        }

        private void Record(object sender, EventArgs eventArgs)
        {
            if (recording)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (i > 0)
                    {
                        if (actions[i].Text != "")
                        {
                            actions[i - 1].Text = actions[i].Text;
                            actions[i].Text = "";
                        }
                        actions[i - 1].ToPage = actions[i].Page;
                        actions[i].ToPage = actions[i].Page;
                    }

                    UserAction action = actions[i];
                    head.UpdateAction(ref action);
                    actions[i] = action;

                    if (!actions[i].IsNamed)
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
                        UpdateBox();
                    }
                    else
                    {
                        return;
                    }
                }
                record.Text = "Stop Recording";
                recording = true;
            }
        }

        private void UpdateSelectedName(Object sender, EventArgs e)
        {
            UserAction current = actions[list.SelectedIndices[0]];
            current.IsNamed = true;
            for (int i = 0; i < actions.Count; i++)
            {
                if (!actions[i].IsNamed)
                {
                    list.Items[i].BackColor = Color.Red;
                }
                else
                {
                    list.Items[i].BackColor = Color.White;
                }
            }
            current.Name = userActionFields[0].Text;
            current.Page = userActionFields[1].Text;
            current.Node = userActionFields[2].Text;
            current.Type = userActionFields[3].Text;
            current.Path = userActionFields[4].Text;
            current.Text = userActionFields[5].Text;
        }

        private void UpdateSelected(Object sender, EventArgs e)
        {
            UserAction current = actions[list.SelectedIndices[0]];
            current.Name = userActionFields[0].Text;
            current.Page = userActionFields[1].Text;
            current.Node = userActionFields[2].Text;
            current.Type = userActionFields[3].Text;
            current.Path = userActionFields[4].Text;
            current.Text = userActionFields[5].Text;
        }

        private void UpdateBox()
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
                userActionFields[0].Text = current.Name;
                userActionFields[1].Text = current.Page;
                userActionFields[2].Text = current.Node;
                userActionFields[3].Text = current.Type;
                userActionFields[4].Text = current.Path;
                userActionFields[5].Text = current.Text;
            }
        }

        private void RListSelect(Object sender, EventArgs e)
        {
            if (rlist.SelectedItems.Count > 0)
            {
                UserAction current = ractions[rlist.SelectedIndices[0]];
                userActionFields[0].Text = current.Name;
                userActionFields[1].Text = current.Page;
                userActionFields[2].Text = current.Node;
                userActionFields[3].Text = current.Type;
                userActionFields[4].Text = current.Path;
                userActionFields[5].Text = current.Text;
            }
        }

        private void doExport(Object sender, EventArgs e)
        {
            foreach (UserAction action in actions)
            {
                if (!action.IsNamed)
                {
                    MessageBox.Show(
                        "You must define a unique name for each action! Unnamed actions are highlighted red.",
                        "Incomplete Naming Scheme",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            Exporter.Export(actions, head, testName);
        }
    }
}