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

        public static string outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrailbreakerOutput");

        public static string pageObjectsFolder = "\\PageObjects\\";
        public static string testsFolder = "\\Tests\\";

        public static IWorkspace workspace = null;
        public static string solutionPath = null;
        public static string pageObjectLibraryName = "MBRegressionLibrary";
        public static IProject pageObjectLibrary = null;
        public static string pageObjectTestLibraryName = "MBRegressionLibrary.Tests";
        public static IProject pageObjectTestLibrary = null;
        public static string treeName = "MBRegressionLibrary.xml";

        public static List<string> pagesToOpen = new List<string>();

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
            Directory.CreateDirectory(outputPath);
            var writer = new XmlTextWriter(outputPath + "\\" + treeName, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            head.WriteToXml(writer);
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        public static void ExportToVisualStudio(List<UserAction> actions, FolderNode head, string testName)
        {
            UpdateTreeWithActions(actions, head);
            WriteTreeToXML(head);

            //Generate the page objects.
            IEnumerable<IProject> projects;

            projects = workspace.CurrentSolution.GetProjectsByName(pageObjectLibraryName);
            foreach (IProject proj in projects)
            {
                pageObjectLibrary = proj;
            }
            bool result = workspace.ApplyChanges(pageObjectLibrary.Solution, head.Build(pageObjectLibrary).Solution,
                                                 "Saved new page objects!");
            Debug.WriteLine("SUCCESSFULLY ADDED PAGE OBJECTS: " + result.ToString());

            //If there are more than 1 recorded actions, then generate a test.
            if (actions.Count > 1)
            {
                projects = workspace.CurrentSolution.GetProjectsByName(pageObjectTestLibraryName);
                foreach (IProject proj in projects)
                {
                    pageObjectTestLibrary = proj;
                }
                result = workspace.ApplyChanges(pageObjectTestLibrary.Solution,
                                                CreateTest(pageObjectTestLibrary, actions, testName).Solution,
                                                "Saved a new test!");
                Debug.WriteLine("SUCCESSFULLY ADDED A TEST: " + result.ToString());
            }
        }

        public static void ExportToOutputFolder(List<UserAction> actions, FolderNode head, string testName, bool openFiles)
        {
            UpdateTreeWithActions(actions, head);
            WriteTreeToXML(head);

            if (!Directory.Exists(outputPath + pageObjectsFolder))
            {
                Directory.CreateDirectory(outputPath + pageObjectsFolder);
            }

            if (!Directory.Exists(outputPath + testsFolder))
            {
                Directory.CreateDirectory(outputPath + testsFolder);
            }

            head.BuildRaw(openFiles);
            if (actions.Count > 1)
            {
                CreateTestRaw(actions, testName, openFiles);
            }

            MessageBox.Show(
                actions.Count + " new page objects " + (actions.Count > 1 ? " and a new test " : "") +
                "were exported to \"" + outputPath + "\"!",
                "Export to Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static FolderNode LoadPageObjectTree()
        {
            var head = new FolderNode(null, pageObjectLibraryName);
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
                reader = new XmlTextReader(outputPath + "\\" + treeName);
                while (true)
                {
                    reader.Read();
                    if (reader.Name == pageObjectLibraryName)
                    {
                        head = new FolderNode(null, pageObjectLibraryName);
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

        public static bool ProjectContainsDocument(IProject project, string name)
        {
            foreach (IDocument document in project.Documents)
            {
                if (document.Name == name + ".cs")
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> BuildTest(List<UserAction> actions, string testName)
        {
            var lines = new List<string>();

//            lines.Add("using System;");
            lines.Add("using MBRegressionLibrary.Base;");
            lines.Add("using MBRegressionLibrary.Clients;");
            lines.Add("using MBRegressionLibrary.Tests.Attributes;");
            lines.Add("using MBRegressionLibrary.Tests.Tests.BusinessMode;");
            lines.Add("using MbUnit.Framework;");
            lines.Add("using " + pageObjectLibraryName + ";");
            lines.Add("");
            lines.Add("namespace " + pageObjectTestLibraryName);
            lines.Add("{");
            lines.Add("\t[Parallelizable]");
            lines.Add("\t[Site(\"AutobotMaster2\")]");
            lines.Add("\tinternal class " + testName + "Tests : AbstractBusinessModeTestSuite");
            lines.Add("\t{");
            lines.Add("\t\t[Test]");
            lines.Add("\t\tpublic void RunSimple" + testName + "Test()");
            lines.Add("\t\t{");
//            lines.Add(
//                "\t\t\tSession.NavigateTo<" + actions[0].Page +
//                ">(\"https://dev7.mindbodyonline.com/ASP/adm/home.asp?studioid=-40000\");");

            //            lines.Add("\t\t\tSession.Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(1));");
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
//                if (action.Node.ToLower() == "select")
//                {
//                    lines.Add("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Label +
//                              ".Options.Single().Click();");
//                }
//                else if (action.Node.ToLower() == "input" && action.Type.ToLower() == "checkbox")
//                {
//                    lines.Add("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Label +
//                              ".Toggle();");
//                }
//                else if (action.Node.ToLower() == "input" && action.Type.ToLower() != "button" &&
//                         action.Type.ToLower() != "submit")
//                {
//                    lines.Add("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Label +
//                              ".EnterText(\"" + action.Text + "\");");
//                }
//                else
//                {
//                    lines.Add("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Label +
//                              ".Click();");
//                }
            }
            lines.Add(";");

            lines.Add("\t\t}");
            lines.Add("\t}");
            lines.Add("}");

            return lines.ToArray();
        }

        private static void CreateTestRaw(List<UserAction> actions, string testName, bool openFiles)
        {
            string path = outputPath + "\\Tests\\" + testName + "Tests.cs";

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

        private static IProject CreateTest(IProject project, List<UserAction> actions, string testName)
        {
            var builder = new StringBuilder();

            IEnumerable<string> lines = BuildTest(actions, testName);

            foreach (string s in lines)
            {
                builder.Append(s);
            }

            IProject cproject = project;
            IDocument doc = null;
            string newclassname = testName;

            if (ProjectContainsDocument(cproject, newclassname))
            {
                int i = 0;
                do
                {
                    newclassname = testName + i.ToString();
                    i++;
                } while (ProjectContainsDocument(cproject, newclassname));
            }
            doc = cproject.AddDocument(newclassname, builder.ToString());

            //            foreach (IDocument document in cproject.Documents)
            //            {
            //                Debug.WriteLine("Document " + document.Name + " exists!");
            //                if (document.Name == classname + ".cs")
            //                {
            //                    doc = document.UpdateText(Syntax.ParseCompilationUnit(builder.ToString()).GetText());
            //                    break;
            //                }
            //            }
            //            if (doc == null)
            //            {
            //                Debug.WriteLine("Document " + classname + " doesn't exist!");
            //                doc = cproject.AddDocument(classname, builder.ToString());
            //            }

            //            doc.Organize();
            //            doc.Cleanup();
            cproject = doc.Project;

            return cproject;
        }
    }
}