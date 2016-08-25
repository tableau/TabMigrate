using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Xml;

/// <summary>
/// Re-Maps all of the references to published datasources inside a Workbook to point to a different server/site.
/// This transformation is needed to successfully copy a Workbook from one site/server to another site/server.
/// </summary>
class TwbDataSourceEditor
{
    private readonly string _pathToTwbInput;
    private readonly string _pathToTwbOutput;
    private readonly ITableauServerSiteInfo _serverMapInfo;
    private readonly TaskStatusLogs _statusLog;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pathTwbx">TWBX we are going to unpack</param>
    /// <param name="workingDirectory"></param>
    public TwbDataSourceEditor(string pathTwbInput, string pathTwbOutput, ITableauServerSiteInfo serverMapInfo, TaskStatusLogs statusLog)
    {
        _pathToTwbInput = pathTwbInput;
        _pathToTwbOutput = pathTwbOutput;
        _serverMapInfo = serverMapInfo;
        _statusLog = statusLog;
    }


    /// <summary>
    /// Run the file transformation and output the remapped file
    /// </summary>
    public void Execute()
    {
        //Load XML for *.twb file
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(_pathToTwbInput);

        //Look for Data Sources that are 'sqlproxy'
        //Remap server-path to point to correct server/site
        RemapDataServerReferences(xmlDoc, _serverMapInfo, _statusLog);
        //Remap global XML references to the server
        RemapWorkbookGlobalReferences(xmlDoc, _serverMapInfo, _statusLog);

        //Write out the transformed XML document
        TableauPersistFileHelper.WriteTableauXmlFile(xmlDoc, _pathToTwbOutput);
    }

    /// <summary>
    /// Remaps global references in the workbook that refer to the site/server
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="serverMapInfo"></param>
    /// <param name="statusLog"></param>
    private static void RemapWorkbookGlobalReferences(XmlDocument xmlDoc, ITableauServerSiteInfo serverMapInfo, TaskStatusLogs statusLog)
    {
        var xnodeWorkbook = xmlDoc.SelectSingleNode("//workbook");
        if(xnodeWorkbook == null)
        {
            statusLog.AddError("Workbook remapper, 'workbook' node not found");
            return;
        }

        //See if there is an an XML base node
        var attrXmlBase = xnodeWorkbook.Attributes["xml:base"];
        if(attrXmlBase != null)
        {
            attrXmlBase.Value = serverMapInfo.ServerNameWithProtocol;
        }

        //We may also have a repository node
        RemapSingleWorkbooksRepositoryNode(xmlDoc, xnodeWorkbook, serverMapInfo, true, statusLog);
    }


    //Update to: -<connection username="" server="preview-online.tableau.com" port="443" directory="/dataserver" dbname="At-Task60days" class="sqlproxy" channel="https">
    private static void RemapDataServerReferences(XmlDocument xmlDoc, ITableauServerSiteInfo serverMapInfo, TaskStatusLogs statusLog)
    {
        var xDataSources = xmlDoc.SelectNodes("workbook/datasources/datasource");
        foreach (XmlNode xnodeDatasource in xDataSources)
        {
            var xnodeConnection = xnodeDatasource.SelectSingleNode("connection");
            //Not all datasources have connection nodes (e.g. the parameters data source). If there is no connection, there is nothing to do
            if(xnodeConnection != null)
            { 
                string dbClass = xnodeConnection.Attributes["class"].Value;

                //If its 'sqlproxy' then its a data server connection
                if (dbClass == "sqlproxy")
                {
                    //Start remapping....
                    RemapSingleDataServerConnectionNode(xnodeDatasource, serverMapInfo, statusLog);
                    RemapSingleDataServerRepositoryNode(xmlDoc, xnodeDatasource, serverMapInfo, false, statusLog);
                }
            }
        }
    }

    /// <summary>
    /// Remaps the 'repository-location' node of the Data Source XML
    /// </summary>
    /// <param name="xnodeRepository"></param>
    /// <param name="serverMapInfo"></param>
    /// <param name="statusLog"></param>
    private static void RemapSingleDataServerRepositoryNode(XmlDocument xmlDoc, XmlNode xNodeDatasource, ITableauServerSiteInfo serverMapInfo, bool ignoreIfMissing, TaskStatusLogs statusLog)
    {
        var siteId = serverMapInfo.SiteId;

        //Get the XML sub mode we need
        var xnodeRepository = xNodeDatasource.SelectSingleNode("repository-location");
        if (xnodeRepository == null)
        {
            if (ignoreIfMissing) return;

            statusLog.AddError("Workbook remapper, no datasource 'repository-location' node found");
            return;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        helper_SetRespositorySite(xmlDoc, xnodeRepository, serverMapInfo);

        ///////////////////////////////////////////////////////////////////////////////////////
        var attrPath = xnodeRepository.Attributes["path"];
        if (attrPath != null)
        {
            //Is there a site specified
            if(!string.IsNullOrWhiteSpace(siteId))
            {
                attrPath.Value = "/t/" + siteId + "/datasources";
            }
            else //Default site
            {
                attrPath.Value = "/datasources";
            }

        }
        else
        {
            statusLog.AddError("Workbook remapper 'path' attribute not found");
        }

    }

    /// <summary>
    /// Ensures the repository Xml Node has the correct site value
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="xnodeRepository"></param>
    /// <param name="serverMapInfo"></param>
    private static void helper_SetRespositorySite(XmlDocument xmlDoc, XmlNode xnodeRepository, ITableauServerSiteInfo serverMapInfo)
    {
        var attrSite = xnodeRepository.Attributes["site"];

        //If we have NOT site id, then get rid of the attribute if it exists
        var siteId = serverMapInfo.SiteId;
        if(string.IsNullOrWhiteSpace(siteId))
        {
            if(attrSite != null)
            {
                xnodeRepository.Attributes.Remove(attrSite);
            }

            return; //Nothing left to do
        }


        //If the the site attribute is missing, then add it
        if (attrSite == null)
        {
            attrSite = xmlDoc.CreateAttribute("site");
            xnodeRepository.Attributes.Append(attrSite);
        }
        //Set the attribute value
        attrSite.Value = siteId;
    }


    /// <summary>
    /// Remaps the 'repository-location' node of the Data Source XML
    /// </summary>
    /// <param name="xnodeRepository"></param>
    /// <param name="serverMapInfo"></param>
    /// <param name="statusLog"></param>
    private static void RemapSingleWorkbooksRepositoryNode(XmlDocument xmlDoc, XmlNode xNodeDatasource, ITableauServerSiteInfo serverMapInfo, bool ignoreIfMissing, TaskStatusLogs statusLog)
    {
        var siteId = serverMapInfo.SiteId;

        //Get the XML sub mode we need
        var xnodeRepository = xNodeDatasource.SelectSingleNode("repository-location");
        if (xnodeRepository == null)
        {
            if (ignoreIfMissing) return;

            statusLog.AddError("Workbook remapper, no workbook 'repository-location' node found");
            return;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        helper_SetRespositorySite(xmlDoc, xnodeRepository, serverMapInfo);

        ///////////////////////////////////////////////////////////////////////////////////////
        var attrPath = xnodeRepository.Attributes["path"];
        if (attrPath != null)
        {
            //Is there a site specified
            if (!string.IsNullOrWhiteSpace(siteId))
            {
                attrPath.Value = "/t/" + siteId + "/workbooks";
            }
            else //Default site
            {
                attrPath.Value = "/workbooks";
            }
        }
        else
        {
            statusLog.AddError("Workbook remapper 'path' attribute not found");
        }

    }


    /// <summary>
    /// Remaps necesary attributes inside of the datasource->connection node to point to a new server
    /// </summary>
    /// <param name="xDSourceConnection"></param>
    /// <param name="serverMapInfo"></param>
    /// <param name="statusLog"></param>
    private static void RemapSingleDataServerConnectionNode(XmlNode xNodeDatasource, ITableauServerSiteInfo serverMapInfo, TaskStatusLogs statusLog)
    {
        //Get the XML sub mode we need
        var xNodeConnection = xNodeDatasource.SelectSingleNode("connection");
        if(xNodeConnection == null)
        {
            statusLog.AddError("Workbook remapper, no 'connection' node found");
            return;
        }
        //====================================================================================
        //PORT NUMBER
        //====================================================================================
        var attrPort = xNodeConnection.Attributes["port"];
        if(attrPort != null)
        {
            if(serverMapInfo.Protocol == ServerProtocol.http)
            {
                attrPort.Value = "80";
            }
            else if (serverMapInfo.Protocol == ServerProtocol.https)
            {
                attrPort.Value = "443";
            }
            else
            {
                statusLog.AddError("Workbook remapper, unknown protocol");
            }
        }
        else
        {
            statusLog.AddError("Workbook remapper, missing attribute 'port'");
        }

        //====================================================================================
        //Server name
        //====================================================================================
        var attrServer = xNodeConnection.Attributes["server"];
        if (attrServer != null)
        {
            attrServer.Value = serverMapInfo.ServerName;
        }
        else
        {
            statusLog.AddError("Workbook remapper, missing attribute 'server'");
        }

        //====================================================================================
        //Channel
        //====================================================================================
        var attrChannel = xNodeConnection.Attributes["channel"];
        if (attrChannel != null)
        {
            if (serverMapInfo.Protocol == ServerProtocol.http)
            {
                attrChannel.Value = "http";
            }
            else if (serverMapInfo.Protocol == ServerProtocol.https)
            {
                attrChannel.Value = "https";
            }
            else
            {
                statusLog.AddError("Workbook remapper, unknown protocol");
            }

        }
        else
        {
            statusLog.AddError("Workbook remapper, missing attribute 'channel'");
        }

    }

}
