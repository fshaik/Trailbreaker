using System.Diagnostics;
using System.Runtime.Serialization;

namespace Trailbreaker.MainApplication
{
    [DataContract]
    public class UserAction
    {
        public bool IsLabeled = false;
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

//        [DataMember(Name = "Text", IsRequired = true)]
        public string Text = "";

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
                return Path;
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

        public void Print()
        {
            Debug.WriteLine("Label: " + Label);
            Debug.WriteLine("Name: " + Name);
            Debug.WriteLine("Id: " + Id);
            Debug.WriteLine("ClassName: " + ClassName);
            Debug.WriteLine("Page: " + Page);
            Debug.WriteLine("Node: " + Node);
            Debug.WriteLine("Type: " + Type);
            Debug.WriteLine("Path: " + Path);
            Debug.WriteLine("Text: " + Text);
//            Debug.WriteLine("Label: " + ToPage);
//            Debug.WriteLine("Label: " + IsLabeled);
            Debug.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }
    }
}