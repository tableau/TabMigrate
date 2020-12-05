using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Downloads the list of data sources
/// </summary>
class DownloadDatasourcesList : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Workbooks we've parsed from server results
    /// </summary>
    private List<SiteDatasource> _datasources;
    public ICollection<SiteDatasource> Datasources
    {
        get
        {
            var ds = _datasources;
            if (ds == null) return null;
            return ds.AsReadOnly();
        }
    }

//    private readonly OnlineUser _user;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadDatasourcesList(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
//        _user = user;
    }

    /// <summary>
    /// Request the data from Online
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {

        var onlineDatasources = new List<SiteDatasource>();
        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineDatasources, thisPage, out numberPages);
            }
            catch(Exception exPageRequest)
            {
                StatusLog.AddError("Datasources error during page request: " + exPageRequest.Message);
            }
        }
        _datasources = onlineDatasources;
    }

    /// <summary>
    /// Get a page's worth of Data Sources
    /// </summary>
    /// <param name="onlineDatasources"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteDatasource> onlineDatasources, int pageToRequest, out int totalNumberPages)
    {
        int pageSize =_onlineUrls.PageSize; 
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_DatasourcesList(_onlineSession, pageSize, pageToRequest);

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var xmlDoc = ResourceSafe_PerformWebRequest_GetXmlDocument(urlQuery, "get datasources list");

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var datasources = xmlDoc.SelectNodes("//iwsOnline:datasource", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in datasources)
        {
            try
            {
                var ds = new SiteDatasource(itemXml);
                onlineDatasources.Add(ds);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Datasource parse error");
                _onlineSession.StatusLog.AddError("Error parsing datasource: " + itemXml.InnerXml);
            }
        } //end: foreach

        //-------------------------------------------------------------------
        //Get the updated page-count
        //-------------------------------------------------------------------
        totalNumberPages  =DownloadPaginationHelper.GetNumberOfPagesFromPagination(
            xmlDoc.SelectSingleNode("//iwsOnline:pagination", nsManager),
            pageSize);
    }
}
