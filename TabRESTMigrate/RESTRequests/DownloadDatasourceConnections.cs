using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Downloads the data connection in a published data source
/// </summary>
class DownloadDatasourceConnections : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _datasourceId;

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
    /// Constructor: Call when we want to query the datasource on behalf of the currently logged in user
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadDatasourceConnections(TableauServerUrls onlineUrls, TableauServerSignIn login, string datasourceId)
        : base(login)
    {
        _datasourceId = datasourceId;
        _onlineUrls = onlineUrls;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var dsConnections = new List<SiteConnection>();

        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_DatasourceConnectionsList(_onlineSession, _datasourceId);

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var xmlDoc = ResourceSafe_PerformWebRequest_GetXmlDocument(urlQuery, "get datasources's connections list");

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var connections = xmlDoc.SelectNodes("//iwsOnline:connection", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in connections)
        {
            try
            {
                var connection = new SiteConnection(itemXml);
                dsConnections.Add(connection);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Workbook  connections parse error");
                _onlineSession.StatusLog.AddError("Error parsing workbook: " + itemXml.InnerXml);
            }
        } //end: foreach

        _connections = dsConnections;
    }
}
