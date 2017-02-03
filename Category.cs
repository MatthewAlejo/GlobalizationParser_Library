using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;

namespace GlobalizationParser
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class Category
    {
        public Category()
        {
            Name = "";
        }

        public Category(string name)
        {
            Name = name;
        }

        public Category(string name, List<Filter> filterList)
        {
            Name = name;
            Filters = filterList;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public List<Filter> Filters { get; set; }
        public Regex Filter_Regex { get; set; }
    }
}
