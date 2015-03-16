using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Assets.Editor
{
    public interface IFrmNfoLineProcessor
    {
        string Process(string[] lines);
    }
    public class FrmNfoLineProcessor : IFrmNfoLineProcessor
    {
        private readonly FrmNfo[] _frameworks;
        private static readonly Regex BeginMainRegex = new Regex(@"\/\* Begin (?<name>[^ ]*) section \*\/");
        private static readonly Regex EndMainRegex = new Regex(@"\/\* End (?<name>[^ ]*) section \*\/");
        private static readonly Regex BeginSubRegex = new Regex(@"(?<name>[^=]*) = [\{\(]{1,1}$");
        private readonly HashSet<string> Groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> SubGroups = new Stack<string>();

        private string _newGroup = null;
        private string _newSubGroup = null;
        private string _closingSubGroup = null;
        private FrmSection _added = FrmSection.None;

        public FrmNfoLineProcessor(FrmNfo[] frameworks)
        {
            _frameworks = frameworks;
        }
        string IFrmNfoLineProcessor.Process(string[] lines)
        {
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                _newGroup = null;
                _newSubGroup = null;
                var trimmedLine = line.Trim();
                CheckForMainGroupChange(trimmedLine);
                CheckForSubGroupChange(trimmedLine);

                OnPreAppendLine(sb);
                sb.AppendLine(line);
                OnPostAppendLine(sb);
            }
            return sb.ToString();
        }
        private void CheckForMainGroupChange(string trimmedLine)
        {
            var metchBegin = BeginMainRegex.Match(trimmedLine);
            if (metchBegin.Success)
            {
                var name = metchBegin.Groups["name"].Value;
                OnBeginGroup(name);
            }
            else
            {
                var metchEnd = EndMainRegex.Match(trimmedLine);
                if (metchEnd.Success)
                {
                    var name = metchEnd.Groups["name"].Value;
                    OnEndGroup(name);
                }
            }
        }
        private void CheckForSubGroupChange(string trimmedLine)
        {
            var metchBegin = BeginSubRegex.Match(trimmedLine);
            if (metchBegin.Success)
            {
                var name = metchBegin.Groups["name"].Value;
                OnBeginSubGroup(name);
            }
            else
            {
                if (trimmedLine == ");" || trimmedLine == "};")
                {
                    OnEndSubGroup();
                }
            }
        }
        private void OnBeginGroup(string name)
        {
            _newGroup = name;
            Groups.Add(name);
        }
        private void OnEndGroup(string name)
        {
            Groups.Remove(name);
        }
        private void OnBeginSubGroup(string name)
        {
            _newSubGroup = name;
            SubGroups.Push(name);
        }
        private void OnEndSubGroup()
        {
            var name = SubGroups.Pop();
            _closingSubGroup = name;
        }

        private void OnPreAppendLine(StringBuilder sb)
        {
            TryAddToXCBuildConfigurationDebugObjC(sb);
            TryAddToXCBuildConfigurationReleaseObjC(sb);
        }
        private void OnPostAppendLine(StringBuilder sb)
        {

            TryAddToPBXBuildFile(sb);
            TryAddToPBXFileReference(sb);
            TryAddToPBXFrameworksBuildPhase(sb);
            TryAddToPBXGroupCustomTemplate(sb);
            TryAddToPBXGroupFrameworks(sb);
            TryAddToPBXResourcesBuildPhase(sb);
            TryAddToXCBuildConfigurationDebug(sb);
            TryAddToXCBuildConfigurationRelease(sb);
        }
        private void TryAddToPBXBuildFile(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXBuildFile) != 0) 
                return;

            if (_newGroup == "PBXBuildFile")
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXBuildFile) == 0) 
                        continue;

                    sb.Append('\t').Append('\t')
                      .Append(f.ID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" in ")
                      .Append(f.In)
                      .Append(" */ = {isa = PBXBuildFile; fileRef = ")
                      .Append(f.FileID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" */; };")
                      .AppendLine();
                }
                _added |= FrmSection.PBXBuildFile;
            }
        }
        private void TryAddToPBXFileReference(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXFileReference) != 0) 
                return;

            if (_newGroup == "PBXFileReference")
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXFileReference) == 0) 
                        continue;

                    sb.Append('\t').Append('\t')
                      .Append(f.FileID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" */ = {isa = PBXFileReference; lastKnownFileType = ")
                      .Append(f.FileType)
                      .Append("; name = ")
                      .Append(f.Name.Contains('+') ? "\""+f.Name+"\"" : f.Name)
                      .Append("; path = ")
                      .Append(f.Path)
                      .Append("; sourceTree = ")
                      .Append(f.Src)
                      .Append("; };")
                      .AppendLine();
                }
                _added |= FrmSection.PBXFileReference;
            }
        }
        private void TryAddToPBXFrameworksBuildPhase(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXFrameworksBuildPhase) != 0) 
                return;

            if (_newSubGroup == "files" && 
                Groups.Contains("PBXFrameworksBuildPhase") &&
                SubGroups.Skip(1).First().Contains("Frameworks"))
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXFrameworksBuildPhase) == 0) 
                        continue;

                    sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .Append(f.ID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" in Frameworks */,")
                      .AppendLine();
                }
                _added |= FrmSection.PBXFrameworksBuildPhase;
            }
        }
        private void TryAddToPBXGroupCustomTemplate(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXGroupCustomTemplate) != 0) 
                return;

            if (_newSubGroup == "children" && 
                Groups.Contains("PBXGroup") &&
                SubGroups.Skip(1).First().Contains("CustomTemplate"))
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXGroupCustomTemplate) == 0) 
                        continue;

                    sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .Append(f.FileID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" */,")
                      .AppendLine();
                }
                _added |= FrmSection.PBXGroupCustomTemplate;
            }
        }
        private void TryAddToPBXGroupFrameworks(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXGroupFrameworks) != 0)
                return;

            if (_newSubGroup == "children" && 
                Groups.Contains("PBXGroup") &&
                SubGroups.Skip(1).First().Contains("Frameworks"))
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXGroupFrameworks) == 0) 
                        continue;

                    sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .Append(f.FileID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" */,")
                      .AppendLine();
                }
                _added |= FrmSection.PBXGroupFrameworks;
            }
        }
        private void TryAddToPBXResourcesBuildPhase(StringBuilder sb)
        {
            if ((_added & FrmSection.PBXResourcesBuildPhase) != 0) 
                return;

            if (_newSubGroup == "files" && 
                Groups.Contains("PBXResourcesBuildPhase") &&
                SubGroups.Skip(1).First().Contains("Resources"))
            {
                foreach (var f in _frameworks)
                {
                    if((f.Section & FrmSection.PBXResourcesBuildPhase) == 0) 
                        continue;

                    sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .Append(f.ID)
                      .Append(" /* ")
                      .Append(f.Name)
                      .Append(" in Resources */,")
                      .AppendLine();
                }
                _added |= FrmSection.PBXResourcesBuildPhase;
            }
        }
        private void TryAddToXCBuildConfigurationDebug(StringBuilder sb)
        {
            if ((_added & FrmSection.XCBuildConfigurationDebug) != 0) 
                return;

            if (_newSubGroup == "buildSettings" && 
                Groups.Contains("XCBuildConfiguration") &&
                SubGroups.Skip(1).First().Contains("Debug"))
            {
                sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("FRAMEWORK_SEARCH_PATHS = (")
                  .Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("\"$(inherited)\",")
                  .Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("/Users/vladimirbodurov/Desktop/Code/GoogleLibs,")
                  .Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine(");");

                _added |= FrmSection.XCBuildConfigurationDebug;
            }
        }
        private void TryAddToXCBuildConfigurationDebugObjC(StringBuilder sb)
        {
            if ((_added & FrmSection.XCBuildConfigurationDebugObjC) != 0) 
                return;

            if (_closingSubGroup == "OTHER_LDFLAGS" && 
                Groups.Contains("XCBuildConfiguration") &&
                SubGroups.Skip(1).First().Contains("Debug"))
            {
                sb.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .Append("\"-ObjC\",")
                      .AppendLine();

                _added |= FrmSection.XCBuildConfigurationDebugObjC;
            }
        }
        private void TryAddToXCBuildConfigurationRelease(StringBuilder sb)
        {
            if ((_added & FrmSection.XCBuildConfigurationRelease) != 0) 
                return;

            if (_newSubGroup == "buildSettings" && 
                Groups.Contains("XCBuildConfiguration") &&
                SubGroups.Skip(1).First().Contains("Release"))
            {
                sb.Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("FRAMEWORK_SEARCH_PATHS = (")
                  .Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("\"$(inherited)\",")
                  .Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("/Users/vladimirbodurov/Desktop/Code/GoogleLibs,")
                  .Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine(");");

                _added |= FrmSection.XCBuildConfigurationRelease;
            }
        }
        private void TryAddToXCBuildConfigurationReleaseObjC(StringBuilder sb)
        {
            if ((_added & FrmSection.XCBuildConfigurationReleaseObjC) != 0)
                return;

            if (_closingSubGroup == "OTHER_LDFLAGS" && 
                Groups.Contains("XCBuildConfiguration") &&
                SubGroups.Skip(1).First().Contains("Release"))
            {
                sb.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                      .AppendLine("\"-ObjC\",");

                _added |= FrmSection.XCBuildConfigurationReleaseObjC;
            }
        }
    }
    public class FrmNfo
    {
        public string Name;
        public string ID;
        public string In;
        public string FileID;
        public string Path;
        public string Src;
        public string FileType;
        public FrmSection Section;
    }
    [Flags]
    public enum FrmSection
    {
        None = 0,
        PBXBuildFile = 1 << 0,
        PBXFileReference = 1 << 1,
        PBXFrameworksBuildPhase = 1 << 2,
        PBXGroupCustomTemplate = 1 << 3,
        PBXGroupFrameworks = 1 << 4,
        PBXResourcesBuildPhase = 1 << 5,
        XCBuildConfigurationDebug = 1 << 6,
        XCBuildConfigurationDebugObjC = 1 << 7,
        XCBuildConfigurationRelease = 1 << 8,
        XCBuildConfigurationReleaseObjC = 1 << 9
    }


}