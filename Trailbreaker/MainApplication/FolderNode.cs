using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace Trailbreaker.MainApplication
{
    public class FolderNode
    {
        public readonly string Title;
        public List<FolderNode> Children = new List<FolderNode>();
        public FolderNode Parent;

        public FolderNode(FolderNode parent, string title)
        {
            Parent = parent;
            Title = title;
        }

        public virtual TreeNode GetTreeNode()
        {
            TreeNode node = new TreeNode(Title);
            foreach (FolderNode child in Children)
            {
                node.Nodes.Add(child.GetTreeNode());
            }
            return node;
        }

        public virtual void WriteToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement(Title);
            foreach (FolderNode element in Children)
            {
                element.WriteToXml(writer);
            }
            writer.WriteEndElement();
        }

        public virtual void UpdateAction(ref UserAction userAction)
        {
            foreach (FolderNode element in Children)
            {
                element.UpdateAction(ref userAction);
            }
        }

        public virtual bool Update(UserAction userAction)
        {
            //If this folder doesn't contain the action's page and this is the root then
            if (Contains(userAction) == false && Parent == null)
            {
                //Create a new page object element to suit this page
                Children.Add(new PageObjectNode(this, userAction.Page));
            }

            //Should always find an element to add the action to.
            foreach (FolderNode element in Children)
            {
                if (element.Update(userAction))
                {
                    return true;
                }
            }
            //Should be unreachable!
            return false;
        }

        public virtual bool Contains(UserAction userAction)
        {
            foreach (FolderNode element in Children)
            {
                if (element.Contains(userAction))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual void BuildRaw(bool openFiles)
        {
            foreach (FolderNode node in Children)
            {
                node.BuildRaw(openFiles);
            }
        }
    }
}