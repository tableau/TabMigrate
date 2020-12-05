using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Sends a request to DELETE a user from a site
/// </summary>
class SendDeleteUser : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// ID for the user to remove from the site
    /// </summary>
    private readonly string _userId;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="login"></param>
    /// <param name="userId"></param>
    /// <param name=" siteId"></param>
    public SendDeleteUser(
        TableauServerSignIn login,
        string userId)
        : base(login)
    {

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Not allowed to delete a blank user id");
        }

        _onlineUrls = login.ServerUrls;
        _userId = userId;
    }

    /// <summary>
    /// Delete the user from the site
    /// </summary>
    /// <param name="serverName"></param>
    public bool ExecuteRequest()
    {
        try
        {
            //Attempt the delete
            bool wasSuccessful = DeleteUserFromSite(_userId);
            this.StatusLog.AddStatus("User deleted from site " + _userId);
            return wasSuccessful;
        }
        catch (Exception exRequest)
        {
            this.StatusLog.AddError("Error attempting to delete user from site " + _userId + "', " + exRequest.Message);
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    private bool DeleteUserFromSite(string userId)
    {
        //ref: site

        //Create a web request 
        var urlDeleteUser = _onlineUrls.Url_DeleteUserFromSite(_onlineSession, userId);

        bool success = ResourceSafe_PerformWebRequestResponseLogErrors(
            urlDeleteUser,
            "delete user from site",
            "DELETE");

        return success;
    }

}
