using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Mime;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Trailbreaker.MainApplication;

namespace TrailbreakerVSExtension
{
    internal class GUI : TrailbreakerReceiverForm
    {
        private DTE2 _applicationObject;

        private const int GuiMargin = 10;
        private const int GuiSeparator = 25;
        private readonly Label newPageObjectLabel = new Label();
        private readonly TextBox newPageObject = new TextBox();
        private readonly Button insertNewPageObject = new Button();

        private int width = 400;
        
//        private readonly ListView pageObjectActions = new ListView();
//        private readonly Button insertNewPageObject = new Button();

        public GUI(DTE2 applicationObject)
        {
            this._applicationObject = applicationObject;

            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = false;
            worker.DoWork += StartReceivingActions;
            worker.RunWorkerAsync();

            SuspendLayout();

            ClientSize = new Size(width, 300);
            Text = "Trailbreaker";

            newPageObjectLabel.Text = "Current Page Object Code To Insert:";
            newPageObjectLabel.Location = new Point(GuiMargin, GuiMargin);
            newPageObjectLabel.Size = new Size(width, GuiSeparator);

            newPageObject.Multiline = true;
            newPageObject.Location = new Point(GuiMargin, GuiMargin * 2 + GuiSeparator);
            newPageObject.Size = new Size(width - GuiMargin * 2, GuiSeparator * 4);
            newPageObject.ReadOnly = true;

            insertNewPageObject.Text = "Insert Page Object Code Into Editor";
            insertNewPageObject.Location = new Point(GuiMargin, GuiMargin * 2 + GuiSeparator *5);
            insertNewPageObject.Size = new Size(200, GuiSeparator);

            Controls.Add(newPageObjectLabel);
            Controls.Add(newPageObject);
            Controls.Add(insertNewPageObject);

            this.FormClosed += EndApplication;

            ResumeLayout();

            Show();

            Activate();
        }

        private void EndApplication(object o, EventArgs e)
        {
            Application.Exit();
        }

        private void StartReceivingActions(object sender, DoWorkEventArgs e)
        {
            new Receiver(this, 8055);
        }

        private void InsertCode()
        {
            if (_applicationObject.ActiveDocument == null)
            {
                MessageBox.Show("You have no open", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var doc = (TextDocument) _applicationObject.ActiveDocument.Object();
                doc.Selection.Insert(newPageObject.Text);
            }
        }

        public override void AddAction(UserAction userAction)
        {
            string webElementClass;
            string by;

            newPageObject.Text = "";

            if (userAction.Node.ToLower() == "select")
            {
                webElementClass = "SelectBox";
            }
            else if (userAction.Node.ToLower() == "input" && userAction.Type.ToLower() == "checkbox")
            {
                webElementClass = "Checkbox";
            }
            else if (userAction.Node.ToLower() == "input" && userAction.Type.ToLower() != "button" && userAction.Type.ToLower() != "submit")
            {
                webElementClass = "TextField";
            }
            else
            {
                webElementClass = "Clickable";
            }

            if (userAction.Id != "null")
            {
                by = "By.Id(\"" + userAction.Id + "\")";
            }
            else if (userAction.Name != "null")
            {
                by = "By.Name(\"" + userAction.Name + "\")";
            }
            else
            {
                by = "By.XPath(\"" + userAction.Path.Replace("\"", "\\\"") + "\")";
            }

            newPageObject.Text += "\t\tpublic I" + webElementClass + "<" + userAction.ToPage + "> " + userAction.Label + Environment.NewLine;
            newPageObject.Text += "\t\t{" + Environment.NewLine;
            newPageObject.Text += "\t\t\tget { return new " + webElementClass + "<" + userAction.ToPage + ">(this, " + by + "); }" + Environment.NewLine;
            newPageObject.Text += "\t\t}" + Environment.NewLine;

//            Update();
        }

        public override void AddCharacter(char c)
        {
        }
    }
}
