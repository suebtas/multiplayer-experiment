using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using Assets;


namespace Assets.Editor
{
    public static class PostBuildTrigger
    {
        private static readonly string[,] LIBS =
        {
            {"AddressBook.framework","693BA8AB1AB4D1FE0014E6E0","693BA8A31AB4D1FE0014E6E0"},
            {"AssetsLibrary.framework","693BA8AC1AB4D1FE0014E6E0","693BA8A41AB4D1FE0014E6E0"},
            {"CoreData.framework","693BA8AD1AB4D1FE0014E6E0","693BA8A51AB4D1FE0014E6E0"},
            {"CoreTelephony.framework","693BA8AE1AB4D1FE0014E6E0","693BA8A61AB4D1FE0014E6E0"},
            {"CoreText.framework","693BA8AF1AB4D1FE0014E6E0","693BA8A71AB4D1FE0014E6E0"},
            {"libc++.dylib","693BA8B01AB4D1FE0014E6E0","693BA8A81AB4D1FE0014E6E0"},
            {"libz.dylib","693BA8B11AB4D1FE0014E6E0","693BA8A91AB4D1FE0014E6E0"},
            {"Security.framework","693BA8B21AB4D1FE0014E6E0","693BA8AA1AB4D1FE0014E6E0"},
            // google frameworks:
            {"GoogleOpenSource.framework","693BA8BA1AB4D36B0014E6E0","693BA8B71AB4D36B0014E6E0"},
            {"GooglePlayGames.bundle","693BA8B51AB4D3390014E6E0","693BA8B31AB4D3390014E6E0"},
            {"GooglePlus.bundle",   "693BA8BB1AB4D36B0014E6E0","693BA8B81AB4D36B0014E6E0"},
            {"GooglePlus.framework","693BA8BC1AB4D36B0014E6E0","693BA8B91AB4D36B0014E6E0"},
            {"gpg.framework","693BA8B61AB4D3390014E6E0","693BA8B41AB4D3390014E6E0"},
        };

        public struct framework
        {
            public string sName ;
            public string sId ;
            public string sFileId ;
       
            public framework(string name, string myId, string fileid)
            {
                sName = name ;
                sId = myId ;
                sFileId = fileid ;
            }
        }

        

        [PostProcessBuild] // <- this is where the magic happens
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            // 1: Check this is an iOS build before running
#if UNITY_IPHONE
            {
                // 2: We init our tab and process our project
                var myFrameworks = new List<framework>();

                for (var i = 0; i < LIBS.GetLength(0); ++i)
                {
                    myFrameworks.Add(new framework(LIBS[i, 0], LIBS[i, 1], LIBS[i, 2]));
                }
// Application.dataPath:/Users/vladimirbodurov/Desktop/Code/GitHub/multiplayer-experiment/CircuitRacerBegin/Assets
                string appPath = Application.dataPath;
                var arr = appPath.Split(new[] {'/', '\\'}, StringSplitOptions.None);

                //code:/Users/vladimirbodurov/Desktop/Code/
                var codeRoot = arr.Take(arr.Length - 4).ToArr().JoinAsString("/")+"/";

                //unityRoot:/Users/vladimirbodurov/Desktop/Code/GitHub/multiplayer-experiment/CircuitRacerBegin/
                var unityRoot = arr.Take(arr.Length - 1).ToArr().JoinAsString("/")+"/";

                TryCopyGoogleBundles(codeRoot+"GoogleLibs", unityRoot+"CircuitRacer_iOS");
               
                Debug.Log("OnPostProcessBuild - START") ;
                UpdateXcodeProject(unityRoot+"CircuitRacer_iOS/Unity-iPhone.xcodeproj", myFrameworks) ;

                //UpdateInfoPlist(xcodeInfoPlist);
            }
#else
        // 3: We do nothing if not iPhone
            Debug.Log("OnPostProcessBuild - Warning: This is not an iOS build") ;
#endif     
            Debug.Log("OnPostProcessBuild - FINISHED") ;
        }

        private static void TryCopyGoogleBundles(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError("Source directiory does not exist:"+sourcePath);
                return;
            }
            if (!Directory.Exists(destinationPath))
            {
                Debug.LogError("Destination directiory does not exist:"+destinationPath);
                return;
            }

            CopyAll(sourcePath, destinationPath, 0);

        }

        private static void CopyAll(string sourcePath, string destinationPath, int level)
        {
            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                var dirName = Path.GetDirectoryName(dir);
                var dirToLower = dir.ToLower();
                var targetDir = Path.Combine(destinationPath, dirName);
                if (level > 0 || dirToLower.EndsWith(".bundle") || dirToLower.EndsWith(".framework"))
                {
                    Directory.CreateDirectory(Path.Combine(destinationPath, dirName));
                }
                CopyAll(dir, targetDir, level + 1);
            }
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(destinationPath, fileName);
                File.Copy(file, targetFile);
            }
        }

        public static void UpdateXcodeProject(string xcodeprojPath, IList<framework> listFrameworks)
        {
            // STEP 1 : We open up the file generated by Unity and read into memory as
            // a list of lines for processing
            string project = xcodeprojPath + "/project.pbxproj" ;
            string[] lines = System.IO.File.ReadAllLines(project);
       
            // STEP 2 : We check if file has already been processed and only proceed if it hasn't,
            // we'll do this by looping through the build files and see if CoreTelephony.framework
            // is there
            int i = 0 ;
            bool bFound = false ;
            bool bEnd = false ;
            while ( !bFound && !bEnd)
            {
                if (lines[i].Length > 5 && (String.Compare(lines[i].Substring(3, 3), "End") == 0) )
                    bEnd = true ;

                foreach (var fr in listFrameworks)
                {
                    bFound = lines[i].Contains(fr.sName);
                    if(bFound) break;
                }
                ++i ;
            }
            if (bFound)
                Debug.Log("OnPostProcessBuild - ERROR: Frameworks have already been added to XCode project") ;
            else
            {
                // STEP 3 : We'll open/replace project.pbxproj for writing and iterate over the old
                // file in memory, copying the original file and inserting every extra we need
                FileStream filestr = new FileStream(project, FileMode.Create); //Create new file and open it for read and write, if the file exists overwrite it.
                filestr.Close() ;
                StreamWriter fCurrentXcodeProjFile = new StreamWriter(project) ; // will be used for writing
           
                // As we iterate through the list we'll record which section of the
                // project.pbxproj we are currently in
                string section = "" ;
 
                // We use this boolean to decide whether we have already added the list of
                // build files to the link line.  This is needed because there could be multiple
                // build targets and they are not named in the project.pbxproj
                bool bFrameworks_build_added = false ;
                int iNbBuildConfigSet = 0 ; // can't be > 2
       
                i = 0 ;
                foreach (string line in lines)
                {
                    if (line.StartsWith("\t\t\t\tGCC_ENABLE_CPP_EXCEPTIONS") ||
                        line.StartsWith("\t\t\t\tGCC_ENABLE_CPP_RTTI") ||
                        line.StartsWith("\t\t\t\tGCC_ENABLE_OBJC_EXCEPTIONS") )
                    {
                        // apparently, we don't copy those lines in our new project
                    }
                    else
                    {                          
    //////////////////////////////
    //  STEP 1 : Build Options  //
    //////////////////////////////
    //
    // TapJoy needs "Enable Object-C Exceptions" to be set to "YES"
    //
    //////////////////////////////
 
    // This one is special, we have to replace a line and not write after
            //          if ( section == "XCBuildConfiguration"  line.Trim().StartsWith("\t\t\t\tGCC_ENABLE_OBJC_EXCEPTIONS") )
            //              fCurrentXcodeProjFile.Write("\t\t\t\tGCC_ENABLE_OBJC_EXCEPTIONS = YES;\n") ;
 
    // in any other situation, we'll first copy the line in our new project, then we might do something special regarding that line
            //              else
                        fCurrentXcodeProjFile.WriteLine(line) ;
                   
                   
 
    //////////////////////////////////
    //  STEP 2 : Include Framewoks  //
    //////////////////////////////////
    //
    // TapJoy needs CoreTelephony (in weak-link = "optional")
    //
    //////////////////////////////////
 
    // Each section starts with a comment such as : /* Begin PBXBuildFile section */'
                        if ( lines[i].Length > 7 && String.Compare(lines[i].Substring(3, 5), "Begin") == 0  )
                        {
                            section = line.Split(' ')[2] ;
                            //Debug.Log("NEW_SECTION: "+section) ;
                            if (section == "PBXBuildFile")
                            {
                                foreach (framework fr in listFrameworks)
                                    AddBuildFile(fCurrentXcodeProjFile, fr.sId, fr.sName, fr.sFileId) ;
                            }
                       
                            if (section == "PBXFileReference")
                            {
                                foreach (framework fr in listFrameworks)
                                    AddFrameworkFileReference(fCurrentXcodeProjFile, fr.sFileId, fr.sName) ;
                            }
                       
                            if (line.Length > 5 && String.Compare(line.Substring(3, 3), "End") == 0)
                                section = "" ;
                        }
    // The PBXResourcesBuildPhase section is what appears in XCode as 'Link
    // Binary With Libraries'.  As with the frameworks we make the assumption the
    // first target is always 'Unity-iPhone' as the name of the target itself is
    // not listed in project.pbxproj               
                        if (section == "PBXFrameworksBuildPhase" &&
                            line.Trim().Length > 4 &&
                            String.Compare(line.Trim().Substring(0, 5) , "files") == 0 &&
                            !bFrameworks_build_added)
                        {
                            foreach (framework fr in listFrameworks)
                                AddFrameworksBuildPhase(fCurrentXcodeProjFile, fr.sId, fr.sName) ;
                            bFrameworks_build_added = true ;
                        }
                   
    // The PBXGroup is the section that appears in XCode as 'Copy Bundle Resources'.           
                        if (section == "PBXGroup" &&
                            line.Trim().Length > 7 &&
                            String.Compare(line.Trim().Substring(0, 8) , "children") == 0 &&
                            lines[i-2].Trim().Split(' ').Length > 0 &&
                            String.Compare(lines[i-2].Trim().Split(' ')[2] , "CustomTemplate" ) == 0 )
                        {
                            foreach (framework fr in listFrameworks)
                                AddGroup(fCurrentXcodeProjFile, fr.sFileId, fr.sName) ;
                        }
 
    //////////////////////////////
    //  STEP 3 : Build Options  //
    //////////////////////////////
    //
    // AdColony needs "Other Linker Flags" to have "-all_load -ObjC" added to its value
    //
    //////////////////////////////
                        if (section == "XCBuildConfiguration" &&
                            line.StartsWith("\t\t\t\tOTHER_LDFLAGS") &&
                            iNbBuildConfigSet < 2)
                        {
                            //fCurrentXcodeProjFile.Write("\t\t\t\t\t\"-all_load\",\n") ;
                            fCurrentXcodeProjFile.Write("\t\t\t\t\t\"-ObjC\",\n") ;
                            Debug.Log("OnPostProcessBuild - Adding \"-ObjC\" flag to build options") ; // \"-all_load\" and
                            ++iNbBuildConfigSet ;
                        }
                    }
                    ++i ;
                }
                fCurrentXcodeProjFile.Close() ;
            }
        }
   
       
        /////////////////
        ///////////
        // ROUTINES
        ///////////
        /////////////////
 
   
        // Adds a line into the PBXBuildFile section
        private static void AddBuildFile(StreamWriter file, string id, string name, string fileref)
        {
            Debug.Log("OnPostProcessBuild - Adding build file " + name) ;
            string subsection = "Frameworks" ;
       
//            if (name == "CoreTelephony.framework")  // CoreTelephony.framework should be weak-linked
//                file.Write("\t\t"+id+" /* "+name+" in "+subsection+" */ = {isa = PBXBuildFile; fileRef = "+fileref+" /* "+name+" */; settings = {ATTRIBUTES = (Weak, ); }; };") ;
//            else // Others framework are normal
                file.Write("\t\t"+id+" /* "+name+" in "+subsection+" */ = {isa = PBXBuildFile; fileRef = "+fileref+" /* "+name+" */; };\n") ;
        }
   
        // Adds a line into the PBXBuildFile section
        private static void AddFrameworkFileReference(StreamWriter file, string id, string name)
        {
            Debug.Log("OnPostProcessBuild - Adding framework file reference " + name) ;
       
            string path = "System/Library/Frameworks" ; // all the frameworks come from here
            if (name == "libsqlite3.0.dylib")           // except for lidsqlite
                path = "usr/lib" ;
       
            file.Write("\t\t"+id+" /* "+name+" */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = "+name+"; path = "+path+"/"+name+"; sourceTree = SDKROOT; };\n") ;
        }
   
        // Adds a line into the PBXFrameworksBuildPhase section
        private static void AddFrameworksBuildPhase(StreamWriter file, string id, string name)
        {
            Debug.Log("OnPostProcessBuild - Adding build phase " + name) ;
       
            file.Write("\t\t\t\t"+id+" /* "+name+" in Frameworks */,\n") ;
        }
   
        // Adds a line into the PBXGroup section
        private static void AddGroup(StreamWriter file, string id, string name)
        {
            Debug.Log("OnPostProcessBuild - Add group " + name) ;
       
            file.Write("\t\t\t\t"+id+" /* "+name+" */,\n") ;
        }


    }
}