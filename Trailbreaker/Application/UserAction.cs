using System.Diagnostics;
using System.Runtime.Serialization;

namespace Trailbreaker.RecorderApplication
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

        [DataMember(Name = "Page", IsRequired = true)]
        public string Page { get; set; }

        [DataMember(Name = "Node", IsRequired = true)]
        public string Node { get; set; }

        [DataMember(Name = "Type", IsRequired = true)]
        public string Type { get; set; }

        [DataMember(Name = "Path", IsRequired = true)]
        public string Path { get; set; }

        [DataMember(Name = "Text", IsRequired = true)]
        public string Text { get; set; }

        public override string ToString()
        {
            return Path;
        }

        public void Print()
        {
            Debug.WriteLine("Label: " + Label);
            Debug.WriteLine("Name: " + Name);
            Debug.WriteLine("Id: " + Id);
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