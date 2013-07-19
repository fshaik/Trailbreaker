using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Trailbreaker.RecorderApplication
{
    internal class WebElementNode : FolderNode
    {
        public static string WebElementString = "WebElement";

        public string Name;
        public string Node;
        public string Path;
        public string ToName;
        public string Type;

        public WebElementNode(FolderNode parent, string name, string node, string type, string path, string toname)
            : base(parent, WebElementString)
        {
            Name = name;
            Node = node;
            Type = type;
            Path = path;
            ToName = toname;
        }

        public override TreeNode GetTreeNode()
        {
            return new TreeNode(Path);
        }

        public override void WriteToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement(Label);
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Node", Node);
            writer.WriteAttributeString("Type", Type);
            writer.WriteAttributeString("Path", Path);
            writer.WriteAttributeString("ToName", ToName);
            writer.WriteEndElement();
        }

        public override void UpdateAction(ref UserAction userAction)
        {
            if (Path == userAction.Path)
            {
                userAction.IsNamed = true;
                userAction.Name = Name;
            }
        }

        public void BuildRaw(StreamWriter writer)
        {
            writer.WriteLine("");

            if (Node.ToLower() == "input" && Type.ToLower() == "checkbox")
            {
                writer.WriteLine("\t\tpublic ICheckbox<" + ToName + "> " + Name);
                writer.WriteLine("\t\t{");
                writer.WriteLine("\t\t\tget { return new Checkbox<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }
            else if (Node.ToLower() == "input" && Type.ToLower() != "button")
            {
                writer.WriteLine("\t\tpublic ITextField<" + ToName + "> " + Name);
                writer.WriteLine("\t\t{");
                writer.WriteLine("\t\t\tget { return new TextField<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }
            else
            {
                writer.WriteLine("\t\tpublic IClickable<" + ToName + "> " + Name);
                writer.WriteLine("\t\t{");
                writer.WriteLine("\t\t\tget { return new Clickable<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }

            writer.WriteLine("\t\t}");
        }

        public StringBuilder Build(StringBuilder builder)
        {
            builder.Append("");

            if (Node.ToLower() == "input" && Type.ToLower() == "checkbox")
            {
                builder.Append("\t\tpublic ICheckbox<" + ToName + "> " + Name);
                builder.Append("\t\t{");
                builder.Append("\t\t\tget { return new Checkbox<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }
            else if (Node.ToLower() == "input" && Type.ToLower() != "button")
            {
                builder.Append("\t\tpublic ITextField<" + ToName + "> " + Name);
                builder.Append("\t\t{");
                builder.Append("\t\t\tget { return new TextField<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }
            else
            {
                builder.Append("\t\tpublic IClickable<" + ToName + "> " + Name);
                builder.Append("\t\t{");
                builder.Append("\t\t\tget { return new Clickable<" + ToName + ">(this, By.XPath(\"" +
                               Path.Replace("\"", "\\\"") + "\")); }");
            }

            builder.Append("\t\t}");

            return builder;
        }
    }
}