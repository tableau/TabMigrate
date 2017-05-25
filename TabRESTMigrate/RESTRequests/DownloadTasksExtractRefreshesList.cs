using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's tasks we have downloaded
/// </summary>
class DownloadTasksExtractRefreshesList : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _scheduleId;

    /// <summary>
    /// Tasks we've parsed from server results
    /// </summary>
    private List<SiteTaskExtractRefresh> _tasks;
    public IEnumerable<SiteTaskExtractRefresh> Tasks
    {
        get
        {
            var thisList = _tasks;
            if (thisList == null) return null;
            return thisList.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="scheduleId">Must be a GUID for a schedule</param>
    public DownloadTasksExtractRefreshesList(TableauServerUrls onlineUrls, TableauServerSignIn login, string scheduleId)
        : base(login)
    {
        if(string.IsNullOrWhiteSpace(scheduleId))
        {
            throw new ArgumentException("DownloadTasksExtractRefreshesList: Missign schedule id");
        }

        _onlineUrls = onlineUrls;
        _scheduleId = scheduleId;

    }

    /// <summary>
    /// Request the data from Online
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var onlineTasks = new List<SiteTaskExtractRefresh>();        

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineTasks, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Tasks error during page request: " + exPageRequest.Message);
            }
        }

        _tasks = onlineTasks;
    }

    /// <summary>
    /// Get a page's worth of Tasks listing
    /// </summary>
    /// <param name="onlineTasks"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteTaskExtractRefresh> onlineTasks, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_TasksExtractRefreshesForScheduleList(_onlineSession, _scheduleId, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get tasks list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the task nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var tasks = xmlDoc.SelectNodes("//iwsOnline:extract", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in tasks)
        {
            try
            {
                var thisTask = new SiteTaskExtractRefresh(itemXml, _scheduleId);
                onlineTasks.Add(thisTask);

                SanityCheckTask(thisTask, itemXml);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Task parse error");
                _onlineSession.StatusLog.AddError("Error parsing task: " + itemXml.OuterXml);
            }
        } //end: foreach

        //-------------------------------------------------------------------
        //Get the updated page-count
        //-------------------------------------------------------------------
        totalNumberPages = DownloadPaginationHelper.GetNumberOfPagesFromPagination(
            xmlDoc.SelectSingleNode("//iwsOnline:pagination", nsManager),
            pageSize);
    }

    /// <summary>
    /// Does sanity checking and error logging on missing data in tasks
    /// </summary>
    /// <param name="task"></param>
    private void SanityCheckTask(SiteTaskExtractRefresh task, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(task.Id))
        {
            _onlineSession.StatusLog.AddError("Task in schedule " + _scheduleId + " is missing a task ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SiteTaskExtractRefresh FindTaskWithId(string id)
    {
        foreach(var thisItem in _tasks)
        {
            if (thisItem.Id == id) { return thisItem; }
        }

        return null;
    }

    /// <summary>
    /// Adds a task to the list
    /// </summary>
    /// <param name="newTask"></param>
    internal void AddTask(SiteTaskExtractRefresh newTask)
    {
        _tasks.Add(newTask);
    }
}
