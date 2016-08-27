using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Attempts to update the owner of a workbook on server
/// </summary>
class SendUpdateWorkbookOwner: TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    private readonly string _newOwnerId;
    private readonly string _workbookId;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="workbookId">GUID</param>
    /// <param name="newOwnerId">GUID</param>
    public SendUpdateWorkbookOwner(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string workbookId,
        string newOwnerId)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _workbookId = workbookId;
        _newOwnerId = newOwnerId;
    }

    /// <summary>
    /// Change the owner of a workbook on server
    /// </summary>
    /// <param name="serverName"></param>
    public SiteWorkbook ExecuteRequest()
    {
        try
        {
            var wb = ChangeContentOwner(_workbookId, _newOwnerId);
            this.StatusLog.AddStatus("Workbook ownership changed. wb:" + wb.Name + "/" + wb.Id +  ", new owner:" + wb.OwnerId);
            return wb;
        }
        catch (Exception exError)
        {
            this.StatusLog.AddError("Error attempting to change the workbook '" + _workbookId + "' owner to '" + _newOwnerId + "', " + exError.Message);
            return null;
        }
    }


    private SiteWorkbook ChangeContentOwner(string workbookId, string newOwnerId)
    {
        AppDiagnostics.Assert(!string.IsNullOrWhiteSpace(workbookId), "missing workbook id");
        AppDiagnostics.Assert(!string.IsNullOrWhiteSpace(newOwnerId), "missing owner id");

        //ref: https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Update_Workbook%3FTocPath%3DAPI%2520Reference%7C_____84 
        var sb = new StringBuilder();
        var xmlWriter = XmlWriter.Create(sb, XmlHelper.XmlSettingsForWebRequests);
        xmlWriter.WriteStartElement("tsRequest");
        xmlWriter.WriteStartElement("workbook");
            xmlWriter.WriteStartElement("owner");
               xmlWriter.WriteAttributeString("id", newOwnerId);  
            xmlWriter.WriteEndElement();//</owner>
        xmlWriter.WriteEndElement();//</workbook>
        xmlWriter.WriteEndElement(); // </tsRequest>
        xmlWriter.Close();

        var xmlText = sb.ToString(); //Get the XML text out

        //Create a web request 
        var urlUpdateWorkbook = _onlineUrls.Url_UpdateWorkbook(_onlineSession, workbookId);
        var webRequest = this.CreateLoggedInWebRequest(urlUpdateWorkbook, "PUT");
        TableauServerRequestBase.SendPutContents(webRequest, xmlText);
        
        //Get the response
        var response = GetWebReponseLogErrors(webRequest, "update workbook (change owner)");
        using (response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            
            //Get all the workbook nodes
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var xNodeWb = xmlDoc.SelectSingleNode("//iwsOnline:workbook", nsManager);

            try
            {
                return new SiteWorkbook(xNodeWb);
            }
            catch (Exception parseXml)
            {
                StatusLog.AddError("Change workbook owner, error parsing XML response " + parseXml.Message + "\r\n" + xNodeWb.InnerXml);
                return null;
            }
            
        }
    }


}
