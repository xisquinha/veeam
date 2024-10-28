using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;


/// <summary>
/// Class <c>SyncTask</c> synchronizes the content of the source folder to the replica folder.
/// For the building of this program was assumed that the source folder can have sub-folders,
/// and that the replica folder won't be changed by the user.
/// </summary>
class SyncTask{

    static string replicaPath;
    static string sourcePath;
    static string logPath;


    static void Main(string[] args){

        if(args.Length != 4)
            while(args.Length != 4){
                Console.WriteLine("Please input the information in this formmat:\nsourceFolderPath replicaFolderPath logFilePath syncInterval\n"+
                "(e.g.:source replica log.txt 01:00:00)");
                args = Console.ReadLine().Split(' ');
            }

        // read the data inputed by the user
        sourcePath = args[0];
        replicaPath = args[1];
        logPath = args[2];
        string syncTimeStr = args[3];

        string folderPathMsg = "The source folder's path doesn't exist.\nPlease try again.";
        string folderPathMsg2 = "The folder path has invalid characteres.\nPlease try again.";
        string filePathMsg = "The file isn't a text file.\nPlease try again.";
        string filePathMsg2 = "The file path has invalid characteres.\nPlease try again.";
        string syncTimePat = @"^\d{2}[:]\d{2}[:]\d{2}$";
        string syncTimeMsg = "The inserted synchronization interval doesn't follow the required format.\nFollow the example: 3 minutes - 00:03:00";
        
        // valid the data
        sourcePath = ValidatePathInput(folderPathMsg, 0,  sourcePath, folderPathMsg2);
        logPath = ValidatePathInput(filePathMsg, 1, logPath, filePathMsg2, "txt");
        syncTimeStr = ValidateInput(syncTimePat, syncTimeMsg, syncTimeStr);

        // creates the replica folder, unless it already exists
        Directory.CreateDirectory(replicaPath);

        SynchronizationLoop(syncTimeStr); // start synchronization loop
    }


    /// <summary>
    /// Validates the path inputed by the user
    /// </summary>
    /// <param name="invalidInputMsg">message that appears in the console output every time
    /// the user inserts a path for a file/folder that doesn't exist</param>
    /// <param name="dir_or_file"> 1 if it's expected a directory path;
    /// 0 if it's expected a file path</param>
    /// <param name="ext">if it is a file, the file extension</param>
    /// <returns>user's validated input</returns>
    static string ValidatePathInput(string invalidInputMsg, int dir_or_file, string path, string invalidInputMsg2, string ext = ""){

        string fileExt = path.Split('.')[path.Split('.').Length-1];;

        // if it is to validate a folder, it checks if it exists, if it doesn't exist, it asks
        // the user to input the folder's path again
        if(dir_or_file == 0)
            while(!Directory.Exists(path) || !IsValidFoldername(path)){
                if(!Directory.Exists(path))
                    Console.WriteLine(invalidInputMsg);
                else
                    Console.WriteLine(invalidInputMsg2);

                Console.WriteLine(invalidInputMsg);
                path = Console.ReadLine();
            }

        // if it is to validate a file, it checks if it exists and if the extension is the as the required one,
        // if it doesn't exist or the extension is different, it asks the user to input the file's path again
        else if(dir_or_file == 1){
            while(!fileExt.Equals(ext) || !IsValidFilename(path)){
                if(!fileExt.Equals(ext))
                    Console.WriteLine(invalidInputMsg);
                else
                    Console.WriteLine(invalidInputMsg2);

                path = Console.ReadLine();
                fileExt = path.Split('.')[path.Split('.').Length-1];
            }
            // creates the log file if it don't exist
            if(!File.Exists(path)){
                FileStream fs = File.Create(path);
                fs.Close();
            }
        }
        return path;
    }

    /// <summary>
    /// Tests if a file name is invalid
    /// </summary>
    /// <param name="testName">file name</param>
    /// <returns></returns>
    static bool IsValidFilename(string testName){
        string invalidFileNameChars = new string(Path.GetInvalidFileNameChars()); 
        Regex regInvalidFileName = new Regex("[" + Regex.Escape(invalidFileNameChars) + "]");
    
        if (regInvalidFileName.IsMatch(testName)) { return false; };
        return true;
    }
    /// <summary>
    /// Tests if a folder name is invalid
    /// </summary>
    /// <param name="testName">folder name</param>
    /// <returns></returns>
    static bool IsValidFoldername(string testName){
        string invalidFolderNameChars = new string(Path.GetInvalidPathChars()); 
        Regex regInvalidFolderName = new Regex("[" + Regex.Escape(invalidFolderNameChars) + "]");
    
        if (regInvalidFolderName.IsMatch(testName)) { return false; };
        return true;
    }

    /// <summary>
    /// Validates the user's input with a especific pattern
    /// </summary>
    /// <param name="pattern">the input needs to follow this pattern</param>
    /// <param name="invalidInputMsg">message that appears in the console output every time
    /// the user inserts an invalid input</param>
    /// <returns>user's validated input</returns>
    static string ValidateInput(string pattern, string invalidInputMsg, string input){

        while(!Regex.IsMatch(input, pattern)){
            Console.WriteLine(invalidInputMsg);
            input = Console.ReadLine();
        }
        return input;
    }



    /// <summary>
    /// Do the synchronization of the two folders, source folder and replica folder
    /// </summary>
    /// <param name="sourcePath">source folder's path</param>
    /// <param name="replicaPath">replica folder's path</param>
    /// <param name="syncTimeStr">time between synchronizations</param>
    static void SynchronizationLoop(string syncTimeStr){

        int secs = SyncTimeToSec(syncTimeStr);
        DateTime lastSync;

        DateTime syncStart = DateTime.Now;
        // since it is the first time running the synchronization, to match the source
        // folder's content and the replica folder's content, it needs to do a full copy
        // of the source folder
        CopyFolderFiles(sourcePath, replicaPath);

        lastSync = DateTime.Now;

        TimeSpan syncDuration = syncStart - DateTime.Now; // duration of the synchronization
        Thread.Sleep(secs*1000 - Convert.ToInt32(syncDuration.TotalMilliseconds));

        while(true){

            syncStart = DateTime.Now;

            // if the source folder, or any other folder/file have been 
            // modified, it updates the replica folder with those changes
            CheckIfChangedDir(sourcePath, lastSync);
            
            lastSync = DateTime.Now;

            syncDuration = syncStart - DateTime.Now; // duration of the synchronization
            Thread.Sleep(secs*1000 - Convert.ToInt32(syncDuration.TotalMilliseconds));
        }
    }



    /// <summary>
    /// Copies all the files and directories from one directory to another directory
    /// </summary>
    /// <param name="fromDirPath">directory that we want to copy from</param>
    /// <param name="toDirPath">directory that we want to copy to</param>
    static void CopyFolderFiles(string fromDirPath, string toDirPath){

        string[] files = Directory.GetFiles(fromDirPath); // all files in the directory
        string[] directories = Directory.GetDirectories(fromDirPath); // all directories in the directory

        // copy all files from one folder to another
        for(int i = 0; i < files.Length; i++){

            FileInfo file = new FileInfo(files[i]);

            // if a file with the same name already exists in the destination folder, the
            // user needs to tell us if he wants to replace it or keep it
            if(File.Exists(toDirPath+files[i].Substring(fromDirPath.Length))){

                string resp = "";

                while(!resp.Equals("y") && !resp.Equals("n")){
                    Console.WriteLine("\nA file with the name \""+files[i].Substring(fromDirPath.Length+1)+
                    "\" already exists in the destination folder.\nDo you want to replace it? (y/n)");
                    resp = Console.ReadLine();
                }

                // if the user wants to replace it, the old one is deleted and copies the file in question
                if(resp.Equals("y")){
                    File.Delete(toDirPath+files[i].Substring(fromDirPath.Length));
                    file.CopyTo(toDirPath+files[i].Substring(fromDirPath.Length));

                    WriteToLogAndConsole(2, toDirPath+files[i].Substring(fromDirPath.Length), DateTime.Now);
                    WriteToLogAndConsole(1, file.DirectoryName+"\\"+file.Name, DateTime.Now);
                }

            }else{

                file.CopyTo(toDirPath+(files[i].Substring(fromDirPath.Length)));
                WriteToLogAndConsole(1, file.DirectoryName+"\\"+file.Name, DateTime.Now);
            }
        }
        
        // copy all directories from one folder to another
        for(int i = 0; i < directories.Length; i++){


            string dirPath = toDirPath+directories[i].Substring(fromDirPath.Length);
            // if a directory with the same name already exists in the destination folder, the user needs
            // to tell us if he wants to replace it or keep it
            if(Directory.Exists(dirPath)){

                string resp = "";

                while(!resp.Equals("y") && !resp.Equals("n")){
                    Console.WriteLine("\nA directory with the name \""+directories[i].Substring(fromDirPath.Length+1)+
                    "\" already exists in the destination folder.\nDo you want to replace it? (y/n)");
                    resp = Console.ReadLine();
                }

                // if the user wants to replace it, the old one is deleted and copies the directory in question
                if(resp.Equals("y")){

                    try{
                        PrintAllFolderFiles(dirPath, 2);
                        Directory.Delete(dirPath, true);
                    }catch(Exception){
                        Console.WriteLine("\nWARNING! The program doesn't have permission to delete the folder \""+
                        dirPath+"\"."+
                        "\nPlease change the permission of the folder and try again.");
                    }
                    
                    Directory.CreateDirectory(dirPath);
                    CopyFolderFiles(fromDirPath+directories[i].Substring(fromDirPath.Length), dirPath);
                }
                
            }else{
                // If the folder doesn't exist in the replica's folder, it is created and all of
                // its files and sub-folders are copied and created, respectively
                Directory.CreateDirectory(dirPath);      
                CopyFolderFiles(fromDirPath+directories[i].Substring(fromDirPath.Length), dirPath);
            }
        }
    }


    /// <summary>
    /// Check for files inside a directory that have been modified since the last synchronization
    /// </summary>
    /// <param name="dirPath">path of the directory that will be verified from
    /// if any file or folder have been modified (source folder/sub-folder)</param>
    /// <param name="lastSync">date time of the last synchronization</param>
    static void CheckIfChangedDir(string dirPath, DateTime lastSync){

        // remove files from replica's folder if they no longer exist in the source folder
        UpdateRemovedFiles(dirPath, replicaPath+dirPath.Substring(sourcePath.Length));
        // remove folders from replica's folder if they no longer exist in the source folder
        UpdateRemovedFolders(dirPath, replicaPath+dirPath.Substring(sourcePath.Length));

        // check if any file has been modified or created after the last synchronization
        List<string> changedFiles = CheckForChangedFiles(dirPath, lastSync);

        for(int i = 0; i < changedFiles.Count; i++)
            // update and create the files in replica's folder, that have been created or
            // modified in the source folder
            UpdateAndCreateFile(changedFiles[i]);
                     

        string[] directories = Directory.GetDirectories(dirPath);
        
        foreach (string dir in directories){

            string folderNewPath = replicaPath+dir.Substring(sourcePath.Length);

            // if the folder doesn't exists in the replica folder, it is created and all its
            // files and sub-folders are copied
            if(!Directory.Exists(folderNewPath)){ 
       
                Directory.CreateDirectory(folderNewPath);
                CopyFolderFiles(dir, folderNewPath);              
            }    
            else
                CheckIfChangedDir(dir, lastSync);
        }
    }


    /// <summary>
    /// Update a file in the replica folder
    /// </summary>
    /// <param name="filePath">original file's path</param>
    static void UpdateAndCreateFile(string filePath){
        string destFolder = replicaPath+filePath.Substring(sourcePath.Length);

        // if a file with the same name already exists, it's deleted and 
        // copied the original one to the replica's folder
        if(File.Exists(destFolder)){
            File.Delete(destFolder);
            WriteToLogAndConsole(2, destFolder, DateTime.Now);
        }

        FileInfo inf = new FileInfo(filePath);
        inf.CopyTo(destFolder);
        WriteToLogAndConsole(1, inf.DirectoryName+"\\"+inf.Name, DateTime.Now);
    }


    /// <summary>
    /// Remove the files in folder2 that don't exist or have been renamed in folder1.
    /// </summary>
    /// <param name="checkPath">folder1, folder that we want to check for deleted files</param>
    /// <param name="targetPath">folder2, folder that we want to update</param>
    static void UpdateRemovedFiles(string checkPath, string targetPath){

        if(Directory.Exists(targetPath)){
            string[] filesOg = Directory.GetFiles(checkPath); // get files from folder1
            // update the array to just have the file's semi path
            // instead of "sourceFolder/fileName.ext" -> "/fileName.ext"
            for(int i = 0; i < filesOg.Length; i++)
                filesOg[i] = filesOg[i].Substring(sourcePath.Length);

            string[] filesRep = Directory.GetFiles(targetPath); // get files from folder2

            // check for files that exist in folder2 and not in folder1, and delete them in folder2
            foreach(string file in filesRep){
                if(!Array.Exists(filesOg, remFile => remFile.Equals(file.Substring(replicaPath.Length)))){
                    try{
                        File.Delete(file);
                        WriteToLogAndConsole(2, file, DateTime.Now);
                    }catch(Exception){
                        Console.WriteLine("\nWARNING! The program doesn't have permission to delete the file \""+file+"\"."+
                        "\nPlease change the permission of the file and try again.");
                    }
                }
            }
        }
    }


    /// <summary>
    /// Remove the folders, and all of it's files and sub-folders, in folder2 that don't exist in folder1.
    /// Don't exist can also mean that the folder have been renamed.
    /// </summary>
    /// <param name="checkPath">folder1, folder that we want to check for deleted files</param>
    /// <param name="targetPath">folder2, folder that we want to update</param>
    static void UpdateRemovedFolders(string checkPath, string targetPath){

        string[] dicsOg = Directory.GetDirectories(checkPath); // get folders from folder1
        // update the array to just have the folder's semi path
        // instead of "sourceFolder/thisFolder" -> "/thisFolder" or
        // "sourceFolder/thatFolder/thisFolder" -> "/thatFolder/thisFolder"
        for(int i = 0; i < dicsOg.Length; i++)
            dicsOg[i] = dicsOg[i].Substring(sourcePath.Length);

        string[] dicsRep = Directory.GetDirectories(targetPath); // get folders from folder2

        // check for folders that exist in folder2 and not in folder1, and delete them in folder2 and all of it's content
        foreach(string dic in dicsRep){
            if(!Array.Exists(dicsOg, remFolder => remFolder.Equals(dic.Substring(replicaPath.Length)))){
                try{
                    PrintAllFolderFiles(dic, 2);
                    Directory.Delete(dic, true);
                }catch(Exception){
                    Console.WriteLine("\nWARNING! The program doesn't have permission to delete the folder \""+dic+"\"."+
                    "\nPlease change the permission of the folder and try again.");
                }
            }
        }
    }


    /// <summary>
    /// Check which files have been modified in a specific directory
    /// since the last synchronization
    /// </summary>
    /// <param name="dirPath">folder's path that we want to check from</param>
    /// <param name="lastSync">date time of the last synchronization</param>
    /// <returns>all the files that have been modified after the last synchronization</returns>
    static List<string> CheckForChangedFiles(string dirPath, DateTime lastSync){

        string[] files = Directory.GetFiles(dirPath);
        List<string> changedFiles = new List<string>();

        for(int i = 0; i < files.Length; i++){

            // date and time of the last modification
            DateTime lastUpdate = File.GetLastWriteTime(files[i]);

            // if DateTime.Compare(lastSync, lastUpdate) < 0 the file has
            // been modified after the last synchronization
            if(DateTime.Compare(lastSync, lastUpdate) < 0){
                changedFiles.Add(files[i]);
            }
        }
        return changedFiles;
    }

    
    /// <summary>
    /// Converts time in the format hh:mm:ss to seconds   
    /// </summary>
    /// <param name="timeStr">time in hh:mm:ss</param>
    /// <returns>time in seconds</returns>
    static int SyncTimeToSec(string timeStr){

        string[] times = timeStr.Split(':');

        int hours = Int32.Parse(times[0]);
        int minutes = Int32.Parse(times[1]);
        int seconds = Int32.Parse(times[2]);

        return seconds+minutes*60+hours*3600;
    }


    /// <summary>
    /// Writes in the log file and in the console output all the files of a directory.
    /// This function will be used when a directory is deleted or copied, to print all
    /// the files that are in the folder and sub-folders
    /// </summary>
    /// <param name="directory">directory that has suffered the operation</param>
    /// <param name="operation">operation number, with the same meaning of
    /// the operation number in WriteToLogAndConsole()</param>
    static void PrintAllFolderFiles(string directory, int operation){
        string[] files = Directory.GetFiles(directory);
        foreach(string file in files)
            WriteToLogAndConsole(operation, file, DateTime.Now);

        string[] dics = Directory.GetDirectories(directory);
        foreach(string dic in dics)
            PrintAllFolderFiles(dic, operation);
    }


    /// <summary>
    /// Writes in the log file and in the console output the file's operations that have been made
    /// in the replica's folder
    /// </summary>
    /// <param name="operation">operation that have been made; 0 - creation; 1 - copy; 2 - removal</param>
    /// <param name="filePath">the file path in question</param>
    /// <param name="dt">the date time of the operation</param>
    static void WriteToLogAndConsole(int operation, string filePath, DateTime dt){

        string text = "";

        switch(operation){
            case 0:
                text = "Creation";
                break;
            case 1:
                text = "Copy";
                break;
            case 2:
                text = "Removal";
                break;
        }

        // writes in the log file
        using (StreamWriter outputFile = new StreamWriter(logPath, true)){
            outputFile.WriteLine(text+" of "+Path.GetFullPath(filePath)+" "+dt);
        }
        // writes in the output console
        Console.WriteLine(text+" of "+Path.GetFullPath(filePath)+" "+dt+"\n");
    }
}