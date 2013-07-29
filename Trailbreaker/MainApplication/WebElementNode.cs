using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

namespace Trailbreaker.MainApplication
{
    internal class WebElementNode : FolderNode
    {
        public static string WebElementString = "WebElement";

        private PageObjectNode ParentPageObjectNode;

        public string Label;
        public string Name;
        public string Id;
        public string ClassName;
        public string Node;
        public string Path;
        public string ToName;
        public string Type;

        public WebElementNode(PageObjectNode parent, string label, string name, string id, string cclass, string node, string type,
                              string path, string toname)
            : base(parent, WebElementString)
        {
            ParentPageObjectNode = parent;
            Label = label;
            Name = name;
            Id = id;
            ClassName = cclass;
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
            writer.WriteStartElement(base.Title);
            writer.WriteAttributeString("Label", Label);
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Id", Id);
            writer.WriteAttributeString("ClassName", ClassName);
            writer.WriteAttributeString("Node", Node);
            writer.WriteAttributeString("Type", Type);
            writer.WriteAttributeString("Path", Path);
            writer.WriteAttributeString("ToName", ToName);
            writer.WriteEndElement();
        }

        public override void UpdateAction(ref UserAction userAction)
        {
            if (Path == userAction.Path && ParentPageObjectNode.Name == userAction.Page)
            {
                userAction.Label = Label;
            }
        }

        public IEnumerable<string> Build()
        {
            var lines = new List<string>();
            string webElementClass;
            string by;

            lines.Add("");

            if (Node.ToLower() == "select")
            {
                webElementClass = "SelectBox";
            }
            else if (Node.ToLower() == "input" && Type.ToLower() == "checkbox")
            {
                webElementClass = "Checkbox";
            }
            else if (Node.ToLower() == "input" && Type.ToLower() != "button" && Type.ToLower() != "submit")
            {
                webElementClass = "TextField";
            }
            else
            {
                webElementClass = "Clickable";
            }

            if (Id != "null")
            {
                by = "By.Id(\"" + Id + "\")";
            }
            else if (Name != "null")
            {
                by = "By.Name(\"" + Name + "\")";
            }
            else if (ClassName != "null")
            {
                by = "By.ClassName(\"" + ClassName + "\")";
            }
            else
            {
                by = "By.XPath(\"" + Path.Replace("\"", "\\\"") + "\")";
            }

            lines.Add("\t\tpublic I" + webElementClass + "<" + ToName + "> " + Label);
            lines.Add("\t\t{");
            lines.Add("\t\t\tget { return new " + webElementClass + "<" + ToName + ">(this, " + by + "); }");

            lines.Add("\t\t}");

            return lines.ToArray();
        }
    }
}