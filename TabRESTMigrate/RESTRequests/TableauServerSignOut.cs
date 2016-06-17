using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;


/// <summary>
/// Handles sign out
/// </summary>
class TableauServerSignOut : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="user"></param>
    public TableauServerSignOut(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var statusLog = _onlineSession.StatusLog;

        //Create a web request, in including the users logged-in auth information in the request headers
        var urlRequest = _onlineUrls.UrlLogout;
        var webRequest = CreateLoggedInWebRequest(urlRequest);
        webRequest.Method = "POST";

        //Request the data from server
        _onlineSession.StatusLog.AddStatus("Web request: " + urlRequest, -10);
        var response = GetWebReponseLogErrors(webRequest, "sign out");

    }
}
