using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

class SendCreateProject : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    private readonly string _projectName;
    private readonly string _projectDesciption = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="projectName"></param>
    public SendCreateProject(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string projectName)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _projectName = projectName;
    }

    /// <summary>
    /// Create a project on server
    /// </summary>
    /// <param name="serverName"></param>
    public SiteProject ExecuteRequest()
    {
        try
        {
            var newProj = CreateProject(_projectName, _projectDesciption);
            this.StatusLog.AddStatus("Project created. " + newProj.ToString());
            return newProj;
        }
        catch (Exception exProject)
        {
            this.StatusLog.AddError("Error attempting to create project '" + _projectName + "', " + exProject.Message);
            return null;
        }
    }


    private SiteProject CreateProject(string projectName, string projectDescription)
    {
        //ref: http://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Create_Project%3FTocPath%3DAPI%2520Reference%7C_____12  
        var sb = new StringBuilder();
        var xmlWriter = XmlWriter.Create(sb, XmlHelper.XmlSettingsForWebRequests);
        xmlWriter.WriteStartElement("tsRequest");
        xmlWriter.WriteStartElement("project");
        xmlWriter.WriteAttributeString("name", projectName);
        xmlWriter.WriteAttributeString("description", projectDescription);
        xmlWriter.WriteEndElement();//</project>
        xmlWriter.WriteEndElement(); // </tsRequest>
        xmlWriter.Close();

        var xmlText = sb.ToString(); //Get the XML text out

        //Generate the MIME message
        //var mimeGenerator = new OnlineMimeXmlPayload(xmlText);

        //Create a web request 
        var urlCreateProject = _onlineUrls.Url_CreateProject(_onlineSession);
        var webRequest = this.CreateLoggedInWebRequest(urlCreateProject, "POST");
        TableauServerRequestBase.SendPostContents(webRequest, xmlText);
        
        //Get the response
        var response = GetWebReponseLogErrors(webRequest, "create project");
        using (response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            
            //Get all the workbook nodes
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var xNodeProject = xmlDoc.SelectSingleNode("//iwsOnline:project", nsManager);

            try
            {
                return new SiteProject(xNodeProject);
            }
            catch (Exception parseXml)
            {
                StatusLog.AddError("Create project, error parsing XML response " + parseXml.Message + "\r\n" + xNodeProject.InnerXml);
                return null;
            }
            
        }
    }


}
