using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;


namespace Assets.Editor
{
    public static class PostBuildTrigger
    {
        [PostProcessBuild] // <- this is where the magic happens
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            // 1: Check this is an iOS build before running
#if UNITY_IPHONE
            string appPath = Application.dataPath;
            var arr = appPath.Split(new[] {'/', '\\'}, StringSplitOptions.None);

            //code:/Users/vladimirbodurov/Desktop/Code/
//          var codeRoot = arr.Take(arr.Length - 4).ToArr().JoinAsString("/")+"/";

            //unityRoot:/Users/vladimirbodurov/Desktop/Code/GitHub/multiplayer-experiment/CircuitRacerBegin/
            var unityRoot = arr.Take(arr.Length - 1).ToArr().JoinAsString("/")+"/";
               
            Debug.Log("OnPostProcessBuild - START") ;
            UpdateXcodeProject(unityRoot+"CircuitRacer_iOS/Unity-iPhone.xcodeproj") ;

#else
        // 3: We do nothing if not iPhone
            Debug.Log("OnPostProcessBuild - Warning: This is not an iOS build") ;
#endif     
            Debug.Log("OnPostProcessBuild - FINISHED") ;
        }

        private static void UpdateXcodeProject(string path)
        {
                        var data = @"
69AC98D51AB530AB006C7BE9;AddressBook.framework;Frameworks;69AC98CD1AB530AB006C7BE9;System/Library/Frameworks/AddressBook.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98D61AB530AB006C7BE9;AssetsLibrary.framework;Frameworks;69AC98CE1AB530AB006C7BE9;System/Library/Frameworks/AssetsLibrary.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98D71AB530AB006C7BE9;CoreData.framework;Frameworks;69AC98CF1AB530AB006C7BE9;System/Library/Frameworks/CoreData.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98D81AB530AB006C7BE9;CoreTelephony.framework;Frameworks;69AC98D01AB530AB006C7BE9;System/Library/Frameworks/CoreTelephony.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98D91AB530AB006C7BE9;CoreText.framework;Frameworks;69AC98D11AB530AB006C7BE9;System/Library/Frameworks/CoreText.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98DA1AB530AB006C7BE9;libc++.dylib;Frameworks;69AC98D21AB530AB006C7BE9;""usr/lib/libc++.dylib"";SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;""compiled.mach-o.dylib""
69AC98DB1AB530AB006C7BE9;libz.dylib;Frameworks;69AC98D31AB530AB006C7BE9;usr/lib/libz.dylib;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;""compiled.mach-o.dylib""
69AC98DC1AB530AB006C7BE9;Security.framework;Frameworks;69AC98D41AB530AB006C7BE9;System/Library/Frameworks/Security.framework;SDKROOT;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupFrameworks;wrapper.framework
69AC98DE1AB54133006C7BE9;gpg.framework;Frameworks;69AC98DD1AB54133006C7BE9;../../../../GoogleLibs/gpg.framework;<group>;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupCustomTemplate;wrapper.framework
69AC98E01AB5413C006C7BE9;GooglePlus.framework;Frameworks;69AC98DF1AB5413C006C7BE9;../../../../GoogleLibs/GooglePlus.framework;<group>;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupCustomTemplate;wrapper.framework
69AC98E21AB54143006C7BE9;GooglePlus.bundle;Resources;69AC98E11AB54143006C7BE9;../../../../GoogleLibs/GooglePlus.bundle;<group>;PBXBuildFile|PBXFileReference|PBXGroupCustomTemplate|PBXResourcesBuildPhase;""wrapper.plug-in""
69AC98E41AB5414C006C7BE9;GooglePlayGames.bundle;Resources;69AC98E31AB5414C006C7BE9;../../../../GoogleLibs/GooglePlayGames.bundle;<group>;PBXBuildFile|PBXFileReference|PBXGroupCustomTemplate|PBXResourcesBuildPhase;""wrapper.plug-in""
69AC98E61AB54154006C7BE9;GoogleOpenSource.framework;Frameworks;69AC98E51AB54154006C7BE9;../../../../GoogleLibs/GoogleOpenSource.framework;<group>;PBXBuildFile|PBXFileReference|PBXFrameworksBuildPhase|PBXGroupCustomTemplate;wrapper.framework
        ";

            var frameworks = data.Trim()
                .Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Contains(";"))
                .Select(s => s.Split(';'))
                .Select<string[],FrmNfo>(ArrayToFrmNfo)
                .ToArr();

            var lines = File.ReadAllLines(path);

            IFrmNfoLineProcessor processor = new FrmNfoLineProcessor(frameworks);
            var result = processor.Process(lines);

            File.WriteAllText(path, result);

        }

        public static FrmNfo ArrayToFrmNfo(string[] arr)
        {
            return new FrmNfo
                   {
                       ID = arr[0], Name = arr[1], In = arr[2],
                       FileID = arr[3], Path = arr[4], Src = arr[5],
                       Section = arr[6]
                           .Split('|')
                           .Select(s => (FrmSection)Enum.Parse(typeof(FrmSection), s.Trim()))
                           .Aggregate(FrmSection.None, (o,n) => o | n),
                       FileType = arr[7]
                   };
        }
    }
}