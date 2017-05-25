using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

internal partial class TaskMaster
{
    Thread _thread = null;

    private readonly string _exportToLocalPath;
    private readonly string _userName;
    private readonly string _password;
    private readonly TableauServerUrls _onlineUrls;
    private readonly TaskStatusLogs _statusLog = null;
    private readonly TaskMasterOptions _taskOptions;
    private readonly CustomerManualActionManager _manualActions; //Tracks the list of manual actions the customer will need to perform after the import/export
    private string _pathGeneratedSiteInventoryReportCsv; //If non-null, it indicates we have generated a site inventory report
    private string _pathGeneratedSiteInventoryReportTwb; //If non-null, it indicates we have generated a site inventory report
    private string _pathGeneratedManualStepsReport; //If non-null, it indicates we have generated a report

    /// <summary>
    /// If there was a task to produce a site inventory report, this property will contain the path to that report
    /// </summary>
    public string PathToSiteInventoryReportCsv
    {
        get
        {
            return _pathGeneratedSiteInventoryReportCsv;
        }
    }

    /// <summary>
    /// If there was a task to produce a site inventory report, this property will contain the path to that report
    /// </summary>
    public string PathToSiteInventoryReportTwb
    {
        get
        {
            return _pathGeneratedSiteInventoryReportTwb;
        }
    }


    /// <summary>
    /// If there were manual steps recorded in a CSV file, this path will point to that file
    /// </summary>
    public string PathToManualStepsReport
    {
        get
        {
            return _pathGeneratedManualStepsReport;
        }
    }

    /// <summary>
    /// If we took an  inventory of the list of projects this will contain the list
    /// </summary>
    public IEnumerable<SiteProject> ProjectsList
    {
        get
        {
            return _downloadedList_Projects;
        }
    }
    private IEnumerable<SiteProject> _downloadedList_Projects;


    /// <summary>
    /// If we took an  inventory of the list of subscriptions this will contain the list
    /// </summary>
    public IEnumerable<SiteSubscription> SubscriptionsList
    {
        get
        {
            return _downloadedList_Subscriptions;
        }
    }
    private IEnumerable<SiteSubscription> _downloadedList_Subscriptions;

    /// <summary>
    /// If we took an  inventory of the list of subscriptions this will contain the list
    /// </summary>
    public IEnumerable<SiteView> ViewsList
    {
        get
        {
            return _downloadedList_Views;
        }
    }
    private IEnumerable<SiteView> _downloadedList_Views;


    /// <summary>
    /// If we took an inventory of groups, return them
    /// </summary>
    public IEnumerable<SiteGroup> GroupsList
    {
        get
        {
            return _downloadedList_Groups;
        }
    }
    private IEnumerable<SiteGroup> _downloadedList_Groups;

    /// <summary>
    /// If we downloaded the list of data sources, it will be here
    /// </summary>
    public IEnumerable<SiteDatasource> DatasourcesList
    {
        get
        {
            return _downloadedList_Datasources;
        }
    }
    private IEnumerable<SiteDatasource> _downloadedList_Datasources;


    /// <summary>
    /// If we downloaded the list of schedules, it will be here
    /// </summary>
    public IEnumerable<SiteSchedule> SchedulesList
    {
        get
        {
            return _downloadedList_Schedules;
        }
    }
    private IEnumerable<SiteSchedule> _downloadedList_Schedules;


    /// <summary>
    /// If we downloaded the set of extract refresh tasks, it will be here
    /// </summary>
    public IEnumerable<SiteTaskExtractRefresh> TasksExtractRefreshesList
    {
        get
        {
            return _downloadedList_ExtractRefreshTasks;
        }
    }
    private IEnumerable<SiteTaskExtractRefresh> _downloadedList_ExtractRefreshTasks;

    /// <summary>
    /// If we downloaded the list of workbooks, it will be here
    /// </summary>
    public IEnumerable<SiteWorkbook> WorkbooksList
    {
        get
        {
            return _downloadedList_Workbooks;
        }
    }
    private IEnumerable<SiteWorkbook> _downloadedList_Workbooks;



    /// <summary>
    /// If we downloaded the list of users, it will be here
    /// </summary>
    public IEnumerable<SiteUser> UsersList
    {
        get
        {
            return _downloadedList_Users;
        }
    }
    private IEnumerable<SiteUser> _downloadedList_Users;

    public CustomerManualActionManager CustomerManualActionManager 
    {
        get
        {
            return _manualActions;
        }
    }

    /// <summary>
    /// Where are we are exporting to
    /// </summary>
    public string PathToExportTo
    {
        get { return _exportToLocalPath; }
    }

    bool _isDone = false;
    KeepAliveBackgroundTaskBase _asyncKeepAliveBackgroundTasks = null;

    public readonly string JobName;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jobName">Name to associate with this work</param>
    /// <param name="onlineUrls"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="taskOptions"></param>
    /// <param name="manualActions"></param>
    public TaskMaster(
        string jobName,
        TableauServerUrls onlineUrls, 
        string userName, 
        string password,
        TaskMasterOptions taskOptions,
        CustomerManualActionManager manualActions = null)
    {
        this.JobName = jobName;

        _manualActions = manualActions;
        if(_manualActions == null)
        {
            _manualActions = new CustomerManualActionManager();
        }
        //Get any export path
        _exportToLocalPath = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_PathDownloadTo);
        _onlineUrls = onlineUrls;
        _userName = userName;
        _password = password;

        //Store the status log at the class level where it is accessable
        _statusLog = new TaskStatusLogs();
        //Store the options
        _taskOptions = taskOptions;

    
        if(_taskOptions.IsOptionSet(TaskMasterOptions.Option_LogVerbose))
        {
            _statusLog.SetStatusLoggingLevel(int.MinValue);
        }
    }


    /// <summary>
    /// Called to force an abort
    /// </summary>
    /// <param name="markAsDone"></param>
    public void Abort(bool markAsDone = false)
    {

        //Do we want to mark ourselves as done
        if (markAsDone)
        {
            _isDone = true;
        }

        
        //=========================================================
        //If there is an async keep alive task, tell it to stop
        //=========================================================
        var asyncKeepAliveTask = _asyncKeepAliveBackgroundTasks;
        if(asyncKeepAliveTask != null)
        {
            asyncKeepAliveTask.ExitAsync();
        }

        //=========================================================
        //Deal with the thread this class manages
        //=========================================================
        var thread = _thread;
        if (thread == null) return;
        thread.Abort();
    }
    public bool IsDone
    {
        get
        {
            return _isDone;
        }
    }

    /// <summary>
    /// Live status log
    /// </summary>
    public TaskStatusLogs StatusLog
    {
        get
        {
            return _statusLog;
        }
    }
    /// <summary>
    /// Starts the task running
    /// </summary>
    public void ExecuteTaskBegin()
    {
        if (_thread != null) return;

        _isDone = false;
        var thread = new Thread(new ThreadStart(ExecuteTask_Internal));
        thread.Start();
        _thread = thread;
    }

    /// <summary>
    /// Called to attempt to execute a custom command (to be run after the user logs in)
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="customCommand"></param>
    private void AttemptExecutionOfCustomHttpGet(TableauServerSignIn onlineLogin, string customCommand)
    {
        _statusLog.AddStatusHeader("GET request: " + customCommand);
        var customGetRequest = new SendPostLogInCommand(_onlineUrls, onlineLogin, customCommand);
        try
        {
            var customResult = customGetRequest.ExecuteRequest();
            _statusLog.AddStatus("GET result: " + customResult);
        }
        catch (Exception exCustomCommand)
        {
            _statusLog.AddError("Error during custom GET, " + exCustomCommand.ToString());

        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="onlineLogin"></param>
    private SiteinfoSite Execute_DownloadSiteInfo(TableauServerSignIn onlineLogin)
    {
        SiteinfoSite site = null;

        _statusLog.AddStatusHeader("Request site info");
        try
        {
            var getSiteInfo = new DownloadSiteInfo(_onlineUrls, onlineLogin);
            getSiteInfo.ExecuteRequest();
            site = getSiteInfo.Site;
        }
        catch (Exception exSite)
        {
            _statusLog.AddError("Error getting site info, " + exSite.ToString());
        }

        return site;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="exportToPath"></param>
    /// <param name="projectsList"></param>
    /// <param name="singleProjectIdFilter"></param>
    /// <param name="exportOnlyWithThisTag"></param>
    /// <param name="deleteTagAfterExport"></param>
    /// <param name="generateInfoFile">TRUE: Each downloaded workbook will get an information file generated that has additional metadata about it</param>
    /// <param name="siteUsers"> If not NULL, then this will get used to look the user name for each downloaded workbook, and safe it into the info file</param>
    private void Execute_DownloadDatasources(
        TableauServerSignIn onlineLogin, 
        string exportToPath, 
        IProjectsList projectsList,
        SiteProject singleProjectIdFilter = null,
        string exportOnlyWithThisTag      = null,
        bool deleteTagAfterExport         = false,
        bool generateInfoFile             = false,
        IEnumerable<SiteUser>   siteUsers = null
        )
    {
        _statusLog.AddStatusHeader("Download datasources");
        ICollection<SiteDatasource> datasourcesList = null;
        try
        {
            //Get the list of datasources
            var datasourcesManager = new DownloadDatasourcesList(_onlineUrls, onlineLogin);
            datasourcesManager.ExecuteRequest();
            datasourcesList = datasourcesManager.Datasources;
        }
        catch(Exception exGetContentList)
        {
            _statusLog.AddError("Error querying for list of datasources, " + exGetContentList.Message.ToString());
        }

        if(datasourcesList == null)
        {
            _statusLog.AddError("Aborting datasources download");
            return;
        }

        //====================================================================================================
        //Apply filters to the list of content to see if we need to reduce the set of content to be downloaded
        //====================================================================================================
        var filteredList = datasourcesList;
        _statusLog.AddStatus("Download datasources count before filters: " + filteredList.Count.ToString());

        //See if we have a PROJECTS filter to apply to the set of content to download
        filteredList = FilterProjectMembership<SiteDatasource>.KeepOnlyProjectMembers(
            filteredList,
            singleProjectIdFilter,
            true);
        _statusLog.AddStatus("Download datasources count after projects filter: " + filteredList.Count.ToString());

        //See if we have a TAGS filter to apply to the set of content to be downloaded
        filteredList = FilterTagSet<SiteDatasource>.KeepOnlyTagged(
            filteredList,
            exportOnlyWithThisTag,
            true);
        _statusLog.AddStatus("Download datasources count after tags filter: " + filteredList.Count.ToString());

        ICollection<SiteDatasource> successfullExportSet = null;
        var datasourcePath = Path.Combine(exportToPath, "datasources");
        FileIOHelper.CreatePathIfNeeded(datasourcePath);

        //-----------------------------------------------------------
        //Download the data sources
        //-----------------------------------------------------------
        //If we are going to write out metadata for each download, then create the object that lets us look up the owner of each workbook
        KeyedLookup<SiteUser> contentOwnerLookup = null;
        if ((generateInfoFile) && (siteUsers != null))
        {
            contentOwnerLookup = new KeyedLookup<SiteUser>(siteUsers, _statusLog);
        }
        try
        {
            var datasourceDownloads = new DownloadDatasources(
                _onlineUrls, 
                onlineLogin,
                filteredList, 
                datasourcePath,
                projectsList,
                generateInfoFile,
                contentOwnerLookup);
            successfullExportSet = datasourceDownloads.ExecuteRequest();
        }
        catch (Exception exDatasourceDownload)
        {
            _statusLog.AddError("Error during datasource download, " + exDatasourceDownload.ToString());
        }

        //--------------------------------------------------------------------------------
        //Do we want to remove tags from successfully downloaded content?
        //--------------------------------------------------------------------------------
        if ((successfullExportSet != null) && (deleteTagAfterExport) && (!string.IsNullOrWhiteSpace(exportOnlyWithThisTag)))
        {
            Execute_DeleteTagFromDatasources(onlineLogin, successfullExportSet, exportOnlyWithThisTag);
        }

    }


    /// <summary>
    /// Download the data sources list
    /// </summary>
    /// <param name="onlineLogin"></param>
    private void Execute_DownloadDatasourcesList(TableauServerSignIn onlineLogin)
    {
        _statusLog.AddStatusHeader("Download datasources list");
        try
        {
            //Get the list of datasources
            var datasources = new DownloadDatasourcesList(_onlineUrls, onlineLogin);
            datasources.ExecuteRequest();

            //Store them in our object
            _downloadedList_Datasources = datasources.Datasources; 
        }
        catch (Exception exDatasourceDownload)
        {
            _statusLog.AddError("Error during datasource list download, " + exDatasourceDownload.ToString());
        }
    }

    /// <summary>
    /// Download the schedules list
    /// </summary>
    /// <param name="onlineLogin"></param>
    private IEnumerable<SiteSchedule> Execute_DownloadSchedulesList(TableauServerSignIn onlineLogin)
    {
        _statusLog.AddStatusHeader("Download schedules list");
        try
        {
            //Get the list of schedules
            var schedules = new DownloadSchedulesList(_onlineUrls, onlineLogin);
            schedules.ExecuteRequest();

            //Store them in our object
            var siteSchedules = schedules.Schedules;
            _downloadedList_Schedules = siteSchedules;
            return siteSchedules;
        }
        catch (Exception exDownload)
        {
            _statusLog.AddError("Error during schedules list download, " + exDownload.ToString());
            return null;
        }
    }


    /// <summary>
    /// Download the list of tasks for the specified schedules
    /// </summary>
    /// <param name="serverLogin"></param>
    /// <param name="enumerable"></param>
    private void Execute_DownloadExtractTasksList(
        TableauServerSignIn serverLogin, 
        IEnumerable<SiteSchedule> schedules)
    {
        _statusLog.AddStatusHeader("Download extract refresh tasks list");

        //Missing schedules set? Something is wrong...
        if (schedules == null)
        {
            this.StatusLog.AddError("NULL set of schedules");
            return;
        }

        var extractRefreshTasks = new List<SiteTaskExtractRefresh>();

        //For each schedule, download its list of extract refresh tasks
        foreach(var thisSchedule in schedules)
        {
            try
            {
                var tasksDownloader = new DownloadTasksExtractRefreshesList(_onlineUrls, serverLogin, thisSchedule.Id);
                tasksDownloader.ExecuteRequest();

                //Add the tasks to the set
                extractRefreshTasks.AddRange(tasksDownloader.Tasks);
            }
            catch(Exception exDownload)
            {
                _statusLog.AddError("Error during extract refresh tasks download, " + exDownload.ToString());
            }
        }

        //Store the set of extract refresh tasks
        _downloadedList_ExtractRefreshTasks = extractRefreshTasks;

    }

    /// <summary>
    /// Download the data sources
    /// </summary>
    /// <param name="onlineLogin"></param>
    private void Execute_DownloadWorkbooksList(TableauServerSignIn onlineLogin)
    {
        _statusLog.AddStatusHeader("Download workbooks list");
        try
        {
            //Get the list of workbooks
            var workbooks = new DownloadWorkbooksList(_onlineUrls, onlineLogin);
            workbooks.ExecuteRequest();

            //Store them in our object
            _downloadedList_Workbooks = workbooks.Workbooks;
        }
        catch (Exception exDownload)
        {
            _statusLog.AddError("Error during workbooks list download, " + exDownload.ToString());
        }
    }


    /// <summary>
    /// Downloads the connections list for each workbook specified
    /// </summary>
    /// <param name="serverLogin"></param>
    /// <param name="serverContent"></param>
    private void Execute_DownloadWorkbooksConnections(
        TableauServerSignIn serverLogin, 
        IEnumerable<SiteWorkbook> serverContent)
    {
        _statusLog.AddStatusHeader("Download each workbook's connections");
        if(serverContent == null)
        {
            _statusLog.AddError("Null workbooks list");
            return;
        }

        //For each content item attempt to download it's list of connections
        foreach(var contentItem in serverContent)
        {
            try
            {
                //Request the list of data connections embedded in the content
                var wbDownloadConnections = new DownloadWorkbookConnections(
                    _onlineUrls,
                    serverLogin,
                    contentItem.Id);
                wbDownloadConnections.ExecuteRequest();

                //Put it into the content object
                //Note: We are using a dedicated interface expose the Edit/Set method because most people using the 
                //      object only want to read the data
                ((IEditDataConnectionsSet) contentItem).SetDataConnections(wbDownloadConnections.Connections);
            }
            catch(Exception ex)
            {
                _statusLog.AddError("Error attepting to get data connection information for " + contentItem.Id + "/" + contentItem.Name + ", " + ex.Message);
            }
        }//end: foreach
    }



    /// <summary>
    /// Downloads the connections list for each datasource specified
    /// </summary>
    /// <param name="serverLogin"></param>
    /// <param name="serverContent"></param>
    private void Execute_DownloadDatasourceConnections(
        TableauServerSignIn serverLogin,
        IEnumerable<SiteDatasource> serverContent)
    {
        _statusLog.AddStatusHeader("Download each datasource's connections");
        if (serverContent == null)
        {
            _statusLog.AddError("Null datasources list");
            return;
        }

        //For each content item attempt to download it's list of connections
        foreach (var contentItem in serverContent)
        {
            try
            {
                //Request the list of data connections embedded in the content
                var dsDownloadConnections = new DownloadDatasourceConnections(
                    _onlineUrls,
                    serverLogin,
                    contentItem.Id);
                dsDownloadConnections.ExecuteRequest();

                //Put it into the content object
                //Note: We are using a dedicated interface expose the Edit/Set method because most people using the 
                //      object only want to read the data
                ((IEditDataConnectionsSet)contentItem).SetDataConnections(dsDownloadConnections.Connections);
            }
            catch (Exception ex)
            {
                _statusLog.AddError("Error attepting to get data connection information for " + contentItem.Id + "/" + contentItem.Name + ", " + ex.Message);
            }
        }//end: foreach
    }


    /// <summary>
    /// Download the workbooks
    /// </summary>
    /// <param name="onlineLogin">logged in session</param>
    /// <param name="exportToPath">local path to export to</param>
    /// <param name="projectsList">project id/name mapping</param>
    /// <param name="singleProjectIdFilter">if specified, export only from a single project</param>
    /// <param name="exportOnlyWithThisTag">if specified, export only content with this tag</param>
    /// <param name="deleteTagAfterExport">TRUE: Remove the server-side tag from exported content (only valid if we have an export tag)</param>
    /// <param name="generateInfoFile">TRUE: Each downloaded workbook will get an information file generated that has additional metadata about it</param>
    /// <param name="siteUsers"> If not NULL, then this will get used to look the user name for each downloaded workbook, and safe it into the info file</param>
    private void Execute_DownloadWorkbooks(
        TableauServerSignIn onlineLogin, 
        string exportToPath, 
        IProjectsList projectsList,
        SiteProject singleProjectIdFilter = null,
        string exportOnlyWithThisTag      = null,
        bool deleteTagAfterExport         = false,
        bool generateInfoFile             = false,
        IEnumerable<SiteUser> siteUsers   = null
        )
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Download workbooks");

        //Get the UserID we need to use for the workbooks query.
        var explicitUserId = onlineLogin.UserId; //See if we have a default user id

        //===================================================================================
        //Workbooks...
        //===================================================================================
        ICollection<SiteWorkbook> workbooksList = null;
        try
        {
            var workbooks = new DownloadWorkbooksList(onlineUrls, onlineLogin, explicitUserId);
            //Query for the list of workbook
            workbooks.ExecuteRequest();
            workbooksList = workbooks.Workbooks;

        }
        catch (Exception exWorkbooksList)
        {
            _statusLog.AddError("Error querying for list of workbooks, " + exWorkbooksList.Message.ToString());
            return;
        }

        //No list of workbooks?  Exit.
        if(workbooksList == null)
        {
            _statusLog.AddStatus("Aborting workbooks download");
            return;
        }
        
        //====================================================================================================
        //Apply filters to the list of content to see if we need to reduce the set of content to be downloaded
        //====================================================================================================
        var filteredList = workbooksList;
        _statusLog.AddStatus("Download workbooks count before filters: " + filteredList.Count.ToString());

        //See if we have a PROJECTS filter to apply to the set of content to be downloaded
        filteredList = FilterProjectMembership<SiteWorkbook>.KeepOnlyProjectMembers(
                            filteredList,
                            singleProjectIdFilter, 
                            true);
        _statusLog.AddStatus("Download workbooks count after projects filter: " + filteredList.Count.ToString());

        //See if we have a TAGS filter to apply to the set of content to be downloaded
        filteredList = FilterTagSet<SiteWorkbook>.KeepOnlyTagged(
            filteredList,
            exportOnlyWithThisTag,
            true);
        _statusLog.AddStatus("Download workbooks count after tags filter: " + filteredList.Count.ToString());

        //-----------------------------------------------------------
        //Download the workbooks
        //-----------------------------------------------------------
        var workbookPath = Path.Combine(exportToPath, "workbooks");
        ICollection<SiteWorkbook> successfullExportSet = null;
        FileIOHelper.CreatePathIfNeeded(workbookPath);

        //If we are going to write out metadata for each download, then create the object that lets us look up the owner of each workbook
        KeyedLookup<SiteUser> workbookOwnerLookup = null;
        if ((generateInfoFile) && (siteUsers != null))
        {
            workbookOwnerLookup = new KeyedLookup<SiteUser>(siteUsers, _statusLog);
        }
        //Do the downloads......
        try
        {
            //Create the workbooks downloader
            var workbookDownloads = new DownloadWorkbooks(
                onlineUrls, 
                onlineLogin, 
                filteredList, 
                workbookPath, 
                projectsList, 
                generateInfoFile,
                workbookOwnerLookup);
            successfullExportSet = workbookDownloads.ExecuteRequest();
        }
        catch (Exception exWorkbooksDownload)
        {
            _statusLog.AddError("Error during workbooks download, " + exWorkbooksDownload.ToString());
        }

        //--------------------------------------------------------------------------------
        //Do we want to remove tags from successfully downloaded content?
        //--------------------------------------------------------------------------------
        if ((successfullExportSet != null) && (deleteTagAfterExport) && (!string.IsNullOrWhiteSpace(exportOnlyWithThisTag)))
        {
            Execute_DeleteTagFromWorkbooks(onlineLogin, successfullExportSet, exportOnlyWithThisTag);
        }
    }

    /// <summary>
    /// Attempt to remove a tag from a set of workbooks
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="contentSet"></param>
    /// <param name="removeTag"></param>
    private void Execute_DeleteTagFromWorkbooks(
        TableauServerSignIn onlineLogin, 
        ICollection<SiteWorkbook> contentSet, 
        string removeTag)
    {
        if(string.IsNullOrWhiteSpace(removeTag))
        {
            throw new ArgumentException("Tag to remove is blank");
        }

        //Nothing to do?
        if(contentSet == null)
        {
            return;
        }

        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Deleting tag '" + removeTag +  "' from " + contentSet.Count.ToString() + " workbooks");
        //Make the delete tag request for each workbook
        foreach(var contentItem in contentSet)
        { 
            try
                {
                    var tagDelete = new SendDeleteWorkbookTag(onlineUrls, onlineLogin, contentItem.Id, removeTag);
                    tagDelete.ExecuteRequest();
                }
            catch(Exception exDeleteTag)
                {
                    _statusLog.AddError(
                        "Error attempting to delete tag from workbook " + 
                        contentItem.Id + "/" + removeTag 
                        + ", " + exDeleteTag.Message);
                }
        }//end: foreach
    }


    /// <summary>
    /// Attempt to remove a tag from a set of datasources
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="contentSet"></param>
    /// <param name="removeTag"></param>
    private void Execute_DeleteTagFromDatasources(
        TableauServerSignIn onlineLogin,
        ICollection<SiteDatasource> contentSet,
        string removeTag)
    {
        if (string.IsNullOrWhiteSpace(removeTag))
        {
            throw new ArgumentException("Tag to remove is blank");
        }

        //Nothing to do?
        if (contentSet == null)
        {
            return;
        }

        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Deleting tag '" + removeTag + "' from " + contentSet.Count.ToString() + " datasources");
        //Make the delete tag request for each datasource
        foreach (var contentItem in contentSet)
        {
            try
            {
                var tagDelete = new SendDeleteDatasourceTag(onlineUrls, onlineLogin, contentItem.Id, removeTag);
                tagDelete.ExecuteRequest();
            }
            catch (Exception exDeleteTag)
            {
                _statusLog.AddError(
                    "Error attempting to delete tag from datasource " +
                    contentItem.Id + "/" + removeTag
                    + ", " + exDeleteTag.Message);
            }
        }//end: foreach
    }

    /// <summary>
    /// Called when the background thread is run
    /// </summary>
    private void ExecuteTask_Internal()
    {
        //Since we are running on a background thread, we ALWAYS want ot get to the point where we mark the work as compelted,
        //even if an unexpected error occured
        try
        {
            ExecuteTask_InternalAllTasks();
        }
        catch(Exception exUnexpected)
        {
            if(_statusLog != null)
            {
                _statusLog.AddError("Error executing tasks: " + exUnexpected.ToString());
            }
        }
        finally
        {
            //Mark the work as done
            _isDone = true;
        }
    }

    /// <summary>
    /// Perform the work
    /// </summary>
    private void ExecuteTask_InternalAllTasks()
    {
        string exportToPath = _exportToLocalPath; 
        var onlineUrls = _onlineUrls;
        var taskOptions = _taskOptions;

        //========================================================================================
        //Log into Tableau Online
        //========================================================================================
        var serverLogin = new TableauServerSignIn(onlineUrls, _userName, _password, _statusLog);
        try
        { 
            serverLogin.ExecuteRequest();
        }
        catch(Exception exLogin)
        {
            _statusLog.AddError("Failed loging, " + exLogin.ToString());

            _isDone = true;
            return;
        }

        //========================================================================================
        //If we need periodic background requests, set them going on a background thread
        //========================================================================================
        if(taskOptions.IsOptionSet(TaskMasterOptions.Option_BackgroundKeepAlive))
        {
            _statusLog.AddStatusHeader("Setting up async background keep-alive requests");
            _asyncKeepAliveBackgroundTasks = new KeepAliveBackgroundWebRequest(onlineUrls, serverLogin);
            _asyncKeepAliveBackgroundTasks.Execute();
        }


        //========================================================================================
        //If there is a custom command we want to execute, do it
        //========================================================================================
        var customCommand1 = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_ArbitraryCommand1);
        if (!string.IsNullOrWhiteSpace(customCommand1))
        {
            AttemptExecutionOfCustomHttpGet(serverLogin, customCommand1);
        }
        var customCommand2 = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_ArbitraryCommand2);
        if (!string.IsNullOrWhiteSpace(customCommand2))
        {
            AttemptExecutionOfCustomHttpGet(serverLogin, customCommand2);
        }

        //========================================================================================
        //If there is a project we want to create, then do it
        //========================================================================================
        var createProjectName = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_CreateProjectWithName);
        if(!string.IsNullOrWhiteSpace(createProjectName))
        {
            Execute_CreateProjectWithName(serverLogin, createProjectName);
        }

        //===================================================================================
        //Views
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetViewsList))
        {
            var viewsList = Execute_DownloadViewsList(serverLogin);
        }

        //========================================================================================
        //Attempt to get the list of users
        //========================================================================================
        if ((taskOptions.IsOptionSet(TaskMasterOptions.Option_GetSiteUsers)) 
            || (taskOptions.IsOptionSet(TaskMasterOptions.Option_AssignContentOwnershipAfterPublish))) //If we do content remapping, we need the users list
        {
            _downloadedList_Users = Execute_DownloadSiteUsers(serverLogin);
        }

        //========================================================================================
        //Attempt to get the site information
        //========================================================================================
        SiteinfoSite site = null;
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetSiteInfo))
        {
            site = Execute_DownloadSiteInfo(serverLogin);
        }

        //If this gets set then we will pass the information for project/name mapping to the workbook and datasource downloaders
        IProjectsList projectNameIdMapping = null;
        SiteProject exportSingleProject = null;  //If non NULL, then look up the project

        //===================================================================================
        //Projects List
        //===================================================================================
        if ((taskOptions.IsOptionSet(TaskMasterOptions.Option_GetProjectsList)) || 
            (taskOptions.IsOptionSet(TaskMasterOptions.Option_DownloadIntoProjects)) ||
            (taskOptions.IsOptionSet(TaskMasterOptions.OptionParameter_ExportSingleProject))
            ) 
        {
            var projectsList = Execute_DownloadProjectsList(serverLogin);

            if(taskOptions.IsOptionSet(TaskMasterOptions.Option_DownloadIntoProjects))
            {
                projectNameIdMapping = projectsList;
            }

            //Determine if the there is a project filter
            exportSingleProject = helper_DetermineIfSingleProjectFilter(projectsList);
        }

        //===================================================================================
        //Subscriptions
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetSubscriptionsList))
        {
            var subscriptionsList = Execute_DownloadSubscriptionsList(serverLogin);
        }

        //===================================================================================
        //List of groups? 
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetGroupsList))
        {
            Execute_DownloadGroupsList(serverLogin);
        }

        //===================================================================================
        //List of datasources? (does not download actual data sources, just the list)
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetDatasourcesList))
        {
            Execute_DownloadDatasourcesList(serverLogin);
        }

        //===================================================================================
        //List of schedules? 
        //===================================================================================
        if ((taskOptions.IsOptionSet(TaskMasterOptions.Option_GetSchedulesList))
            || (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetExtractTasksList)))
        {
            var schedules  = Execute_DownloadSchedulesList(serverLogin);

            //See if we should download the list of extract refresh tasks
            if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetExtractTasksList))
            {
                Execute_DownloadExtractTasksList(
                    serverLogin, 
                    SiteSchedule.FilterListToExtractSchedules(schedules));
            }
        }

        //===================================================================================
        //Datasources download...
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_DownloadDatasources))
        {
            Execute_DownloadDatasources(
                serverLogin 
                ,exportToPath 
                ,projectNameIdMapping 
                ,exportSingleProject
                ,taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_ExportOnlyTaggedWith)
                ,taskOptions.IsOptionSet(TaskMasterOptions.OptionParameter_RemoveTagFromExportedContent)
                ,taskOptions.IsOptionSet(TaskMasterOptions.OptionParameter_GenerateInfoFilesForDownloadedContent)
                ,_downloadedList_Users
                ); 
        }


        //===================================================================================
        //Workbooks download...
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_DownloadWorkbooks))
        {
            Execute_DownloadWorkbooks(
                 serverLogin 
                ,exportToPath 
                ,projectNameIdMapping 
                ,exportSingleProject
                ,taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_ExportOnlyTaggedWith)
                ,taskOptions.IsOptionSet(TaskMasterOptions.OptionParameter_RemoveTagFromExportedContent)
                ,taskOptions.IsOptionSet(TaskMasterOptions.OptionParameter_GenerateInfoFilesForDownloadedContent)
                ,_downloadedList_Users
                );
        }

        //===================================================================================
        //List of workbooks? (does not download actual workbooks, just the list)
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_GetWorkbooksList))
        {
            Execute_DownloadWorkbooksList(serverLogin);

            //Do we want to download the connection information for each workbook?
            if(taskOptions.IsOptionSet(TaskMasterOptions.Option_GetWorkbooksConnections))
            {
                Execute_DownloadWorkbooksConnections(
                    serverLogin
                    , this.WorkbooksList);
            }

        }

        //===================================================================================
        //Are there database credentials we need to associate with content being uploaded...
        //===================================================================================
        var pathDBCredentials = taskOptions.GetOptionValue(TaskMasterOptions.Option_DBCredentialsPath);
        CredentialManager uploadCredentialManager = null;
        if (!string.IsNullOrWhiteSpace(pathDBCredentials))
        {
            uploadCredentialManager = Execute_LoadDBCredentialsFile(pathDBCredentials);
        }

        //===================================================================================
        //Datasources upload...
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_UploadDatasources))
        {
            Execute_UploadDatasources(
                serverLogin, 
                taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_PathUploadFrom),
                uploadCredentialManager,
                taskOptions.IsOptionSet(TaskMasterOptions.Option_AssignContentOwnershipAfterPublish),
                _downloadedList_Users);
        }

        //===================================================================================
        //Workbooks upload...
        //===================================================================================
        if (taskOptions.IsOptionSet(TaskMasterOptions.Option_UploadWorkbooks))
        {            
            Execute_UploadWorkbooks(
                serverLogin,
                taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_PathUploadFrom), 
                taskOptions.IsOptionSet(TaskMasterOptions.Option_RemapWorkbookReferencesOnUpload),
                uploadCredentialManager,
                taskOptions.IsOptionSet(TaskMasterOptions.Option_AssignContentOwnershipAfterPublish),
                _downloadedList_Users);
        }

        //===================================================================================
        //Save the site inventory file
        //===================================================================================
        var inventoryFile = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_SaveInventoryFile);
        if(!string.IsNullOrWhiteSpace(inventoryFile))
        {
            Execute_GenerateSiteInventoryFile(inventoryFile);

            //Do we want to generate a TWB file that uses the inventory file
            if(taskOptions.IsOptionSet(TaskMasterOptions.Option_GenerateInventoryTwb))
            {
                Execute_GenerateSiteInventoryFile_Twb(inventoryFile);
            }
        }

        //===================================================================================
        //Save the manual steps file
        //===================================================================================
        var manualStepsFile = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_SaveManualSteps);
        if (!string.IsNullOrWhiteSpace(manualStepsFile))
        {
            Execute_GenerateManualStepsFile(manualStepsFile);
        }

        //===================================================================================
        //Save the logs file
        //===================================================================================
        var logFile = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_SaveLogFile);
        if(!string.IsNullOrWhiteSpace(logFile))
        {
            Execute_SaveLogFile(logFile);
        }

        //===================================================================================
        //Save the errors file
        //===================================================================================
        var errorsFile = taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_SaveErrorsFile);
        if (!string.IsNullOrWhiteSpace(errorsFile))
        {
            Execute_SaveErrorsFile(errorsFile);
        }

        
        //===================================================================================
        //Sign out and clean up
        //===================================================================================
        if(_asyncKeepAliveBackgroundTasks != null) //If we had background async keep alive tasks, tell them to stop
        {
            _asyncKeepAliveBackgroundTasks.ExitAsync();
            _asyncKeepAliveBackgroundTasks = null;
        }
        //Sign out of Tableau Server
        _statusLog.AddStatusHeader("Sign out");
        serverLogin.SignOut(onlineUrls);
    }



    /// <summary>
    /// Attempts to load a DB credential file
    /// </summary>
    /// <param name="pathDBCredentials"></param>
    /// <returns></returns>
    private CredentialManager Execute_LoadDBCredentialsFile(string pathDBCredentials)
    {
        try
        {
            return CredentialManager.LoadFromFile(pathDBCredentials, this.StatusLog);
        }
        catch (Exception ex)
        {
            this.StatusLog.AddError("Error loading DB credentials file '" + System.Convert.ToString(pathDBCredentials) + "', " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// If the user has specified a single project filter, then get it from the projects list
    /// </summary>
    /// <param name="projectsList"></param>
    /// <returns></returns>
    private SiteProject helper_DetermineIfSingleProjectFilter(IProjectsList projectsList)
    {
        string exportSingleProject_name = _taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_ExportSingleProject);
        //If there's no exclusive project name in the config, then the fitler is null
        if (string.IsNullOrWhiteSpace(exportSingleProject_name))
        {
            return null;
        }

        var project = projectsList.FindProjectWithName(exportSingleProject_name);
        if (project == null)
        {
            throw new Exception("Site contains no project with name " + exportSingleProject_name);
        }
        return project;
    }


    /// <summary>
    /// Save the log file
    /// </summary>
    /// <param name="logFile"></param>
    private void Execute_SaveLogFile(string logFile)
    {
        try
        {
            System.IO.File.AppendAllText(logFile, this.StatusLog.StatusText);
        }
        catch(Exception ex)
        {
            this.StatusLog.AddError("Error writing log file '" + System.Convert.ToString(logFile) + "', " + ex.Message);
        }
    }

    /// <summary>
    /// Save the log file
    /// </summary>
    /// <param name="logFile"></param>
    private void Execute_GenerateManualStepsFile(string filename)
    {
        try
        {
            var manualSteps = this.CustomerManualActionManager;
            if((manualSteps == null) || (manualSteps.Count == 0))
            {
                this.StatusLog.AddStatus("No manual steps file written because there are no necessary manual steps");
                return;
            }

            manualSteps.GenerateCSVFile(filename);

            //Record the file name
            _pathGeneratedManualStepsReport = filename;
        }
        catch (Exception ex)
        {
            this.StatusLog.AddError("Error writing manual steps file '" + System.Convert.ToString(filename) + "', " + ex.Message);
        }
    }

    /// <summary>
    /// Save the errors file
    /// </summary>
    /// <param name="logFile"></param>
    private void Execute_SaveErrorsFile(string filename)
    {
        try
        {
            string errorsText = this.StatusLog.ErrorText;

            //No text to write?  Do nothing...
            if(string.IsNullOrWhiteSpace(errorsText))
            {
                return;
            }
            System.IO.File.AppendAllText(filename, errorsText);
        }
        catch (Exception ex)
        {
            this.StatusLog.AddError("Error writing log file '" + System.Convert.ToString(filename) + "', " + ex.Message);
        }
    }

    /// <summary>
    /// Creates a TWB file that points to the CSV file
    /// </summary>
    /// <param name="pathReportCsv"></param>
    private void Execute_GenerateSiteInventoryFile_Twb(string pathReportCsv)
    {
        _statusLog.AddStatusHeader("Generate site inventory TWB");

        try
        {
            //Calculate the name/path for the output TWB.  It will match the name/path of the CSV file
            string pathTwbOut = PathHelper.GetInventoryTwbPathMatchingCsvPath(pathReportCsv);
            this.StatusLog.AddStatusHeader("Generating Tableau Workbook " + pathTwbOut);


            var twbGenerateFromTemplate = new TwbReplaceCSVReference(
                PathHelper.GetInventoryTwbTemplatePath(),   //*.twb we are using as our template
                pathTwbOut,                                 //Output *.twb we are generating
                "siteInventoryTemplate.csv",                //Datasource filename in tempalte workbook
                pathReportCsv,                              //CSV file we want to associate with the datasource above
                _statusLog);

            //Transform the template into the output file
            bool successRemapping = twbGenerateFromTemplate.Execute();
            if (!successRemapping)
            {
                this.StatusLog.AddError("Error generating site inventory TWB. No data source could be found to remap");
            }


            //Store it as our output
            if (File.Exists(pathTwbOut))
            {
                _pathGeneratedSiteInventoryReportTwb = pathTwbOut;
            }
        }
        catch(Exception ex)
        {
            StatusLog.AddError("Error generating Twb file: " + ex.ToString());
        }
    }

    /// <summary>
    /// Generate the status report of the site's inventory
    /// </summary>
    /// <param name="pathReport"></param>
    private void Execute_GenerateSiteInventoryFile(string pathReport)
    {
        _statusLog.AddStatusHeader("Generate site inventory CSV");

        try
        {
            var reportGenerator = new CustomerSiteInventory(
                this.ProjectsList,
                this.DatasourcesList,
                this.WorkbooksList,
                this.ViewsList,
                this.UsersList,
                this.GroupsList,
                this.SubscriptionsList,
                this.SchedulesList,
                this.TasksExtractRefreshesList,
                _statusLog);

            reportGenerator.GenerateCSVFile(pathReport);

            //Store the path to the report we just generated
            _pathGeneratedSiteInventoryReportCsv = pathReport;
        }
        catch (Exception ex)
        {
            StatusLog.AddError("Error generating inventory report: " + ex.ToString());
        }
    }

    /// <summary>
    /// Downloads the set of projects in the site
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <returns></returns>
    private DownloadProjectsList Execute_DownloadProjectsList(TableauServerSignIn onlineLogin)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Request site projects");
        DownloadProjectsList projects = null;
        //===================================================================================
        //Projects...
        //===================================================================================
        try
        {
            projects = new DownloadProjectsList(onlineUrls, onlineLogin);
            projects.ExecuteRequest();

            //List all the projects
            foreach (var singleProject in projects.Projects)
            {
                _statusLog.AddStatus(singleProject.ToString());
            }
        }
        catch (Exception ex)
        {
            _statusLog.AddError("Error during projects query, " + ex.ToString());
        }

        //Store it
        _downloadedList_Projects = projects.Projects;
        return projects;
    }

    /// <summary>
    /// Downloads the set of subscriptions in the site
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <returns></returns>
    private DownloadSubscriptionsList Execute_DownloadSubscriptionsList(TableauServerSignIn onlineLogin)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Request site subscriptions");
        DownloadSubscriptionsList subscriptions = null;
        //===================================================================================
        //Subscriptions...
        //===================================================================================
        try
        {
            subscriptions = new DownloadSubscriptionsList(onlineUrls, onlineLogin);
            subscriptions.ExecuteRequest();

            //List all the subscriptions
            foreach (var singleSubscription in subscriptions.Subscriptions)
            {
                _statusLog.AddStatus(singleSubscription.ToString());
            }
        }
        catch (Exception ex)
        {
            _statusLog.AddError("Error during subscriptions query, " + ex.ToString());
        }

        //Store it
        _downloadedList_Subscriptions = subscriptions.Subscriptions;
        return subscriptions;
    }

    /// <summary>
    /// Downloads the set of views in the site
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <returns></returns>
    private DownloadViewsList Execute_DownloadViewsList(TableauServerSignIn onlineLogin)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Request site views");
        DownloadViewsList siteViews = null;
        //===================================================================================
        //Views...
        //===================================================================================
        try
        {
            siteViews = new DownloadViewsList(onlineUrls, onlineLogin);
            siteViews.ExecuteRequest();

            //List all the subscriptions
            foreach (var singleView in siteViews.Views)
            {
                _statusLog.AddStatus(singleView.ToString());
            }
        }
        catch (Exception ex)
        {
            _statusLog.AddError("Error during views query, " + ex.ToString());
        }

        //Store it
        _downloadedList_Views = siteViews.Views;
        return siteViews;
    }

    /// <summary>
    /// Downloads the set of groups in the site
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <returns></returns>
    private DownloadGroupsList Execute_DownloadGroupsList(TableauServerSignIn onlineLogin)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Request site groups");
        DownloadGroupsList groups = null;
        //===================================================================================
        //Projects...
        //===================================================================================
        try
        {
            groups = new DownloadGroupsList(onlineUrls, onlineLogin);
            groups.ExecuteRequest();

            //List all the groups
            foreach (var thisGroup in groups.Groups)
            {
                _statusLog.AddStatus(thisGroup.ToString());
            }
        }
        catch (Exception ex)
        {
            _statusLog.AddError("Error during groups query, " + ex.ToString());
        }

        //Store it
        _downloadedList_Groups = groups.Groups;
        return groups;
    }

    /// <summary>
    /// Called to perform Uploads of the data sources
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="localBasePath"></param>
    /// <param name="credentialManager">Database credentials that will be associaed with uploaded content</param>
    private void Execute_UploadDatasources(
        TableauServerSignIn onlineLogin, 
        string localBasePath,
        CredentialManager credentialManager,
        bool attemptContentOwnershipAssignment,
        IEnumerable<SiteUser> siteUsers)
    {
        StatusLog.AddStatusHeader("Upload datasources");

        if (string.IsNullOrWhiteSpace(localBasePath))
        {
            _statusLog.AddError("Abort uploads. Local path is not specified");
            return;
        }

        string pathDataSources = Path.Combine(localBasePath, "datasources");
        if(!Directory.Exists(pathDataSources))
        {
            _statusLog.AddStatus("Skipping datasources upload. Local datasources path does not exist: \"" + pathDataSources+ "\"");
            return;
        }

        //Upload all the files
        var uploadProjectBehavior = new UploadBehaviorProjects(
            _taskOptions.IsOptionSet(TaskMasterOptions.Option_UploadCreateNeededProjects), 
            true);


        var dsUploader = new UploadDatasources(
            _onlineUrls, 
            onlineLogin,
            credentialManager,
            pathDataSources, 
            uploadProjectBehavior, 
            _manualActions,
            attemptContentOwnershipAssignment,
            siteUsers,
            this.UploadChunksSizeBytes,
            this.UploadChunksDelaySeconds);
        try
        {
            dsUploader.ExecuteRequest();
        }
        catch(Exception exUploader)
        {
            StatusLog.AddError("Aborted upload datasources. Unexpected error + " + exUploader.Message);
        }
    }


    /// <summary>
    /// Return the Chunk size we want to use for uploads
    /// </summary>
    private int UploadChunksSizeBytes
    {
        get
        {
            int chunkSize = 8000000;
            var optionValue = _taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_UploadChunkSizeBytes);
            if (!string.IsNullOrWhiteSpace(optionValue))
            {
                chunkSize =  System.Convert.ToInt32(optionValue);
            }

            System.Diagnostics.Debug.Assert(chunkSize > 0, "Chunk size must be postive");
            return chunkSize;
        }
    }

    /// <summary>
    /// Return the testing delay we want to have after we upload a chunk
    /// </summary>
    private int UploadChunksDelaySeconds
    {
        get
        {
            var optionValue = _taskOptions.GetOptionValue(TaskMasterOptions.OptionParameter_UploadChunkDelaySeconds);
            if (!string.IsNullOrWhiteSpace(optionValue))
            {
                return System.Convert.ToInt32(optionValue);
            }

            return 0; //No delay
        }
    }


    /// <summary>
    /// Called to perform Uploads of the workbooks
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="localBasePath"></param>
    /// <param name="remapWorkbookReferences">TRUE is we want to transform workbooks to remap any published datasources to the new server/site we are uploading to</param>
    /// <param name="attemptContentOwnershipAssignment">TRUE: Look for content metadata files, and attempt to assign the owner to the published content</param>
    /// <param name="credentialManager">Database credentials to associate with content we are uploading</param>
    /// <param name="siteUsers">Users in site, needed for content ownership remapping</param>
    private void Execute_UploadWorkbooks(
        TableauServerSignIn onlineLogin, 
        string localBasePath, 
        bool remapWorkbookReferences, 
        CredentialManager credentialManager,
        bool attemptContentOwnershipAssignment,
        IEnumerable<SiteUser> siteUsers)
    {
        StatusLog.AddStatusHeader("Upload workbooks");

        if (string.IsNullOrWhiteSpace(localBasePath))
        {
            _statusLog.AddError("Abort uploads. Local path is not specified");
            return;
        }

        string pathWorkbooks = Path.Combine(localBasePath, "workbooks");
        if (!Directory.Exists(pathWorkbooks))
        {
            _statusLog.AddStatus("Skipping workbooks upload. Local workbooks path does not exist: \"" + pathWorkbooks + "\"");
            return;
        }

        //Do we have a directory to perform remapping
        string pathRemappingTempspace = Path.Combine(localBasePath, "_remapTempspace");
        if (!Directory.Exists(pathRemappingTempspace))
        {
            Directory.CreateDirectory(pathRemappingTempspace);
        }


        //Upload all the files
        var uploadProjectBehavior = new UploadBehaviorProjects(
            _taskOptions.IsOptionSet(TaskMasterOptions.Option_UploadCreateNeededProjects), 
            true);

        var dsUploader = new UploadWorkbooks(
            _onlineUrls, 
            onlineLogin,
            credentialManager,
            pathWorkbooks, 
            remapWorkbookReferences, 
            pathRemappingTempspace, 
            uploadProjectBehavior, 
            _manualActions,
            attemptContentOwnershipAssignment,
            siteUsers,
            this.UploadChunksSizeBytes,
            this.UploadChunksDelaySeconds
            );
        try
        {
            dsUploader.ExecuteRequest();
        }
        catch (Exception exUploader)
        {
            StatusLog.AddError("Aborted upload workbooks. Unexpected error + " + exUploader.Message);
        }
    }


    /// <summary>
    /// Attempts to create a project
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <param name="projectName"></param>
    private void Execute_CreateProjectWithName(TableauServerSignIn onlineLogin, string projectName)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Create project: " + projectName);
        try
        {
            var createProject = new SendCreateProject(onlineUrls, onlineLogin, projectName);
            createProject.ExecuteRequest();
        }
        catch (Exception ex)
        {
            _statusLog.AddError("Error during project create, " + ex.Message);
        }
    }

    /// <summary>
    /// Download the users in the site
    /// </summary>
    /// <param name="onlineLogin"></param>
    /// <returns></returns>
    private IEnumerable<SiteUser> Execute_DownloadSiteUsers(TableauServerSignIn onlineLogin)
    {
        var onlineUrls = _onlineUrls;
        _statusLog.AddStatusHeader("Request site users");
        DownloadUsersList users = null;
        //===================================================================================
        //Users...
        //===================================================================================
        try
        {
            users = new DownloadUsersList(onlineUrls, onlineLogin);
            users.ExecuteRequest();

            //List all the users
            foreach(var singleUser in users.Users)
            {
                _statusLog.AddStatus("user: " + singleUser.Name + "/" + singleUser.SiteRole + "/" + singleUser.Id.ToString());
            }
            return users.Users;
        }
        catch (Exception exUsersQuery)
        {
            _statusLog.AddError("Error during users query, " + exUsersQuery.ToString());
            return null;
        }
    }
}
