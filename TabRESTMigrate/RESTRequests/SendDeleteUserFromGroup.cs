using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Sends a request to DELETE a user from a group
/// </summary>
class SendDeleteUserFromGroup : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// ID for the user to remove from the group
    /// </summary>
    private readonly string _userId;

    /// <summary>
    /// Group we want to delete from
    /// </summary>
    private readonly string _groupId;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="login"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    public SendDeleteUserFromGroup(
        TableauServerSignIn login,
        string userId,
        string groupId)
        : base(login)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new ArgumentException("Not allowed to delete a blank group id");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Not allowed to delete a blank user id");
        }

        _onlineUrls = login.ServerUrls;
        _userId = userId;
        _groupId = groupId;
    }

    /// <summary>
    /// Delete the user from the group
    /// </summary>
    /// <param name="serverName"></param>
    public bool ExecuteRequest()
    {
        try
        {
            //Attempt the delete
            bool wasSuccessful = DeleteUserFromGroup(_userId, _groupId);
            this.StatusLog.AddStatus("User deleted from group " + _userId + "/" + _groupId);
            return wasSuccessful;
        }
        catch (Exception exRequest)
        {
            this.StatusLog.AddError("Error attempting to delete user from group " + _userId + "/" + _groupId + "', " + exRequest.Message);
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    private bool DeleteUserFromGroup(string userId, string groupId)
    {
        //ref: https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api_ref.htm#remove_user_to_group

        //Create a web request 
        var urlDeleteUserFromGroup = _onlineUrls.Url_DeleteUserFromGroup(_onlineSession, userId, groupId);

        bool success = ResourceSafe_PerformWebRequestResponseLogErrors(
            urlDeleteUserFromGroup,
            "delete user from group",
            "DELETE");

        return success;

        //var webRequest = this.CreateLoggedInWebRequest(urlDeleteUserFromGroup, "DELETE");
        //var response = GetWebReponseLogErrors(webRequest, "delete user from group");
        //response.Dispose();
        //return true; //If we did not get an error code, assume success
    }

}
