using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's groups we have downloaded
/// </summary>
class DownloadGroupsList : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Groups we've parsed from server results
    /// </summary>
    private List<SiteGroup> _groups;
    public IEnumerable<SiteGroup> Groups
    {
        get
        {
            var ds = _groups;
            if (ds == null) return null;
            return ds.AsReadOnly();
        }
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadGroupsList(TableauServerUrls onlineUrls, TableauServerSignIn login)
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
        var siteGroups = new List<SiteGroup>();

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(siteGroups, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Groups error during page request: " + exPageRequest.Message);
            }
        }

        _groups = siteGroups;
    }

    /// <summary>
    /// Get a page's worth of Groups
    /// </summary>
    /// <param name="onlineGroups"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(
        List<SiteGroup> onlineGroups, 
        int pageToRequest, 
        out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_GroupsList(_onlineSession, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get groups list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the group nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var groups = xmlDoc.SelectNodes("//iwsOnline:group", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in groups)
        {
            SiteGroup thisGroup = null;
            try
            {
                thisGroup = new SiteGroup(
                    itemXml, 
                    null);   //We'll get and add the list of users later (see below)
                onlineGroups.Add(thisGroup);
                SanityCheckGroup(thisGroup, itemXml);
            }
            catch(Exception exGetGroup)
            {
                AppDiagnostics.Assert(false, "Group parse error");
                _onlineSession.StatusLog.AddError("Error parsing group: " + itemXml.OuterXml + ", " + exGetGroup.Message);
            }


            //==============================================================
            //Get the set of users in the group
            //==============================================================
            if (thisGroup != null)
            {
                try
                {
                    var downloadUsersInGroup = new DownloadUsersListInGroup(
                        _onlineUrls, 
                        _onlineSession, 
                        thisGroup.Id);
                    downloadUsersInGroup.ExecuteRequest();
                    thisGroup.AddUsers(downloadUsersInGroup.Users);
                }
                catch (Exception exGetUsers)
                {
                    _onlineSession.StatusLog.AddError("Error parsing group's users: " + exGetUsers.Message);
                }
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
    /// Does sanity checking and error logging on missing data in groups
    /// </summary>
    /// <param name="group"></param>
    private void SanityCheckGroup(SiteGroup group, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(group.Id))
        {
            _onlineSession.StatusLog.AddError(group.Name + " is missing a group ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }


    /// <summary>
    /// Finds a group with matching name
    /// </summary>
    /// <param name="findName"></param>
    /// <returns></returns>
    public SiteGroup FindGroupWithName(string findName)
    {
        foreach(var thisGroup in _groups)
        {
            if (thisGroup.Name == findName)
            {
                return thisGroup;
            }
        }

        return null; //Not found
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    SiteGroup FindGroupWithId(string groupId)
    {
        foreach(var thisGroup in _groups)
        {
            if (thisGroup.Id == groupId) { return thisGroup; }
        }

        return null;
    }

    /// <summary>
    /// Adds to the list
    /// </summary>
    /// <param name="newGroup"></param>
    internal void AddGroup(SiteGroup newGroup)
    {
        _groups.Add(newGroup);
    }
}
