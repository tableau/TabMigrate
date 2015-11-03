using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Xml;
class TwbxDataSourceEditor
{
    private readonly string _pathToTwbx;
    private readonly string _pathToWorkIn;
    private readonly TaskStatusLogs _statusLog;
    private readonly ITableauServerSiteInfo _serverInfo;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pathTwbx">TWBX we are going to unpack</param>
    /// <param name="workingDirectory"></param>
    public TwbxDataSourceEditor(string pathTwbx, string workingDirectory, ITableauServerSiteInfo serverInfo, TaskStatusLogs statusLog)
    {
        _pathToTwbx = pathTwbx;
        _pathToWorkIn = workingDirectory;
        _statusLog = statusLog;
        _serverInfo = serverInfo;

        if (!File.Exists(_pathToTwbx))
        {
            throw new ArgumentException("Original file does not exist " + _pathToTwbx);
        }
    }


    /// <summary>
    /// The subdirectory we are unzipping to
    /// </summary>
    public string UnzipDirectory
    {
        get
        {
            return Path.Combine(_pathToWorkIn, "unzipped");
        }
    }

    /// <summary>
    /// The path to the output
    /// </summary>
    public string OutputDirectory
    {
        get
        {
            return Path.Combine(_pathToWorkIn, "output");
        }
    }

    /// <summary>
    /// The name and path of the TWBX file we are generating
    /// </summary>
    public string OutputFileWithPath
    {
        get
        {
            return Path.Combine(OutputDirectory, TwbxFileName);
        }
    }

    /// <summary>
    /// The filename
    /// </summary>
    public string TwbxFileName
    {
        get
        {
            return Path.GetFileName(_pathToTwbx);
        }
    }

    /// <summary>
    /// There should only be one *.twb file in the unzipped set of files
    /// </summary>
    /// <returns></returns>
    private string GetPathToUnzippedTwb()
    {
        var twbFiles = Directory.EnumerateFiles(this.UnzipDirectory, "*.twb");
        
        foreach (var twb in twbFiles)
        {
            return twb;
        }

        _statusLog.AddError("Twb editor; no twb file found");
        return null;
    }


    private static bool CreateDirectoryIfNeeded(string path)
    {
        if(Directory.Exists(path)) return false;

        Directory.CreateDirectory(path);
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Path to the ZIPed fixed up TWBX</returns>
    public string Execute()
    {
        //1. Create temp directory to decompress file to
        //      - Create subdirectories for 'unzipped', 're-zipped'
        //Create the directories we nee
        CreateDirectoryIfNeeded(_pathToWorkIn);
        CreateDirectoryIfNeeded(this.UnzipDirectory);
        CreateDirectoryIfNeeded(this.OutputDirectory);

        //2. Unzip file
        ZipFile.ExtractToDirectory(_pathToTwbx, this.UnzipDirectory);

        //3. Look for *.twb file in uncompressed directory (does not need to have maching name)
        string twbFile = GetPathToUnzippedTwb();
        //4. Remap server-path to point to correct server/site (replaces existing file)
        var twbMapper = new TwbDataSourceEditor(twbFile, twbFile, _serverInfo, _statusLog);
        twbMapper.Execute();

        //5. Recreate the TWBX File
        string filenameTwbx = Path.GetFileName(_pathToTwbx);
        string outputPath = Path.Combine(this.OutputDirectory, filenameTwbx);
        ZipFile.CreateFromDirectory(this.UnzipDirectory, outputPath);

        //Return the path to the remapped/re-zipped *.twbx file
        return outputPath;
    }

}


