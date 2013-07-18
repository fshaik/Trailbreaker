using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Trailbreaker.RecorderApplication
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

        public override void WriteToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement(Label);
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
                        element.Name = userAction.Name;
                        element.Node = userAction.Node;
                        element.Type = userAction.Type;
                        element.ToName = userAction.ToPage;
                        return true;
                    }
                }
                Children.Add(new WebElementNode(this, userAction.Name, userAction.Node, userAction.Type, userAction.Path,
                                                userAction.ToPage));
                return true;
            }
            return false;
        }

        public override bool Contains(UserAction userAction)
        {
            return Name == userAction.Page;
        }

        public StringBuilder Build()
        {
            var builder = new StringBuilder();

            builder.Append("using Bumblebee.Implementation;");
            builder.Append("using Bumblebee.Interfaces;");
            builder.Append("using Bumblebee.Setup;");
            builder.Append("using OpenQA.Selenium;");
            builder.Append("");
            builder.Append("namespace " + Exporter.pageObjectLibraryName);
            builder.Append("{");
            builder.Append("\tpublic class " + Name + " : Block");
            builder.Append("\t{");
            builder.Append("\t\tpublic " + Name + "(Session session)");
            builder.Append("\t\t\t: base(session)");
            builder.Append("\t\t{");
            builder.Append("\t\t}");

            foreach (WebElementNode node in Children)
            {
                builder = node.Build(builder);
            }

            builder.Append("\t}");
            builder.Append("}");

            return builder;
        }
    }
}