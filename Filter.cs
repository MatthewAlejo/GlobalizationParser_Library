using System.Configuration;
using System.Text.RegularExpressions;

namespace GlobalizationParser
{
    //[SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class Filter
    {
        public Filter()
        {
            Name = "";
            Include = true;
        }

        public Filter(string name)
        {
            Name = name;
        }

        public Filter(string name, string category, bool include, string userfilter, bool pattern = false, int cs = 1, int cshtml = 1, int js = 1)
        {
            Name = name;
            ParentCategory = category;
            Include = include;
            Filter_String = userfilter;
            IsPattern = pattern;
            fileCS = cs;
            fileCSHTML = cshtml;
            fileJS = js;
        }

        public string Name { get; set; }
        public string ParentCategory { get; set; }
        public string Filter_String { get; set; }
        public Regex Filter_Regex { get; set; }
        public bool Include { get; set; }
        public bool IsPattern { get; set; }
        public int fileCS { get; set; }
        public int fileCSHTML { get; set; }
        public int fileJS { get; set; }
    }
}
