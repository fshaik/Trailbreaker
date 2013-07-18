using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using Roslyn.Services;

namespace Trailbreaker.RecorderApplication
{
    public class Exporter
    {
        public static string OutputSolutionSetting = "outputSolution";

        public static string outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrailbreakerOutput");

        public static IWorkspace workspace = null;
        public static string solutionPath = null;
        public static string pageObjectLibraryName = "MBRegressionLibrary";
        public static IProject pageObjectLibrary = null;
        public static string pageObjectTestLibraryName = "MBRegressionLibrary.Tests";
        public static IProject pageObjectTestLibrary = null;
        public static string treeName = "MBRegressionLibrary.xml";

        public static void Export(List<UserAction> actions, FolderNode head, string testName)
        {
            //Update the head nodes with the user-altered actions (via GUI).
            foreach (UserAction action in actions)
            {
                if (head.Update(action) == false)
                {
                    Debug.WriteLine("The action was not able to be updated to the XML head!");
                }
            }

            //Write the nodes (via tree root node) to the XML library.
            Directory.CreateDirectory(outputPath);
            var writer = new XmlTextWriter(outputPath + "\\" + treeName, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            head.WriteToXml(writer);
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();

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

        public static FolderNode LoadPageObjectTree()
        {
            var head = new FolderNode(null, pageObjectLibraryName);
            FolderNode curFolder;
            FolderNode folder;
            PageObjectNode pageObject = null;
            WebElementNode webElement;

            string name;
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
                            name = reader.GetAttribute("Name");
                            node = reader.GetAttribute("Node");
                            type = reader.GetAttribute("Type");
                            path = reader.GetAttribute("Path");
                            toname = reader.GetAttribute("ToName");
                            if (pageObject == null)
                            {
                                Debug.WriteLine("Bad tree XML! A web element was found before a page object! Exiting");
                                return head;
                            }
                            webElement = new WebElementNode(pageObject, name, node, type, path, toname);
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

        public static IProject CreateTest(IProject project, List<UserAction> actions, string classname)
        {
            var builder = new StringBuilder();

            builder.Append("using System;");
            builder.Append("using MBRegressionLibrary.Base;");
            builder.Append("using MBRegressionLibrary.Tests.Tests.BusinessMode;");
            builder.Append("using MbUnit.Framework;");
            builder.Append("using " + pageObjectLibraryName + ";");
            builder.Append("");
            builder.Append("namespace " + pageObjectTestLibraryName);
            builder.Append("{");
            builder.Append("\t[Parallelizable]");
            builder.Append("\tpublic class " + classname + "Test : AbstractBusinessModeTestSuite");
            builder.Append("\t{");
            builder.Append("\t\t[Test]");
            builder.Append("\t\tpublic void Run" + classname + "Test()");
            builder.Append("\t\t{");
            builder.Append(
                "\t\t\tSession.NavigateTo<" + actions[0].Page +
                ">(\"https://dev7.mindbodyonline.com/ASP/adm/home.asp?studioid=-40000\");");

            foreach (UserAction action in actions)
            {
                if (action.Node.ToLower() == "input" && action.Type.ToLower() == "checkbox")
                {
                    builder.Append("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Name +
                                   ".Toggle();");
                }
                else if (action.Node.ToLower() == "input" && action.Type.ToLower() != "button")
                {
                    builder.Append("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Name +
                                   ".EnterText(\"" + action.Text + "\");");
                }
                else
                {
                    builder.Append("\t\t\tSession.CurrentBlock<" + action.Page + ">()." + action.Name +
                                   ".Click();");
                }

                builder.Append("\t\t\tSession.Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(1));");
            }

            builder.Append("\t\t}");
            builder.Append("\t}");
            builder.Append("}");

            IProject cproject = project;
            IDocument doc = null;
            string newclassname = classname;

            if (ProjectContainsDocument(cproject, newclassname))
            {
                int i = 0;
                do
                {
                    newclassname = classname + i.ToString();
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