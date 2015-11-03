using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

class DownloadSiteInfo : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    /// <summary>
    /// Workbooks we've parsed from server results
    /// </summary>
    private SiteinfoSite _onlineSite;
    public SiteinfoSite Site
    {
        get
        {
            return _onlineSite;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="user"></param>
    public DownloadSiteInfo(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
//        _user = user;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var statusLog = _onlineSession.StatusLog;

        //Create a web request, in including the users logged-in auth information in the request headers
        var urlRequest = _onlineUrls.Url_SiteInfo(_onlineSession);
        var webRequest = CreateLoggedInWebRequest(urlRequest);
        webRequest.Method = "GET";

        //Request the data from server
        _onlineSession.StatusLog.AddStatus("Web request: " + urlRequest, -10);
        var response = GetWebReponseLogErrors(webRequest, "get site info");
        
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var sites = xmlDoc.SelectNodes("//iwsOnline:site", nsManager);

        int numberSites = 0;
        foreach(XmlNode contentXml in sites)
        {
            try
            {
                numberSites++;
                var site = new SiteinfoSite(contentXml);
                _onlineSite = site;

                statusLog.AddStatus("Site info: " + site.Name + "/" + site.Id + "/" + site.State);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Site parse error");
                statusLog.AddError("Error parsing site: " + contentXml.InnerXml);
            }
        }

        //Sanity check
        if(numberSites > 1)
        {
            statusLog.AddError("Error - how did we get more than 1 site? " + numberSites.ToString() + " sites");
        }
    }
}
