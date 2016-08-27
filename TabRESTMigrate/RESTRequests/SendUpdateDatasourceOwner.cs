using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// Attempts to update the owner of a datasource on server
/// </summary>
class SendUpdateDatasourceOwner: TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    private readonly string _newOwnerId;
    private readonly string _datasourceId;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="datasourceId">GUID</param>
    /// <param name="newOwnerId">GUID</param>
    public SendUpdateDatasourceOwner(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string datasourceId,
        string newOwnerId)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _datasourceId = datasourceId;
        _newOwnerId = newOwnerId;
    }

    /// <summary>
    /// Change the owner of a datasource on server
    /// </summary>
    /// <param name="serverName"></param>
    public SiteDatasource ExecuteRequest()
    {
        try
        {
            var ds = ChangeContentOwner(_datasourceId, _newOwnerId);
            this.StatusLog.AddStatus("Datasource ownership changed. ds:" + ds.Name + "/" + ds.Id +  ", new owner:" + ds.OwnerId);
            return ds;
        }
        catch (Exception exError)
        {
            this.StatusLog.AddError("Error attempting to change the datasource '" + _datasourceId + "' owner to '" + _newOwnerId + "', " + exError.Message);
            return null;
        }
    }


    private SiteDatasource ChangeContentOwner(string datasourceId, string newOwnerId)
    {
        AppDiagnostics.Assert(!string.IsNullOrWhiteSpace(datasourceId), "missing datasource id");
        AppDiagnostics.Assert(!string.IsNullOrWhiteSpace(newOwnerId), "missing owner id");

        //ref: https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Update_Datasource%3FTocPath%3DAPI%2520Reference%7C_____76
        var sb = new StringBuilder();
        var xmlWriter = XmlWriter.Create(sb, XmlHelper.XmlSettingsForWebRequests);
        xmlWriter.WriteStartElement("tsRequest");
        xmlWriter.WriteStartElement("datasource");
            xmlWriter.WriteStartElement("owner");
               xmlWriter.WriteAttributeString("id", newOwnerId);  
            xmlWriter.WriteEndElement();//</owner>
        xmlWriter.WriteEndElement();//</datasource>
        xmlWriter.WriteEndElement(); // </tsRequest>
        xmlWriter.Close();

        var xmlText = sb.ToString(); //Get the XML text out

        //Create a web request 
        var urlUpdateDatasource = _onlineUrls.Url_UpdateDatasource(_onlineSession, datasourceId);
        var webRequest = this.CreateLoggedInWebRequest(urlUpdateDatasource, "PUT");
        TableauServerRequestBase.SendPutContents(webRequest, xmlText);
        
        //Get the response
        var response = GetWebReponseLogErrors(webRequest, "update datasource (change owner)");
        using (response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            
            //Get all the datasource nodes
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var xNodeDs = xmlDoc.SelectSingleNode("//iwsOnline:datasource", nsManager);

            try
            {
                return new SiteDatasource(xNodeDs);
            }
            catch (Exception parseXml)
            {
                StatusLog.AddError("Change datasource owner, error parsing XML response " + parseXml.Message + "\r\n" + xNodeDs.InnerXml);
                return null;
            }
            
        }
    }


}
