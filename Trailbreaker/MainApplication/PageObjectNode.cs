using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Trailbreaker.MainApplication
{
    internal class PageObjectNode : FolderNode
    {
        public static string PageObjectString = "PageObject";

        public new List<WebElementNode> Children = new List<WebElementNode>();
        public string Name;

        public PageObjectNode(FolderNode parent, string name)
            : base(parent, PageObjectString)
        {
            Name = name;
        }

        public override TreeNode GetTreeNode()
        {
            var node = new TreeNode(Name);
            foreach (WebElementNode child in Children)
            {
                node.Nodes.Add(child.GetTreeNode());
            }
            return node;
        }

        public override void WriteToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement(Title);
            writer.WriteAttributeString("Name", Name);
            foreach (WebElementNode element in Children)
            {
                element.WriteToXml(writer);
            }
            writer.WriteEndElement();
        }

        public override void UpdateAction(ref UserAction userAction)
        {
            foreach (WebElementNode element in Children)
            {
                element.UpdateAction(ref userAction);
            }
        }

        public override bool Update(UserAction userAction)
        {
            if (Name == userAction.Page)
            {
                //If this PageObjectElement contains a PageObjectWebElement with this new action's path, then a new one doesn't need to be added.
                foreach (WebElementNode element in Children)
                {
                    if (element.Path == userAction.Path)
                    {
                        element.Label = userAction.Label;
                        element.Name = userAction.Name;
                        element.Id = userAction.Id;
                        element.ClassName = userAction.ClassName;
                        element.Node = userAction.Node;
                        element.Type = userAction.Type;
                        element.ToName = userAction.ToPage;
                        return true;
                    }
                }
                Children.Add(new WebElementNode(this, userAction.Label, userAction.Name, userAction.Id, userAction.ClassName, userAction.Node,
                                                userAction.Type, userAction.Path,
                                                userAction.ToPage));
                return true;
            }
            return false;
        }

        public override bool Contains(UserAction userAction)
        {
            return Name == userAction.Page;
        }

        private IEnumerable<string> Build()
        {
            var lines = new List<string>();

            lines.Add("using Bumblebee.Implementation;");
            lines.Add("using Bumblebee.Interfaces;");
            lines.Add("using Bumblebee.Setup;");
            lines.Add("using OpenQA.Selenium;");
            lines.Add("");
            lines.Add("namespace " + Exporter.pageObjectLibraryName);
            lines.Add("{");
            lines.Add("\tpublic class " + Name + " : BusinessModePage");
            lines.Add("\t{");
            lines.Add("\t\tpublic " + Name + "(Session session)");
            lines.Add("\t\t\t: base(session)");
            lines.Add("\t\t{");
            lines.Add("\t\t}");

            foreach (WebElementNode node in Children)
            {
                lines.AddRange(node.Build());
            }

            lines.Add("\t}");
            lines.Add("}");

            return lines.ToArray();
        }

        public override void BuildRaw(bool openFiles)
        {
            if (Name == null)
            {
                return;
            }

            string path = Exporter.outputPath + "\\PageObjects\\" + Name + ".cs";

            FileStream fileStream = File.Create(path);
            var writer = new StreamWriter(fileStream);

            IEnumerable<string> build = Build();

            foreach (string s in build)
            {
                writer.WriteLine(s);
            }

            writer.Close();
            fileStream.Close();

            if (openFiles && (GUI.testName == Name || Exporter.pagesToOpen.Contains(Name)))
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

        public StringBuilder BuildString()
        {
            var builder = new StringBuilder();
            IEnumerable<string> build = Build();

            foreach (string s in build)
            {
                builder.Append(s);
            }

            return builder;
        }

//        public override void BuildOther()
//        {
//            string path = Exporter.outputPath + "\\PageObjects\\" + Name + ".cs";
//
//            FileStream fileStream = File.Create(path);
//            StreamWriter writer = new StreamWriter(fileStream);
//
//            writer.WriteLine("using Bumblebee.Implementation;");
//            writer.WriteLine("using Bumblebee.Interfaces;");
//            writer.WriteLine("using Bumblebee.Setup;");
//            writer.WriteLine("using OpenQA.Selenium;");
//            writer.WriteLine("");
//            writer.WriteLine("namespace " + Exporter.pageObjectLibraryName);
//            writer.WriteLine("{");
//            writer.WriteLine("\tpublic class " + Name + " : Block");
//            writer.WriteLine("\t{");
//            writer.WriteLine("\t\tpublic " + Name + "(Session session)");
//            writer.WriteLine("\t\t\t: base(session)");
//            writer.WriteLine("\t\t{");
//            writer.WriteLine("\t\t}");
//
//            foreach (WebElementNode node in Children)
//            {
//                node.BuildRaw(writer);
//            }
//
//            writer.WriteLine("\t}");
//            writer.WriteLine("}");
//
//            writer.Close();
//            fileStream.Close();
//        }

//        public StringBuilder Build()
//        {
//            var builder = new StringBuilder();
//
//            builder.Append("using Bumblebee.Implementation;");
//            builder.Append("using Bumblebee.Interfaces;");
//            builder.Append("using Bumblebee.Setup;");
//            builder.Append("using OpenQA.Selenium;");
//            builder.Append("");
//            builder.Append("namespace " + Exporter.pageObjectLibraryName);
//            builder.Append("{");
//            builder.Append("\tpublic class " + Name + " : Block");
//            builder.Append("\t{");
//            builder.Append("\t\tpublic " + Name + "(Session session)");
//            builder.Append("\t\t\t: base(session)");
//            builder.Append("\t\t{");
//            builder.Append("\t\t}");
//
//            foreach (WebElementNode node in Children)
//            {
//                builder = node.Build(builder);
//            }
//
//            builder.Append("\t}");
//            builder.Append("}");
//
//            return builder;
//        }
    }
}