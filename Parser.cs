using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace GlobalizationParser
{
    public class Parser
    {
        private enum _filetype {
            CS,
            CSHTML,
            JS
        }

        private static _filetype _fileTypeSelected = _filetype.CS;
        private static string _FilterPath;

        //extract all .csproj from .sln and send to ProjectList
        public static List<FileParseResult> GetParseResults(string solutionPath)
        {
            //initialize default category and filter used for parsing
            var systemPath = System.Environment.
                     GetFolderPath(
                         Environment.SpecialFolder.CommonApplicationData
                     );
            var complete = Path.Combine(systemPath, "files");
            Directory.CreateDirectory(complete);
            _FilterPath = Path.Combine(complete, @"GlobalizationSerializationTest.xml");

            Console.WriteLine("Creating XML file at directory: " + _FilterPath);

            //extract .csproj files from .sln
            ArrayList CSProjList = ExtractCSProjFiles(solutionPath);
            //extract all .cs/.cshtml/.js files from .csproj
            Dictionary<string, ArrayList> ProjectsAndFiles = SetProjectFileList(CSProjList);
            //parse through each gathered file
            return TraverseFiles(ProjectsAndFiles, solutionPath);
        }

        /// <summary>
        /// Takes a .sln file path and extracts all .csproj files and adds to Project List
        /// </summary>
        /// <param name="solutionPath">
        /// The .sln file path to extract all .csproj files from
        /// </param>
        private static ArrayList ExtractCSProjFiles(string solutionPath)
        {
            string line;
            ArrayList ProjectList = new ArrayList(); ;
            // Read the file and display it line by line.
            var sln = new System.IO.StreamReader(solutionPath);

            if (ProjectList.Count > 0)
            {
                ProjectList.Clear();
            }

            var parts = solutionPath.Split('\\');
            var SolutionName = parts[parts.Length - 1];
            var CSProjPath = solutionPath.Substring(0, solutionPath.Length - SolutionName.Length);
            while ((line = sln.ReadLine()) != null)
            {
                if (line.IndexOf("Project(") >= 0 && line.IndexOf(".csproj") > 0)
                {
                    var sp = line.Split('"');
                    ProjectList.Add(CSProjPath + sp[5]);
                }
            }

            sln.Close();
            return ProjectList;
        }

        /// <summary>
        /// For each .csproj in Project File List, extract all relevant files associated with that project and add to ArrayList AllFileNames.
        /// Projects and their files are stored associatively in Dictionary _ProjectsandFiles
        /// </summary>
        private static Dictionary<string, ArrayList> SetProjectFileList(ArrayList CSProjList)
        {
            Dictionary<string, ArrayList> _ProjectsAndFiles = new Dictionary<string, ArrayList>();
            ArrayList CSFileList;
            if (CSProjList.Count > 0)
            {
                int index = 0;
                string line;
                foreach (string csproj in CSProjList)
                {
                    CSFileList = new ArrayList();
                    ++index;
                    var dir = csproj.Split('\\');
                    if (File.Exists(csproj))
                    {
                        StreamReader project = new StreamReader(csproj);
                        string newPath = csproj.Substring(0, csproj.Length - dir[dir.Length - 1].Length);
                        //read the .csproj file line by line - parse for the selected file type
                        while ((line = project.ReadLine()) != null)
                        {
                            var sp = line.Split('"');
                            if (line.IndexOf("<Compile Include=") > 0)
                            {
                                if (_fileTypeSelected == _filetype.CS && (line.IndexOf(".cs\"") > 0 && !line.Contains("esigner")))
                                {
                                    var parts = sp[1].Split('.');
                                    FileInfo f = new FileInfo(newPath + sp[1]);
                                    CSFileList.Add(f.ToString());
                                }
                            }
                            if (line.IndexOf("<Content Include=") > 0)
                            {
                                if (_fileTypeSelected == _filetype.CSHTML && line.IndexOf(".cshtml\"") > 0)
                                {
                                    var parts = sp[1].Split('.');
                                    FileInfo f = new FileInfo(newPath + sp[1]);
                                    CSFileList.Add(f.ToString());
                                }
                                if (_fileTypeSelected == _filetype.JS && line.IndexOf(".js\"") > 0)
                                {
                                    var parts = sp[1].Split('.');
                                    FileInfo f = new FileInfo(newPath + sp[1]);
                                    CSFileList.Add(f.ToString());
                                }
                            }
                        }
                        project.Close();
                    }
                    _ProjectsAndFiles.Add(csproj, CSFileList);
                }
            }

            return _ProjectsAndFiles;
        }

        /// <summary>
        /// Creates a list of parsing categories from a pre-existing xml file
        /// Populates each catergory's filter list with regex
        /// </summary>
        private static List<Category> InitCategoryList()
        {
            List<Category> categoryList = new List<Category>();

            //if doesnt exist, create xml file
            //if xml exists, read it in and assign to filter list
            if (!File.Exists(_FilterPath))
                SerializeDefaultSet(_FilterPath);

            categoryList = DeserializeFilterXML(_FilterPath);

            foreach (var category in categoryList)
            {
                foreach(var filter in category.Filters)
                {
                    //assign the regex
                    if (!filter.IsPattern)
                        filter.Filter_Regex = new Regex(Regex.Escape(filter.Filter_String));
                    else
                        filter.Filter_Regex = new Regex(filter.Filter_String, RegexOptions.IgnoreCase);
                }
            }

            return categoryList;
        }
        
        private static List<FileParseResult> TraverseFiles(Dictionary<string, ArrayList> ProjectsAndFiles, string solutionPath)
        {
            int filesParsed = 0;

            List<Category> categoryList = InitCategoryList();

            //go through and parse every indexed file 
            List<FileParseResult> projectParseResult = new List<FileParseResult>();
            foreach (KeyValuePair<string, ArrayList> projects in ProjectsAndFiles)
            {
                string currentProject = projects.Key;
                string outputFile = String.Format("Results For Filetype: {0} in Project / Folder: {1}", "CS_Test", solutionPath);
                foreach (string file in projects.Value)
                {
                    filesParsed++;
                    FileInfo fi = new FileInfo(file);
                    //outputFile += ParseFile(fi, categoryList, solutionPath);
                    projectParseResult.Add(ParseFile(fi, categoryList, solutionPath));
                }
            }

            return projectParseResult;
        }

        /// <summary>
        /// Goes through each cs file line by line and determines if there is a match for the selected parsing category
        /// </summary>
        /// <param name="fi"> The file currently being parsed </param>
        private static FileParseResult ParseFile(FileInfo fi, List<Category> categoryList, string solutionPath)
        {
            StreamReader file = fi.OpenText();
            string line;
            string filename = fi.FullName;

            //shorten the filename path in ui for easier reading
            string[] pathName = solutionPath.Split('\\');
            string[] currentName = filename.Split('\\');
            for (int i = 0; i < pathName.Count(); i++)
            {
                if (pathName[i].Equals(currentName[i]))
                {
                    currentName[i] = "";
                }
                else
                {
                    break;
                }
            }
            filename = "~" + Path.Combine(currentName);

            //Read resulting file line by line
            int lineCount = 1;
            int matchCount = 0;
            FileParseResult parseResults = new FileParseResult(filename);
            while ((line = file.ReadLine()) != null)
            {
                //verify if line matches any subfilter within each category
                foreach (Category category in categoryList)
                {
                    //check for inclusive matches first
                    if (IsConditionMet(category, line))
                    {
                        //check if matched line is part of any exclusion filter
                        bool exclusionsDetected = ExclusionsDetected(category, line);

                        //if the line matches and no exclusions detected
                        if (!exclusionsDetected)
                        {
                            category.Count++;
                            matchCount = category.Count;
                            //display the line
                            parseResults.AddMatch(category, line.Trim(), lineCount);
                        }
                    }
                }
                lineCount++;
            }
            file.Close();
            return parseResults;
        }

        /// <summary>
        /// Determines if a line in a file being parsed contains a match for any of the selected categories through Regex
        /// </summary>
        /// <param name="c"> The parsing category </param>
        /// <param name="line"> The line being parsed </param>
        /// <returns> TRUE if line produces a match, FALSE if not </returns>
        private static bool IsConditionMet(Category c, string line)
        {
            foreach (Filter filter in c.Filters)
            {
                if (filter.Include) {
                    if (filter.IsPattern)
                    {
                        if (filter.Filter_Regex.IsMatch(line))
                            return true;
                    }
                    else
                    {
                        if (line.IndexOf(filter.Filter_String, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if the matched line should be omitted due to conflict with an exclusion filter
        /// </summary>
        /// <param name="c">The parsing category</param>
        /// <param name="line">The line being parsed</param>
        /// <returns></returns>
        private static bool ExclusionsDetected(Category c, string line)
        {
            foreach (Filter exclusions in c.Filters)
            {
                if (!exclusions.Include) {
                    if (exclusions.IsPattern)
                    {
                        if (exclusions.Filter_Regex.IsMatch(line))
                            return true;
                    }
                    else
                    {
                        if (line.IndexOf(exclusions.Filter_String, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Saves the Output to Text File
        /// </summary>
        /// <param name="projectPath"> The output file path </param>
        /// <param name="filename"> The name of the output file </param>
        /// <param name="output"> The contents of the output file </param>
        public static void ExportFileToText(string projectPath, string filename, List<FileParseResult> results)
        {
            string outputPath, output;
            DateTime date = DateTime.Now;
            outputPath = String.Format("{0}_{1}_{2}.txt", Path.GetFileName(projectPath), filename, date.ToString("yyyyMMdd_HHmmss"));
            output = String.Format("Results for {0}\r\n", projectPath);

            foreach(FileParseResult fileResult in results){
                output += String.Format("\r\nCurrent File: {0}\r\n", fileResult.GetFileName());
                var categories = fileResult.GetResults();
                foreach (var category in categories) {
                    output += String.Format("     Current Category: {0} - {1} case(s) found\r\n", category.Key, category.Value.Count);
                    foreach (KeyValuePair<int, string> match in category.Value) {
                        output += String.Format("          Line {0} _____ {1}\r\n", match.Key, match.Value);
                    }
                }
            }

            // Write the string to a file.
            System.IO.StreamWriter file = new System.IO.StreamWriter(outputPath);
            file.WriteLine(output);
            file.Close();
        }

        /// <summary>
        /// Initializes the Default Filter Set.
        /// To add a new Filter: 
        /// Name of Filter, Parent Category, Inclusive (true) or Exclusive (false), Regex String, pattern (true), cs compat, cshtml compat, js compat
        /// </summary>
        /// <param name="filename">The XML file to read in the Default and User-Defined filters</param>
        private static void SerializeDefaultSet(string filename)
        {
            //XmlSerializer ser = new XmlSerializer(typeof(List<Filter>));
            XmlSerializer ser = new XmlSerializer(typeof(List<Category>));

            List<Category> categoryList = new List<Category>();
            List<Filter> filterList = new List<Filter>();

            //concatenations
            filterList.Add(new Filter("\"str\" + \"str\"", "String Concatenations", true, "([+].*)?&quot;.*&quot;.*[+].*&quot;", pattern: true, cs: 1, cshtml: 1, js: 0));
            filterList.Add(new Filter("String.Format", "String Concatenations", true, @"tring.Format", cs: 1, cshtml: 1, js: 0));
            filterList.Add(new Filter("String.Concat()", "String Concatenations", true, @".concat", cs: 1, cshtml: 1, js: 1));
            List<Filter> concatFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("String Concatenations", concatFilter));
            filterList.Clear();


            //comparisons
            filterList.Add(new Filter("String.Compare()", "String Comparisons", true, @"tring.Compare", cs: 1, cshtml: 0, js: 0));
            filterList.Add(new Filter("String.CompareTo()", "String Comparisons", true, @"tring.CompareTo", cs: 1, cshtml: 0, js: 0));
            filterList.Add(new Filter("String.Equals()", "String Comparisons", true, @"tring.Equals", cs: 1, cshtml: 0, js: 0));
            filterList.Add(new Filter("StringComparison Enumerator", "String Comparisons", true, @"StringComparison", cs: 1, cshtml: 0, js: 0));
            filterList.Add(new Filter("locale.Compare()", "String Comparisons", true, @"locale.Compare", cs: 0, cshtml: 0, js: 1));
            List<Filter> compareFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("String Comparisons", compareFilter));
            filterList.Clear();

            //func returning strings
            filterList.Add(new Filter("Method Signature", "Functions Returning Strings", true, "([sS]tring( |\\[\\]).*\\()|([cC]har( |\\[\\]).*\\()", pattern: true, cs: 1, cshtml: 0, js: 0));
            filterList.Add(new Filter("Method Signature Exclusions", "Functions Returning Strings", false, @"\.|=|:|return|//|/\*|\*/", pattern: true, cs: 1, cshtml: 0, js: 0));
            List<Filter> funcFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("Functions Returning Strings", funcFilter));
            filterList.Clear();

            //hardcoded 
            filterList.Add(new Filter("Strings in Double Quotes: All", "Hard Coded Strings", true, @"""[a-zA-Z\s]*""", pattern: true));
            filterList.Add(new Filter("Strings in Double Quotes: Multiple Words Only", "Hard Coded Strings", true, @"""(\w+\s.)+[^\""]*""", pattern: true));
            filterList.Add(new Filter("Exclude Log", "Hard Coded Strings", false, @"Log|log"));
            filterList.Add(new Filter("Exclude Assembly", "Hard Coded Strings", false, @"Assembly"));
            filterList.Add(new Filter("Exclude System.Diagnostics", "Hard Coded Strings", false, @"System\.Diagnostics"));
            filterList.Add(new Filter("Exclude Message", "Hard Coded Strings", false, @"Message"));
            filterList.Add(new Filter("Exclude GUID", "Hard Coded Strings", false, @"guid"));
            filterList.Add(new Filter("Exclude Guard.Argument", "Hard Coded Strings", false, @"Guard\.ArgumentNotNull"));
            filterList.Add(new Filter("Exclude InternalCode", "Hard Coded Strings", false, @"InternalCode"));
            filterList.Add(new Filter("Exclude Attribute()", "Hard Coded Strings", false, @"Attribute"));
            filterList.Add(new Filter("Exclude ResourceManager.GetString()", "Hard Coded Strings", false, @"ResourceManager\.GetString"));
            filterList.Add(new Filter("Exclude LocalizableDisplayName", "Hard Coded Strings", false, @"LocalizableDisplayName"));
            List<Filter> hardcodedFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("Hard Coded Strings", hardcodedFilter));
            filterList.Clear();

            //date formats
            filterList.Add(new Filter("\"yyyy\" or \"yy\"", "Date Formats", true, "(yyyy|/|-)(yy|yyyy)|(yy|yyyy)(/|-)", pattern: true, cs: 1, cshtml: 0, js: 1));
            filterList.Add(new Filter("MyDateTime()", "Date Formats", true, @"MyDateTime", cs: 0, cshtml: 1, js: 0));
            List<Filter> dateFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("Date Formats", dateFilter));
            filterList.Clear();

            //decimal formats
            filterList.Add(new Filter("#.# Pattern", "Decimal Formats", true, @"#+[\.,][0-9#]+|[0-9#]+[\.,]#+", pattern: true, cs: 1, cshtml: 1, js: 0));
            filterList.Add(new Filter("Include ToFixed()", "Decimal Formats", true, @"ToFixed", cs: 0, cshtml: 0, js: 1));
            filterList.Add(new Filter("Include ToPrecision()", "Decimal Formats", true, @"ToPrecision", cs: 0, cshtml: 0, js: 1));
            List<Filter> decimalFilter = new List<Filter>(filterList);
            categoryList.Add(AddFilterListToCategory("Decimal Formats", decimalFilter));
            filterList.Clear();

            TextWriter writer = new StreamWriter(filename);
            //ser.Serialize(writer, filterList);
            ser.Serialize(writer, categoryList);
            writer.Close();
        }

        private static Category AddFilterListToCategory(string categoryName, List<Filter> filterList) {
            Category category = new Category(categoryName);
            category.Filters = filterList;
            return category;
        }

        /// <summary>
        /// Reads in the XML file containing all persistent filter data
        /// Stores the filters in a List
        /// </summary>
        /// <param name="filename">The XML file to read in</param>
        /// <returns>A list of filters contained in the XML file</returns>
        private static List<Category> DeserializeFilterXML(string filename)
        {
            List<Category> categoryList;
            // Construct an instance of the XmlSerializer with the type
            // of object that is being deserialized.
            XmlSerializer mySerializer = new XmlSerializer(typeof(List<Category>));
            // To read the file, create a FileStream.
            FileStream myFileStream = new FileStream(filename, FileMode.Open);
            // Call the Deserialize method and cast to the object type.
            categoryList = (List<Category>)mySerializer.Deserialize(myFileStream);
            myFileStream.Close();
            return categoryList;
        }

        /// <summary>
        /// Serializes the Filters into an external XML file for persistnce between sessions 
        /// Called whenever filter list is updated (added to or deleted from)
        /// (Overwrites and updates external xml where filters are stored)
        /// </summary>
        /// <param name="filename">The file the XML is saved to</param>
        private static void SerializeFilterXML(string filename)
        {
            ////XmlSerializer ser = new XmlSerializer(typeof(Filter));
            //List<Filter> filterList = new List<Filter>();
            //XmlSerializer ser = new XmlSerializer(typeof(List<Filter>));

            //TextWriter writer = new StreamWriter(filename);

            //foreach (KeyValuePair<string, List<Filter>> entry in _CategoryFilter)
            //{
            //    foreach (Filter f in entry.Value)
            //    {
            //        filterList.Add(f);
            //    }
            //}
            //ser.Serialize(writer, filterList);

            //writer.Close();
        }
    }

}
