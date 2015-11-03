using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

/// <summary>
/// Helper for file io
/// </summary>
static class FileIOHelper
{

    /// <summary>
    /// Ensures the specified path exists 
    /// </summary>
    /// <param name="localPath"></param>
    public static void CreatePathIfNeeded(string localPath)
    {
        if (Directory.Exists(localPath)) return;
        Directory.CreateDirectory(localPath);
    }


    public static string GenerateWindowsSafeFilename(string fileNameIn)
    {
        string fileNameOut = fileNameIn;
        fileNameOut = fileNameOut.Replace("\\", "-SLASH-");
        fileNameOut = fileNameOut.Replace("/", "-SLASH-");
        fileNameOut = fileNameOut.Replace("$", "-DOLLAR-");
        fileNameOut = fileNameOut.Replace("*", "STAR");
        fileNameOut = fileNameOut.Replace("?", "-QQQ-");
        fileNameOut = fileNameOut.Replace("%", "-PERCENT-");
        fileNameOut = fileNameOut.Replace(":", "-COLON-");
        fileNameOut = fileNameOut.Replace("|", "-PIPE-");
        fileNameOut = fileNameOut.Replace("\"", "-QUOTE-");
        fileNameOut = fileNameOut.Replace(">", "-GT-");
        fileNameOut = fileNameOut.Replace("<", "-LT-");
        return fileNameOut;
    }

    /// <summary>
    /// Creates a high-probabilty-unique path based on the current date-time
    /// </summary>
    /// <param name="basePath"></param>
    /// <returns></returns>
    public static string PathDateTimeSubdirectory(string basePath, bool createDirectory, string newDirectoryPrefix = "", Nullable<DateTime> when = null)
    {
        //Subdirectory name
        DateTime now;
        if(when.HasValue)
        {
            now = when.Value;
        }
        else
        {
            now = DateTime.Now;
        }

        string subDirectory = now.Year.ToString() + "-" + now.Month.ToString("00") + "-" + now.Day.ToString("00") + "-" + now.Hour.ToString("00") + now.Minute.ToString("00") + "-" + now.Second.ToString("00");
        if(!string.IsNullOrWhiteSpace(newDirectoryPrefix))
        {
            subDirectory = newDirectoryPrefix + subDirectory;
        }

        //Combined path
        string fullPathToDateTime = Path.Combine(basePath, subDirectory);
        //Create if specified
        if (createDirectory)
        {
            CreatePathIfNeeded(fullPathToDateTime);
        }
        return fullPathToDateTime;
    }


    /// <summary>
    /// Gives us a high probability unqique file name
    /// </summary>
    /// <param name="baseName"></param>
    /// <returns></returns>
    public static string FilenameWithDateTimeUnique(string baseName, Nullable<DateTime> when = null)
    {
        string rootName = Path.GetFileNameWithoutExtension(baseName);
        string extension = Path.GetExtension(baseName);

        //Subdirectory name

        DateTime now;
        if(when.HasValue)
        {
            now = when.Value;
        }
        else
        {
            now = DateTime.Now;
        }

        string subNameDateTime = now.Year.ToString() + "-" + now.Month.ToString("00") + "-" + now.Day.ToString("00") + "-" + now.Hour.ToString("00") + now.Minute.ToString("00") + "-" + now.Second.ToString("00");

        //Combined path
        return rootName + "_" + subNameDateTime + extension;
    }

        /// <summary>
    /// If we have Project Mapping information, generate a project based path for the download
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="projectList"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static string EnsureProjectBasedPath(string basePath, IProjectsList projectList, IHasProjectId project, TaskStatusLogs statusLog)
    {
        //If we have no project list to do lookups in then just return the base path
        if (projectList == null) return basePath;

        //Look up the project name
        var projWithId = projectList.FindProjectWithId(project.ProjectId);
        if(projWithId == null)
        {
            statusLog.AddError("Project not found with id " + project.ProjectId);
            return basePath;
        }

        //Turn the project name into a directory name
        var safeDirectoryName = GenerateWindowsSafeFilename(projWithId.Name);

        var pathWithProject = Path.Combine(basePath, safeDirectoryName);
        //If needed, create the directory
        if(!Directory.Exists(pathWithProject))
        {
            Directory.CreateDirectory(pathWithProject);
        }

        return pathWithProject;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="directoryName"></param>
    /// <returns></returns>
    internal static string ReverseGenerateWindowsSafeFilename(string directoryName)
    {
        //[2015-03-20] UNDONE: Unmunge any escape characters we baked into the directory name
        return directoryName;
    }
}
