using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's subscriptions we have downloaded
/// </summary>
class DownloadSubscriptionsList : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Subscriptions we've parsed from server results
    /// </summary>
    private List<SiteSubscription> _subscriptions;
    public IEnumerable<SiteSubscription> Subscriptions
    {
        get
        {
            var ds = _subscriptions;
            if (ds == null) return null;
            return ds.AsReadOnly();
        }
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadSubscriptionsList(TableauServerUrls onlineUrls, TableauServerSignIn login)
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
        var onlineSubscriptions = new List<SiteSubscription>();

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineSubscriptions, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Subscriptions error during page request: " + exPageRequest.Message);
            }
        }

        _subscriptions = onlineSubscriptions;
    }

    /// <summary>
    /// Get a page's worth of Subscriptions listing
    /// </summary>
    /// <param name="onlineSubscriptions"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteSubscription> onlineSubscriptions, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_SubscriptionsList(_onlineSession, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get subscriptions list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the subscription nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var subscriptions = xmlDoc.SelectNodes("//iwsOnline:subscription", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in subscriptions)
        {
            try
            {
                var thisSubscription = new SiteSubscription(itemXml);
                onlineSubscriptions.Add(thisSubscription);

                SanityCheckSubscription(thisSubscription, itemXml);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Subscription parse error");
                _onlineSession.StatusLog.AddError("Error parsing subscription: " + itemXml.OuterXml);
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
    /// Does sanity checking and error logging on missing data in subscriptions
    /// </summary>
    /// <param name="subscription"></param>
    private void SanityCheckSubscription(SiteSubscription subscription, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(subscription.Id))
        {
            _onlineSession.StatusLog.AddError(subscription.Subject + "/" + subscription.UserName +  " is missing a subscription ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SiteSubscription FindSubscriptionWithId(string id)
    {
        foreach(var thisItem in _subscriptions)
        {
            if (thisItem.Id == id) { return thisItem; }
        }

        return null;
    }

    /// <summary>
    /// Adds a subscription to the list
    /// </summary>
    /// <param name="newSubscription"></param>
    internal void AddSubscription(SiteSubscription newSubscription)
    {
        _subscriptions.Add(newSubscription);
    }
}
