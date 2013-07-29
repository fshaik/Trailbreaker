using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Roslyn.Services;

namespace Trailbreaker.MainApplication
{
    public class Exporter
    {
        public static string OutputSolutionSetting = "outputSolution";

        public static string OutputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrailbreakerOutput");

        public static string PageObjectsFolder = "\\PageObjects\\";
        public static string TestsFolder = "\\Tests\\";

        public static IWorkspace Workspace = null;
        public static string SolutionPath = null;
        public static string PageObjectLibraryName = "MBRegressionLibrary";
        public static IProject PageObjectLibrary = null;
        public static string PageObjectTestLibraryName = "MBRegressionLibrary.Tests";
        public static IProject PageObjectTestLibrary = null;
        public static string TreeName = "MBRegressionLibrary.xml";

        public static List<string> PagesToOpen = new List<string>();

        private static void UpdateTreeWithActions(List<UserAction> actions, FolderNode head)
        {
            //Update the head nodes with the user-altered actions (via GUI).
            foreach (UserAction action in actions)
            {
                if (head.Update(action) == false)
                {
                    Debug.WriteLine("The action was not able to be updated to the XML head!");
                }
            }
        }

        private static void WriteTreeToXML(FolderNode head)
        {
            //Write the nodes (via tree root node) to the XML library.
            Directory.CreateDirectory(OutputPath);
            var writer = new XmlTextWriter(OutputPath + "\\" + TreeName, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            head.WriteToXml(writer);
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        public static void ExportToOutputFolder(List<UserAction> actions, FolderNode head, string testName, bool openFiles)
        {
            UpdateTreeWithActions(actions, head);
            WriteTreeToXML(head);

            if (!Directory.Exists(OutputPath + PageObjectsFolder))
            {
                Directory.CreateDirectory(OutputPath + PageObjectsFolder);
            }

            if (!Directory.Exists(OutputPath + TestsFolder))
            {
                Directory.CreateDirectory(OutputPath + TestsFolder);
            }

            head.BuildRaw(openFiles);
            if (actions.Count > 1)
            {
                CreateTestRaw(actions, testName, openFiles);
            }

            MessageBox.Show(
                actions.Count + " new page objects " + (actions.Count > 1 ? " and a new test " : "") +
                "were exported to \"" + OutputPath + "\"!",
                "Export to Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static FolderNode LoadPageObjectTree()
        {
            var head = new FolderNode(null, PageObjectLibraryName);
            FolderNode curFolder;
            FolderNode folder;
            PageObjectNode pageObject = null;
            WebElementNode webElement;

            string label;
            string name;
            string id;
            string cclass;
            string node;
            string type;
            string path;
            string toname;

            XmlTextReader reader;
            try
            {
                reader = new XmlTextReader(OutputPath + "\\" + TreeName);
                while (true)
                {
                    reader.Read();
                    if (reader.Name == PageObjectLibraryName)
                    {
                        head = new FolderNode(null, PageObjectLibraryName);
                        curFolder = head;
                        break;
                    }
                }
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == PageObjectNode.PageObjectString)
                        {
                            pageObject = new PageObjectNode(curFolder, reader.GetAttribute("Name"));
                            curFolder.Children.Add(pageObject);
                        }
                        else if (reader.Name == WebElementNode.WebElementString)
                        {
                            label = reader.GetAttribute("Label");
                            name = reader.GetAttribute("Name");
                            id = reader.GetAttribute("Id");
                            cclass = reader.GetAttribute("Class");
                            node = reader.GetAttribute("Node");
                            type = reader.GetAttribute("Type");
                            path = reader.GetAttribute("Path");
                            toname = reader.GetAttribute("ToName");
                            if (pageObject == null)
                            {
                                Debug.WriteLine("Bad tree XML! A web element was found before a page object! Exiting");
                                return head;
                            }
                            webElement = new WebElementNode(pageObject, label, name, id, cclass, node, type, path, toname);
                            pageObject.Children.Add(webElement);
                        }
                        else
                        {
                            folder = new FolderNode(curFolder, reader.Name);
                            curFolder.Children.Add(folder);
                            curFolder = folder;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name != PageObjectNode.PageObjectString &&
                             reader.Name != WebElementNode.WebElementString)
                    {
                        curFolder = curFolder.Parent;
                    }
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine("Creating a new page objext tree!");
            }
            return head;
        }

        private static IEnumerable<string> BuildTest(List<UserAction> actions, string testName)
        {
            var lines = new List<string>();

            lines.Add("using MBRegressionLibrary.Base;");
            lines.Add("using MBRegressionLibrary.Clients;");
            lines.Add("using MBRegressionLibrary.Tests.Attributes;");
            lines.Add("using MBRegressionLibrary.Tests.Tests.BusinessMode;");
            lines.Add("using MbUnit.Framework;");
            lines.Add("using " + PageObjectLibraryName + ";");
            lines.Add("");
            lines.Add("namespace " + PageObjectTestLibraryName);
            lines.Add("{");
            lines.Add("\t[Parallelizable]");
            lines.Add("\t[Site(\"AutobotMaster2\")]");
            lines.Add("\tinternal class " + testName + "Tests : AbstractBusinessModeTestSuite");
            lines.Add("\t{");
            lines.Add("\t\t[Test]");
            lines.Add("\t\tpublic void RunSimple" + testName + "Test()");
            lines.Add("\t\t{");
            lines.Add("\t\t\tSession.CurrentBlock<BusinessModePage>().GoTo<" + testName + ">()");

            foreach (UserAction action in actions)
            {
                if (action.Node.ToLower() == "select")
                {
                    lines.Add("\t\t\t\t." + action.Label + ".Options.ToList()[5].Click()");
                }
                else if (action.Node.ToLower() == "input" && action.Type.ToLower() == "checkbox")
                {
                    lines.Add("\t\t\t\t." + action.Label + ".Toggle()");
                }
                else if (action.Node.ToLower() == "input" && action.Type.ToLower() != "button" &&
                         action.Type.ToLower() != "submit")
                {
                    lines.Add("\t\t\t\t." + action.Label + ".EnterText(\"" + action.Text + "\")");
                }
                else
                {
                    lines.Add("\t\t\t\t." + action.Label + ".Click()");
                }
            }
            lines.Add(";");

            lines.Add("\t\t}");
            lines.Add("\t}");
            lines.Add("}");

            return lines.ToArray();
        }

        private static void CreateTestRaw(List<UserAction> actions, string testName, bool openFiles)
        {
            string path = OutputPath + "\\Tests\\" + testName + "Tests.cs";

            FileStream fileStream = File.Create(path);
            var writer = new StreamWriter(fileStream);

            IEnumerable<string> lines = BuildTest(actions, testName);

            foreach (string s in lines)
            {
                writer.WriteLine(s);
            }

            writer.Close();
            fileStream.Close();

            if (openFiles)
            {
                ProcessStartInfo pi = new ProcessStartInfo(path);
                pi.Arguments = Path.GetFileName(path);
                pi.UseShellExecute = true;
                pi.WorkingDirectory = Path.GetDirectoryName(path);
                pi.FileName = "C:\\Windows\\notepad.exe";
                pi.Verb = "OPEN";
                Process.Start(pi);
            }
        }
    }
}