using System;
using System.Collections.Generic;
using System.Text;

//Values we can use for the options we pass to the downloader/uploader
public partial class TaskMasterOptions
{
    public const int RestApiReponsePageSizeDefault = 1000;

    //If set, we will generate more detailed log information
    public const string Option_LogVerbose = "LogVerbose";

    //If set, the system will ensure that periodic (parallel to normal requests) keep alive authenticated requests get sent to the server
    public const string Option_BackgroundKeepAlive = "BackgroundKeepAliveRequests";

    //On/Off flags (if the flag is present it's On)
    public const string Option_DownloadDatasources = "DownloadDatasources";
    public const string Option_UploadDatasources = "UploadDatasources";
    public const string Option_GetDatasourcesList = "DownloadDatasourcesList";
    public const string Option_GetGroupsList = "DownloadGroupsList";
    public const string Option_GetSchedulesList = "DownloadSchedulesList";
    public const string Option_GetExtractTasksList = "DownloadExtractTasksList";
    public const string Option_DownloadWorkbooks = "DownloadWorkbooks";
    public const string Option_UploadWorkbooks = "UploadWorkbooks";
    public const string Option_GetWorkbooksList = "DownloadWorkbooksList";
    public const string Option_GetWorkbooksConnections = "DownloadWorkbooksConnections"; //Download the data connections for each workbook we have in our workbooks list
    public const string Option_DownloadIntoProjects = "DownloadIntoProjects";  //Put the downloads into file system directories named after the projects
    public const string Option_GetProjectsList = "DownloadProjectsList";
    public const string Option_GetSubscriptionsList = "DownloadSubscriptionsList";
    public const string Option_GetViewsList = "DownloadViewsList";
    public const string Option_UploadCreateNeededProjects = "UploadCreateNeededProjects";
    public const string Option_AssignContentOwnershipAfterPublish = "AssignContentOwnershipAfterPublish";
    public const string Option_GetSiteInfo = "SiteInfo";
    public const string Option_GetSiteUsers = "SiteUsers";
    public const string Option_RemapWorkbookReferencesOnUpload = "UploadRemapWorkbookReferences";
    public const string Option_DBCredentialsPath = "DBCredentialsFile";
    public const string Option_GenerateInventoryTwb = "GenerateInventoryTwb";

    //Take values associated with the parameters
    public const string OptionParameter_PathUploadFrom = "PathUploadFrom";
    public const string OptionParameter_PathDownloadTo = "PathDownloadTo";
    public const string OptionParameter_UploadChunkSizeBytes = "UploadChunkSizeBytes";
    public const string OptionParameter_UploadChunkDelaySeconds = "UploadChunkDelaySeconds";
    public const string OptionParameter_ArbitraryCommand1 = "ArbitraryPostLoginCommand-1";
    public const string OptionParameter_ArbitraryCommand2 = "ArbitraryPostLoginCommand-2";
    public const string OptionParameter_SaveInventoryFile = "SaveInventoryFile";
    public const string OptionParameter_SaveManualSteps = "SaveManualStepsFile";
    public const string OptionParameter_SaveLogFile = "SaveLogFile";
    public const string OptionParameter_SaveErrorsFile = "SaveErrorsFile";
    public const string OptionParameter_CreateProjectWithName = "CreateProjectWithName";
    public const string OptionParameter_JobName = "JobName";
    public const string OptionParameter_ExportSingleProject = "ExportSingleProject";
    public const string OptionParameter_ExportOnlyTaggedWith = "ExportOnlyTaggedWith";
    public const string OptionParameter_RemoveTagFromExportedContent = "RemoveTagFromExportedContent";
    public const string OptionParameter_GenerateInfoFilesForDownloadedContent = "GenerateInfoFilesForDownloadedContent";

}
