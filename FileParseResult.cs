using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalizationParser
{
    class FileParseResult
    {
        public FileParseResult(string fileName) {
            this.fileName = fileName;
            categoryMatches = new Dictionary<string, Dictionary<int, string>>();
        }
        public void AddMatch(Category category, string codeSnippet, int codeLineNum) {
            if (categoryMatches.ContainsKey(category.Name))
            {
                categoryMatches[category.Name].Add(codeLineNum, codeSnippet);
            }
            else {
                Dictionary<int, string> codeMatch = new Dictionary<int, string>();
                codeMatch.Add(codeLineNum, codeSnippet);
                categoryMatches.Add(category.Name, codeMatch);
            }
            
        }
        public String GetFileName()
        {
            return fileName;
        }

        public Dictionary<string, Dictionary<int, string>> GetResults() {
            return categoryMatches;
        }

        public int GetCountByCategory(string categoryName) {
            return categoryMatches[categoryName].Count;
        }

        public Dictionary<int, string> GetResultsByCategory(string categoryName)
        {
            return categoryMatches[categoryName];
        }

        private string fileName;
        private Dictionary<string, Dictionary<int, string>> categoryMatches;
    }
}
