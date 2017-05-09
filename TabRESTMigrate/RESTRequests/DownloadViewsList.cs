using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's views we have downloaded
/// </summary>
class DownloadViewsList : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Views we've parsed from server results
    /// </summary>
    private List<SiteView> _views;
    public IEnumerable<SiteView> Views
    {
        get
        {
            var ds = _views;
            if (ds == null) return null;
            return ds.AsReadOnly();
        }
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadViewsList(TableauServerUrls onlineUrls, TableauServerSignIn login)
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
        var onlineViews = new List<SiteView>();

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineViews, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Views error during page request: " + exPageRequest.Message);
            }
        }

        _views = onlineViews;
    }

    /// <summary>
    /// Get a page's worth of Views listing
    /// </summary>
    /// <param name="onlineViews"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteView> onlineViews, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_ViewsList(_onlineSession, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get views list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the view nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var views = xmlDoc.SelectNodes("//iwsOnline:view", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in views)
        {
            try
            {
                var thisView = new SiteView(itemXml);
                onlineViews.Add(thisView);

                SanityCheckView(thisView, itemXml);
            }
            catch
            {
                AppDiagnostics.Assert(false, "View parse error");
                _onlineSession.StatusLog.AddError("Error parsing view: " + itemXml.OuterXml);
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
    /// Does sanity checking and error logging on missing data in views
    /// </summary>
    /// <param name="view"></param>
    private void SanityCheckView(SiteView view, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(view.Id))
        {
            _onlineSession.StatusLog.AddError(view.Name  +  " is missing a view ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }

    /*
    /// <summary>
    /// Finds a view with matching name
    /// </summary>
    /// <param name="findViewName"></param>
    /// <returns></returns>
    public SiteView FindViewWithName(string findViewName)
    {
        foreach(var proj in _views)
        {
            if(proj.Name == findViewName)
            {
                return proj;
            }
        }

        return null; //Not found
    }
    */
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SiteView FindViewWithId(string id)
    {
        foreach(var thisItem in _views)
        {
            if (thisItem.Id == id) { return thisItem; }
        }

        return null;
    }

    /// <summary>
    /// Adds a view to the list
    /// </summary>
    /// <param name="newView"></param>
    internal void AddView(SiteView newView)
    {
        _views.Add(newView);
    }
}
