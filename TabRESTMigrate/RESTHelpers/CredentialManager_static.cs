using System;
using System.IO;
using System.Xml;

partial class CredentialManager
{

    /// <summary>
    /// Load the DB credentials set from a file
    /// </summary>
    /// <param name="pathDBCredentials"></param>
    /// <returns></returns>
    internal static CredentialManager LoadFromFile(string pathDBCredentials, TaskStatusLogs statusLog)
    {
        if(statusLog == null) 
        {
            statusLog = new TaskStatusLogs();
        }
        statusLog.AddStatus("Loading database credentials from " + pathDBCredentials);

        //Load the XML document and get the credentials
        var xDoc = new XmlDocument();
        xDoc.Load(pathDBCredentials);
        var nodesList =  xDoc.SelectNodes("//credential");

        var credentialManager = new CredentialManager();
        foreach (XmlNode credentialNode in nodesList)
        {
            try
            {
                helper_parseCredentialNode(credentialManager, credentialNode);
            }
            catch(Exception ex)
            {
                statusLog.AddError("Error parsing credential, " + ex.Message + ", " + credentialNode.OuterXml);
            }
        }

        return credentialManager;
    }


    /// <summary>
    /// Parse the credential and add it to the credential manager
    /// </summary>
    /// <param name="credentialManager"></param>
    /// <param name="credentialNode"></param>
    private static void helper_parseCredentialNode(CredentialManager credentialManager, XmlNode credentialNode)
    {
        var contentType =  XmlHelper.ReadTextAttribute(credentialNode, "contentType", "");
        var contentProjectName = XmlHelper.ReadTextAttribute(credentialNode, "contentProjectName");
        var contentName = XmlHelper.ReadTextAttribute(credentialNode, "contentName"); 

        var dbUserName = XmlHelper.ReadTextAttribute(credentialNode, "dbUser");
        var dbPassword = XmlHelper.ReadTextAttribute(credentialNode, "dbPassword");
        var isEmbedded = XmlHelper.ReadBooleanAttribute(credentialNode, "credentialIsEmbedded", false);

        //Sanity checking
        if(string.IsNullOrWhiteSpace(contentName))
        {
            throw new Exception("Credential is missing content name");
        }

        if(contentType == "workbook")
        {
            credentialManager.AddWorkbookCredential(contentName, contentProjectName, dbUserName, dbPassword, isEmbedded);
        }
        else if(contentType == "datasource")
        {
            credentialManager.AddDatasourceCredential(contentName, contentProjectName, dbUserName, dbPassword, isEmbedded);
        }
        else
        {
            throw new Exception("Unknown credential content type: " + contentType);
        }
    }
}
