using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Sends a request to DELETE a tag from a site's datasource
/// </summary>
class SendDeleteDatasourceTag : TableauServerSignedInRequestBase
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
    /// <param name="datasourceId"></param>
    /// <param name="tagText"></param>
    public SendDeleteDatasourceTag(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string datasourceId,
        string tagText)
        : base(login)
    {
        if(string.IsNullOrWhiteSpace(tagText))
        {
            throw new ArgumentException("Not allowed to delete a blank tag");
        }

        if (string.IsNullOrWhiteSpace(datasourceId))
        {
            throw new ArgumentException("Not allowed to delete a tag without datasource id");
        }

        _onlineUrls = onlineUrls;
        _contentId = datasourceId;
        _tagText = tagText;
    }

    /// <summary>
    /// Delete the tag
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        //**********************************************************************************
        //** [2015-10-28] Functionality not yet supported by Tableau Server
        //** Remove this error when Server has implemented and this has been tested
        //**********************************************************************************
        throw new Exception("2015-10-28: API does not yet exist on server for Remove Tag from Datasource");
        /*
        try
        {
            //Attempt the delete
            DeleteTagFromContent(_contentId, _tagText);
            this.StatusLog.AddStatus("Tag deleted from datasource "  + _contentId + "/" + _tagText);
        }
        catch (Exception exProject)
        {
            this.StatusLog.AddError("Error attempting to delete content tag " + _contentId + "/" + _tagText + "', " + exProject.Message);
            return;
        }
        */
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="datasourceId"></param>
    /// <param name="tagText"></param>
    private bool DeleteTagFromContent(string datasourceId, string tagText)
    {
        //Create a web request 
        var urlRequest = _onlineUrls.Url_DeleteDatasourceTag(_onlineSession, datasourceId, tagText);
        //var webRequest = this.CreateLoggedInWebRequest(urlRequest, "DELETE");
        //var response = GetWebReponseLogErrors(webRequest, "delete tag from content request");

        return ResourceSafe_PerformWebRequestResponseLogErrors(
            urlRequest,
            "delete tag from content request",
            "DELETE");
    }

}
