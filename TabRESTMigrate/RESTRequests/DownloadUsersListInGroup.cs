using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Get's the list of users in a Tableau Server site.
/// 
/// This derives from a base class because "Getting the set of users on the site" and "Getting the set of users in a group" are very similar 
/// and can share most of the code
/// </summary>
class DownloadUsersListInGroup : DownloadUsersListBase
{
    private readonly string _groupId;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadUsersListInGroup(TableauServerUrls onlineUrls, TableauServerSignIn login, string groupId) 
        : base(onlineUrls, login)
    {
        _groupId = groupId;
    }


    /// <summary>
    /// Generate the URL we use to request the list of users in the site
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    protected override string  UrlForUsersListRequest(int pageSize, int pageNumber)
    {
        //The URL to get us the data
        return _onlineUrls.Url_UsersListInGroup(_onlineSession, _groupId, pageSize, pageNumber);
    }

}
