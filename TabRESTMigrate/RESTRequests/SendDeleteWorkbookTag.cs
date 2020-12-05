using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Sends a request to DELETE a tag from a site's workbook
/// </summary>
class SendDeleteWorkbookTag : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// ID for the content
    /// </summary>
    private readonly string _contentId;

    /// <summary>
    /// Tag we want to delete
    /// </summary>
    private readonly string _tagText;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="workbookId"></param>
    /// <param name="tagText"></param>
    public SendDeleteWorkbookTag(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string workbookId,
        string tagText)
        : base(login)
    {
        if(string.IsNullOrWhiteSpace(tagText))
        {
            throw new ArgumentException("Not allowed to delete a blank tag");
        }

        if (string.IsNullOrWhiteSpace(workbookId))
        {
            throw new ArgumentException("Not allowed to delete a tag without workbook id");
        }

        _onlineUrls = onlineUrls;
        _contentId = workbookId;
        _tagText = tagText;
    }

    /// <summary>
    /// Delete the tag
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        try
        {
            //Attempt the delete
            DeleteTagFromContent(_contentId, _tagText);
            this.StatusLog.AddStatus("Tag deleted from workbook "  + _contentId + "/" + _tagText);
        }
        catch (Exception exProject)
        {
            this.StatusLog.AddError("Error attempting to delete content tag " + _contentId + "/" + _tagText + "', " + exProject.Message);
            return;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="workbookId"></param>
    /// <param name="tagText"></param>
    private void DeleteTagFromContent(string workbookId, string tagText)
    {
        //ref: http://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Delete_Tag_from_Workbook%3FTocPath%3DAPI%2520Reference%7C_____20

        //Create a web request 
        var urlDeleteContentTag = _onlineUrls.Url_DeleteWorkbookTag(_onlineSession, workbookId, tagText);
        //var webRequest = this.CreateLoggedInWebRequest(urlDeleteContentTag, "DELETE");
        //var response = GetWebReponseLogErrors(webRequest, "delete tag from content request"); 
        ResourceSafe_PerformWebRequestResponseLogErrors(
            urlDeleteContentTag,
            "delete tag from content request",
            "DELETE");
    }
}
