using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's schedules we have downloaded
/// </summary>
class DownloadSchedulesList : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Schedules we've parsed from server results
    /// </summary>
    private List<SiteSchedule> _schedules;
    public IEnumerable<SiteSchedule> Schedules
    {
        get
        {
            var thisList = _schedules;
            if (thisList == null) return null;
            return thisList.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadSchedulesList(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
    }

    /// <summary>
    /// Request the data from Online
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var onlineSchedules = new List<SiteSchedule>();        

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineSchedules, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Schedules error during page request: " + exPageRequest.Message);
            }
        }

        _schedules = onlineSchedules;
    }

    /// <summary>
    /// Get a page's worth of Schedules listing
    /// </summary>
    /// <param name="onlineSchedules"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteSchedule> onlineSchedules, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_SchedulesList(_onlineSession, pageSize, pageToRequest);

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var xmlDoc = ResourceSafe_PerformWebRequest_GetXmlDocument(urlQuery, "get schedules list");

        //Get all the schedule nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var schedules = xmlDoc.SelectNodes("//iwsOnline:schedule", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in schedules)
        {
            try
            {
                var thisSchedule = new SiteSchedule(itemXml);
                onlineSchedules.Add(thisSchedule);

                SanityCheckSchedule(thisSchedule, itemXml);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Schedule parse error");
                _onlineSession.StatusLog.AddError("Error parsing schedule: " + itemXml.OuterXml);
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
    /// Does sanity checking and error logging on missing data in schedules
    /// </summary>
    /// <param name="schedule"></param>
    private void SanityCheckSchedule(SiteSchedule schedule, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(schedule.Id))
        {
            _onlineSession.StatusLog.AddError(schedule.ScheduleName +  " is missing a schedule ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SiteSchedule FindScheduleWithId(string id)
    {
        foreach(var thisItem in _schedules)
        {
            if (thisItem.Id == id) { return thisItem; }
        }

        return null;
    }

    /// <summary>
    /// Adds a schedule to the list
    /// </summary>
    /// <param name="newSchedule"></param>
    internal void AddSchedule(SiteSchedule newSchedule)
    {
        _schedules.Add(newSchedule);
    }
}
