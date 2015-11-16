using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Downloads the list of Workbooks from the server
/// </summary>
class DownloadWorkbookConnections : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _workbookId;

    /// <summary>
    /// Workbooks we've parsed from server results
    /// </summary>
    private List<SiteConnection> _connections;
    public ICollection<SiteConnection> Connections
    {
        get
        {
            var connections = _connections;
            if (connections == null) return null;
            return connections.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor: Call when we want to query the workbooks on behalf of the currently logged in user
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadWorkbookConnections(TableauServerUrls onlineUrls, TableauServerSignIn login, string workbookId)
        : base(login)
    {
        _workbookId = workbookId;
        _onlineUrls = onlineUrls;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var wbConnections = new List<SiteConnection>();

        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_WorkbookConnectionsList(_onlineSession, _workbookId);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get workbook's connections list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var connections = xmlDoc.SelectNodes("//iwsOnline:connection", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in connections)
        {
            try
            {
                var connection = new SiteConnection(itemXml);
                wbConnections.Add(connection);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Workbook  connections parse error");
                _onlineSession.StatusLog.AddError("Error parsing workbook: " + itemXml.InnerXml);
            }
        } //end: foreach

        _connections = wbConnections;
    }
}
