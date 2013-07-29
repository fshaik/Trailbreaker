using System.Diagnostics;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Trailbreaker.MainApplication
{
    [DataContract]
    public class UserAction
    {
        public string ToPage;

        [DataMember(Name = "Label", IsRequired = true)]
        public string Label { get; set; }

        [DataMember(Name = "Name", IsRequired = true)]
        public string Name { get; set; }

        [DataMember(Name = "Id", IsRequired = true)]
        public string Id { get; set; }

        [DataMember(Name = "ClassName", IsRequired = true)]
        public string ClassName { get; set; }

        [DataMember(Name = "Page", IsRequired = true)]
        public string Page { get; set; }

        [DataMember(Name = "Node", IsRequired = true)]
        public string Node { get; set; }

        [DataMember(Name = "Type", IsRequired = true)]
        public string Type { get; set; }

        [DataMember(Name = "Path", IsRequired = true)]
        public string Path { get; set; }

        public string Text = "";

        public void ResolveMultipleClassNames()
        {
            string[] classNames = ClassName.Split(new char[] {' '});
            if (Id == "null" && Name == "null")
            {
                if (classNames.Length > 0)
                {
//                    MessageBox.Show("The clicked element has more than one class name ascribed to it. You must select one to use!", "Selector Selector", new MessageBoxButtons())
                }
            }
        }

        public string GetBestLabel()
        {
            if (Id != "null")
            {
                return Id;
            }
            else if (Name != "null")
            {
                return Name;
            }
            else if (ClassName != "null")
            {
                return ClassName;
            }
            else
            {
                return Node + "/" + Type;
            }
        }

        public override string ToString()
        {
            string by;
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
            return by;
        }
    }
}