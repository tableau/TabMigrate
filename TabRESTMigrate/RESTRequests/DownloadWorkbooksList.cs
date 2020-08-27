using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Downloads the list of Workbooks from the server
/// </summary>
class DownloadWorkbooksList : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _userId;

    /// <summary>
    /// Workbooks we've parsed from server results
    /// </summary>
    private List<SiteWorkbook> _workbooks;
    public ICollection<SiteWorkbook> Workbooks
    {
        get
        {
            var wb = _workbooks;
            if (wb == null) return null;
            return wb.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor: Call when we want to query the workbooks on behalf of the currently logged in user
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadWorkbooksList(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _userId = login.UserId;
    }

    /// <summary>
    /// Constructor: Call when we want to query the Workbooks on behalf of an explicitly specified user
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="user"></param>
    public DownloadWorkbooksList(TableauServerUrls onlineUrls, TableauServerSignIn login, string userId) : base(login)
    {
        _onlineUrls = onlineUrls;
        _userId = userId;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        //Sanity check
        if(string.IsNullOrWhiteSpace(_userId))
        {
            _onlineSession.StatusLog.AddError("User ID required to query workbooks");            
        }

        var onlineWorkbooks = new List<SiteWorkbook>();
        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineWorkbooks, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Workbooks error during page request: " + exPageRequest.Message);
            }
        }

        _workbooks = onlineWorkbooks;
    }

    /// <summary>
    /// Get a page's worth of Workbook listings
    /// </summary>
    /// <param name="onlineWorkbooks"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteWorkbook> onlineWorkbooks, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_WorkbooksListForUser(_onlineSession, _userId, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var xmlDoc = GetWebReponseLogErrors_AsXmlDoc(webRequest, "get workbooks list");

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var workbooks = xmlDoc.SelectNodes("//iwsOnline:workbook", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in workbooks)
        {
            try
            {
                var ds = new SiteWorkbook(itemXml);
                onlineWorkbooks.Add(ds);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Workbook parse error");
                _onlineSession.StatusLog.AddError("Error parsing workbook: " + itemXml.InnerXml);
            }
        } //end: foreach

        //-------------------------------------------------------------------
        //Get the updated page-count
        //-------------------------------------------------------------------
        totalNumberPages = DownloadPaginationHelper.GetNumberOfPagesFromPagination(
            xmlDoc.SelectSingleNode("//iwsOnline:pagination", nsManager),
            pageSize);
    }

}
