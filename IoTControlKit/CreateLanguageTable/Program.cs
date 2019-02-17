using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CreateLanguageTable
{
    class Program
    {
        static HashSet<string> allEntries;
        static HashSet<string> allJSEntries;

        static void Main(string[] args)
        {
            var _buildIdentifier = Guid.NewGuid().ToString("N");

            var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (string.Compare(curDir.Name, "IoTControlKit", true) != 0 && curDir.Parent != null)
            {
                curDir = curDir.Parent;
            }
            if (string.Compare(curDir.Name, "IoTControlKit", true) == 0)
            {
                var projDir = curDir.GetDirectories("IoTControlKit");
                if (projDir.Length == 1)
                {
                    curDir = projDir[0];
                }

                allEntries = new HashSet<string>();
                allJSEntries = new HashSet<string>();
                var sb = new StringBuilder();
                var js = new StringBuilder();
                sb.AppendLine(@"namespace IoTControlKit.Data
    {
        //THIS FILE IS AUTO GENERATED DO NOT EDIT
        public static class OriginalText
        {
            public static string[] Entries = {
");

                js.AppendLine(@"<script>

    var __translationTable = [];
");


                //parse all cs, ts, and cshtml files
                ParseDirectory(curDir, sb, js);
                sb.AppendLine(@"        };
        }
    }");
                js.AppendLine(@"

    function _T(text) {
        var resultText = __translationTable[text];
        if (resultText != undefined)
            return resultText;
        return text;
    }

</script>");

                if (!Directory.Exists(Path.Combine(curDir.FullName, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(curDir.FullName, "Data"));
                }
                File.WriteAllText(Path.Combine(curDir.FullName, "Data", "OriginalText.cs"), sb.ToString());
                File.WriteAllText(Path.Combine(curDir.FullName, "Views", "Shared", "LocalizationForJavascript.cshtml"), js.ToString());
            }
        }

        static void ParseDirectory(DirectoryInfo di, StringBuilder sb, StringBuilder js)
        {
            var allFiles = di.GetFiles("*.cs").ToList();
            allFiles.AddRange(di.GetFiles("*.cshtml"));
            allFiles.AddRange(di.GetFiles("*.ts"));
            foreach (var f in allFiles)
            {
                if (f.Name != "LocalizationForJavascript.cshtml")
                {
                    ParseFile(f, sb, js);
                }
            }
            var allDirs = di.GetDirectories();
            foreach (var d in allDirs)
            {
                ParseDirectory(d, sb, js);
            }
        }
        static void ParseFile(FileInfo fi, StringBuilder sb, StringBuilder js)
        {
            Regex regex;
            Regex regex2 = null;
            Regex regex_fixedData = new Regex("CreateInstance\\(\"(.*?)\", \"(.*?)\"\\)");
            if (string.Compare(fi.Extension, ".cs", true) == 0)
            {
                regex = new Regex("_T\\(\"(.*?)\"\\)");
            }
            else if (string.Compare(fi.Extension, ".ts", true) == 0)
            {
                regex = new Regex("_T\\(\"(.*?)\"\\)");
                regex2 = new Regex("_T\\(\'(.*?)\'\\)");
            }
            else if (string.Compare(fi.Extension, ".cshtml", true) == 0)
            {
                regex = new Regex("Html.T\\(\"(.*?)\"\\)");
            }
            else
            {
                return;
            }

            var matches = regex.Matches(File.ReadAllText(fi.FullName));
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    if (string.Compare(fi.Extension, ".ts", true) == 0)
                    {
                        AddEntry(m.Groups[1].Value, fi.Name, sb, js);
                    }
                    else
                    {
                        AddEntry(m.Groups[1].Value, fi.Name, sb, null);
                    }
                }
            }

            if (regex2 != null)
            {
                matches = regex2.Matches(File.ReadAllText(fi.FullName));
                if (matches.Count > 0)
                {
                    foreach (Match m in matches)
                    {
                        if (string.Compare(fi.Extension, ".ts", true) == 0)
                        {
                            AddEntry(m.Groups[1].Value, fi.Name, sb, js);
                        }
                        else
                        {
                            AddEntry(m.Groups[1].Value, fi.Name, sb, null);
                        }
                    }
                }
            }

            var matches_fixedData = regex_fixedData.Matches(File.ReadAllText(fi.FullName));
            if (matches_fixedData.Count > 0)
            {
                foreach (Match m in matches_fixedData)
                {
                    AddEntry(m.Groups[1].Value, fi.Name, sb, null);

                    if (m.Groups[2] != null)
                    {
                        AddEntry(m.Groups[2].Value, fi.Name, sb, null);
                    }
                }
            }

        }

        private static void AddEntry(string value, string filename, StringBuilder sb, StringBuilder js)
        {
            var v = value;
            if (!allEntries.Contains(v))
            {
                allEntries.Add(v);
                if (js == null)
                {
                    sb.AppendLine($"\"{v}\", //{filename}");
                }
                else
                {
                    sb.AppendLine($"\"{v.Replace("\"", "\\\"")}\", //{filename}");
                }
            }
            if (js != null)
            {
                if (!allJSEntries.Contains(v))
                {
                    allJSEntries.Add(v);
                    if (!v.Contains("'"))
                    {
                        js.AppendLine($"__translationTable['{v}'] = '@Html.Raw(Html.T(\"{v.Replace("\"", "\\\"")}\"))';");
                    }
                    else
                    {
                        js.AppendLine($"__translationTable[\"{v}\"] = '@Html.Raw(Html.T(\"{v.Replace("\"", "\\\"")}\"))';");
                    }
                }
            }

        }

    }
}
