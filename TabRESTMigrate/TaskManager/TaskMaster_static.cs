using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

//Holds the static functions
internal partial class TaskMaster
{
    //Names we assoicated with async jobs
    public const string JobName_SiteImport = "SiteImportJob";
    public const string JobName_SiteExport = "SiteExportJob";
    public const string JobName_SiteInventory = "SiteInventoryJob";

    /// <summary>
    /// Creates a TaskMaster from command line arguments
    /// </summary>
    /// <param name="parsedCommandLine"></param>
    /// <returns></returns>
    public static TaskMaster FromCommandLine(CommandLineParser commandLine)
    {
        //----------------------------------------------------------------------
        //Add common options
        //----------------------------------------------------------------------
        var taskOptions = new TaskMasterOptions();
        //Log file
        helper_AddValueIfExists(taskOptions, TaskMasterOptions.OptionParameter_SaveLogFile, commandLine, CommandLineParser.Parameter_LogFile);
        //Error file
        helper_AddValueIfExists(taskOptions, TaskMasterOptions.OptionParameter_SaveErrorsFile, commandLine, CommandLineParser.Parameter_ErrorsFile);
        //Manual steps file
        helper_AddValueIfExists(taskOptions, TaskMasterOptions.OptionParameter_SaveManualSteps, commandLine, CommandLineParser.Parameter_ManualStepsFile);

        //Verbose logging
        if(commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_LogVerbose, false))
        {
            taskOptions.AddOption(TaskMasterOptions.Option_LogVerbose);
        }

        //Background keep alive requests
        if (commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_BackgroundKeepAlive, false))
        {
            taskOptions.AddOption(TaskMasterOptions.Option_BackgroundKeepAlive);
        }

        //----------------------------------------------------------------------------
        //Which command are we dealing with?
        //----------------------------------------------------------------------------
        var commandType = commandLine.GetParameterValue(CommandLineParser.Parameter_Command);

        if(commandType == CommandLineParser.ParameterValue_Command_Inventory)
        {
            return helper_CreateTaskMaster_SiteInventory(
                commandLine.GetParameterValue(CommandLineParser.Parameter_InventoryOutputFile)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromSiteUrl)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromUserId)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromUserPassword)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_FromSiteIsSystemAdmin)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_GenerateInventoryTwb, false)
                ,taskOptions
                );
        }
        else if (commandType == CommandLineParser.ParameterValue_Command_Export)
        {
            return helper_CreateTaskMaster_SiteExport(
                commandLine.GetParameterValue(CommandLineParser.Parameter_ExportDirectory)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromSiteUrl)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromUserId)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_FromUserPassword)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_FromSiteIsSystemAdmin)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_ExportSingleProject)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_ExportOnlyWithTag)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_RemoveTagAfterExport, false)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_GenerateInfoFilesForDownloads, false)
                , taskOptions
                );
        }
        else if (commandType == CommandLineParser.ParameterValue_Command_Import)
        {
            return helper_CreateTaskMaster_SiteImport(
                commandLine.GetParameterValue(CommandLineParser.Parameter_ImportDirectory)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_ToSiteUrl)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_ToUserId)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_ToUserPassword)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_ToSiteIsSystemAdmin)
                ,commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_RemapDataserverReferences)
                ,commandLine.GetParameterValue(CommandLineParser.Parameter_DBCredentialsFile)
                ,taskOptions
                );
        }

        AppDiagnostics.Assert(false, "Unknown command: " + commandType);
        return null;
    }

    /// <summary>
    /// Sets up parameters needed by site inventory
    /// </summary>
    /// <param name="pathToWriteInventoryFile"></param>
    /// <param name="urlToServerSite"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSystemAdmin"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static TaskMaster helper_CreateTaskMaster_SiteInventory(
        string pathToWriteInventoryFile, 
        string urlToServerSite, 
        string userName, 
        string password,
        bool isSystemAdmin,
        bool generateTwbFile,
        TaskMasterOptions options)
    {
        //If we were passed in no existing options, then add them
        if (options == null)
        {
            options = new TaskMasterOptions();
        }

        //Where we want to write the results file to
        options.AddOption(TaskMasterOptions.OptionParameter_SaveInventoryFile, pathToWriteInventoryFile);

        //Things we want to download to
        options.AddOption(TaskMasterOptions.Option_GetProjectsList);
        options.AddOption(TaskMasterOptions.Option_GetDatasourcesList);
        options.AddOption(TaskMasterOptions.Option_GetWorkbooksList);
        options.AddOption(TaskMasterOptions.Option_GetWorkbooksConnections);

        //Some features are only accessible to System Admins
        //NOTE: When the APIs support site-admin getting this information, they will be moved into the more general block above
        if(isSystemAdmin)
        {
            options.AddOption(TaskMasterOptions.Option_GetSiteUsers);
            options.AddOption(TaskMasterOptions.Option_GetSiteInfo);
            options.AddOption(TaskMasterOptions.Option_GetGroupsList);
        }

        //Do we want to create a Tableau Workbook that uses the inventory CSV file?
        if(generateTwbFile)
        {
            options.AddOption(TaskMasterOptions.Option_GenerateInventoryTwb);
        }


        //Generate the URLs mapping class
        var onlineUrls = TableauServerUrls.FromContentUrl(urlToServerSite, TaskMasterOptions.RestApiReponsePageSizeDefault);

        //Create the task master object
        return new TaskMaster(
            JobName_SiteInventory,
            onlineUrls,
            userName,
            password,
            options);
    }


    /// <summary>
    /// Sets up parameters needed by Site Export
    /// </summary>
    /// <param name="dirExportDirectory"></param>
    /// <param name="urlToServerSite"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSystemAdmin"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static TaskMaster helper_CreateTaskMaster_SiteExport(
        string dirExportDirectory,
        string urlToServerSite,
        string userName,
        string password,
        bool isSystemAdmin,
        string exportSingleProject,
        string exportOnlyTaggedWith,
        bool removeTagAfterExport,
        bool generateInfoFilesForDownloadedContent,
        TaskMasterOptions options)
    {
        //If we were passed in no existing options, then add them
        if (options == null)
        {
            options = new TaskMasterOptions();
        }

        //Path to download to
        options.AddOption(TaskMasterOptions.OptionParameter_PathDownloadTo, dirExportDirectory);

        //Things we want to download to
        options.AddOption(TaskMasterOptions.Option_DownloadIntoProjects);
        options.AddOption(TaskMasterOptions.Option_DownloadDatasources);
        options.AddOption(TaskMasterOptions.Option_DownloadWorkbooks);

        //Export only a single project
        if(!string.IsNullOrWhiteSpace(exportSingleProject))
        {
            options.AddOption(TaskMasterOptions.OptionParameter_ExportSingleProject, exportSingleProject);
        }

        //Export only if tagged
        if (!string.IsNullOrWhiteSpace(exportOnlyTaggedWith))
        {
            options.AddOption(TaskMasterOptions.OptionParameter_ExportOnlyTaggedWith, exportOnlyTaggedWith);

            //Does the tag need to get get removed from content we export?
            if (removeTagAfterExport)
            {
                options.AddOption(TaskMasterOptions.OptionParameter_RemoveTagFromExportedContent);
            }
        }

        //Do we want add to save additional metadata with each downloaded workbook/datasource?
        if (generateInfoFilesForDownloadedContent)
        {
            options.AddOption(TaskMasterOptions.OptionParameter_GenerateInfoFilesForDownloadedContent);
        }

        //Some features are only accessible to System Admins
        if (isSystemAdmin)
        {
            options.AddOption(TaskMasterOptions.Option_GetSiteUsers);
            options.AddOption(TaskMasterOptions.Option_GetSiteInfo);
        }

        //Generate the URLs mapping class
        var onlineUrls = TableauServerUrls.FromContentUrl(urlToServerSite, TaskMasterOptions.RestApiReponsePageSizeDefault);

        //Create the task master object
        return new TaskMaster(
            JobName_SiteExport,
            onlineUrls,
            userName,
            password,
            options);

    }

    /// <summary>
    /// Sets up parameters needed by Site Import
    /// </summary>
    /// <param name="dirImportFromDirectory"></param>
    /// <param name="urlToServerSite"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSystemAdmin"></param>
    /// <param name="remapDataserverReferences"></param>
    /// <param name="pathDbCredentials"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static TaskMaster helper_CreateTaskMaster_SiteImport(
        string dirImportFromDirectory,
        string urlToServerSite,
        string userName,
        string password,
        bool isSystemAdmin,
        bool remapDataserverReferences,
        string pathDbCredentials,
        TaskMasterOptions options)
    {
        //If we were passed in no existing options, then add them
        if (options == null)
        {
            options = new TaskMasterOptions();
        }

        //The local file system path we want to upload from
        options.AddOption(TaskMasterOptions.OptionParameter_PathUploadFrom, dirImportFromDirectory);

        //Things we want to upload
        options.AddOption(TaskMasterOptions.Option_UploadDatasources);
        options.AddOption(TaskMasterOptions.Option_UploadWorkbooks);

        //Some features are only accessible to System Admins
        if (isSystemAdmin)
        {
            options.AddOption(TaskMasterOptions.Option_UploadCreateNeededProjects);
        }

        //Do we need to remap workbook references to point to the Server/Site we are uploading to
        if(remapDataserverReferences)
        {
            options.AddOption(TaskMasterOptions.Option_RemapWorkbookReferencesOnUpload);
        }

        if(!string.IsNullOrWhiteSpace(pathDbCredentials))
        {
            options.AddOption(TaskMasterOptions.Option_DBCredentialsPath, pathDbCredentials);
        }

        //Generate the URLs mapping class
        var onlineUrls = TableauServerUrls.FromContentUrl(urlToServerSite, TaskMasterOptions.RestApiReponsePageSizeDefault);

        //Create the task master object
        return new TaskMaster(
            JobName_SiteImport,
            onlineUrls,
            userName,
            password,
            options);
    }


    /// <summary>
    /// Helper: Adds a command line parameter (if it exists) to the Options colleciton
    /// </summary>
    /// <param name="options"></param>
    /// <param name="optionKey"></param>
    /// <param name="commandLine"></param>
    /// <param name="commandLineKey"></param>
    private static void helper_AddValueIfExists(TaskMasterOptions options, string optionKey, CommandLineParser commandLine, string commandLineKey)
    {
        var commandlineValue = commandLine.GetParameterValue(commandLineKey);
        if (!string.IsNullOrWhiteSpace(commandlineValue))
        {
            options.AddOption(optionKey, commandlineValue);
        }
    }

    /// <summary>
    /// Returns TRUE if the specified directory looks to be a valid directory we can import site content from
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool IsValidImportFromDirectory(string path)
    {

        //If the directory does not exist, then no import possible
        if(!Directory.Exists(path))
        {
            return false;
        }

        //If it's got at least a workbooks directory, then assume its valid
        if(Directory.Exists(Path.Combine(path, "workbooks")))
        {
            return true;
        }

        //If it's got at least a datasources directory, then assume its valid
        if (Directory.Exists(Path.Combine(path, "datasources")))
        {
            return true;
        }

        return false; //We did not find any subdirectories we expected
    }
}
