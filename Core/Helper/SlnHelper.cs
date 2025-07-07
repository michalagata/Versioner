using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnubisWorks.Tools.Versioner.Sln
{
    public class CsProjLocations
    {
        public string CsProjDirectory { get; set; }
        public string CsProjFile { get; set; }
    }

    public class SlnHelper
    {
        private string SlnPath;
        private string workingDir;
        private bool searchAllForSln;

        private const string csprojExpression =
            "^\\s*Project\\(\\s*\\\"(?<sln_id>[^\\\"]+)\\\"\\s*\\)\\s*=\\s*\\\"(?<proj_name>[^\\\"]+)\\\",\\s*\\\"(?<proj_path>[^\\\"]+)\\\",\\s*\\\"(?<proj_id>[^\\\"]+)\\\"\\s*";

        public SlnHelper(string workDir, bool searchForSlnInAllLocations)
        {
            workingDir = workDir;
            searchAllForSln = searchForSlnInAllLocations;
        }

        public void Initialize()
        {
            if (string.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir))
                throw new Exception("Working directory does not exist.");

            List<string> checkList = new List<string>();

            if (searchAllForSln)
            {
                foreach (string file in Directory.EnumerateFiles(workingDir,
                    "*.sln",
                    SearchOption.TopDirectoryOnly))
                {
                    checkList.Add(file);
                }
            }
            else
            {
                foreach (string file in Directory.EnumerateFiles(workingDir,
                    "*.sln",
                    SearchOption.TopDirectoryOnly))
                {
                    checkList.Add(file);
                }
            }


            if (!searchAllForSln && checkList.Count > 1) throw new Exception("Multiple SLN files found, cannot continue.");
            if (checkList.Count == 0) throw new Exception("No SLN files found, cannot continue.");

            SlnPath = checkList.FirstOrDefault();
        }

        public string LocateSlnFile()
        {
            return SlnPath;
        }

        public List<CsProjLocations> EnumerateCsProjDirectories()
        {
            if (string.IsNullOrEmpty(SlnPath) || !File.Exists(SlnPath)) throw new Exception("SLN file not found!");

            List<CsProjLocations> csProjList = new List<CsProjLocations>();
            Regex re = new Regex(csprojExpression, RegexOptions.Compiled);
            using StreamReader reader = new StreamReader(SlnPath);
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                var match = re.Match(line);
                if (match != null && match.Success)
                {
                    csProjList.Add(new CsProjLocations
                    {
                        CsProjFile = Path.GetFullPath(Path.Combine(workingDir, match.Groups["proj_path"].Value)),
                        CsProjDirectory = Path.GetDirectoryName(Path.Combine(workingDir,
                            Path.GetDirectoryName(match.Groups["proj_path"].Value)))
                    });
                }
            }

            return csProjList;
        }

        public string GetStartingProjectDirectory()
        {
            List<CsProjLocations> lList = EnumerateCsProjDirectories();
            return lList.FirstOrDefault().CsProjDirectory;
        }

        public string GetStartingProjectFile()
        {
            List<CsProjLocations> lList = EnumerateCsProjDirectories();
            return lList.FirstOrDefault().CsProjFile;
        }
    }
}