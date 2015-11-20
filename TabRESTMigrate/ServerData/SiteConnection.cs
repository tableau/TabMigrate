using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Data connection that is embedded in a Workbook or Data Source
/// </summary>
class SiteConnection : IHasSiteItemId
{
    public readonly string Id;
    public readonly string ConnectionType;
    public readonly string ServerAddress;
    public readonly string ServerPort;
    public readonly string UserName;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="projectNode"></param>
    public SiteConnection(XmlNode projectNode)
    {
        var sbDevNotes = new StringBuilder();

        if(projectNode.Name.ToLower() != "connection")
        {
            AppDiagnostics.Assert(false, "Not a connection");
            throw new Exception("Unexpected content - not connection");
        }

        this.Id = projectNode.Attributes["id"].Value;
        this.ConnectionType = projectNode.Attributes["type"].Value;

        this.ServerAddress = XmlHelper.SafeParseXmlAttribute(projectNode, "serverAddress", "");
        this.ServerPort = XmlHelper.SafeParseXmlAttribute(projectNode, "serverPort", "");
        this.UserName = XmlHelper.SafeParseXmlAttribute(projectNode, "userName", "");

        this.DeveloperNotes = sbDevNotes.ToString();
    }


    public override string ToString()
    {
        return "Connection: " + this.ConnectionType + "/" + this.ServerAddress + "/" + this.Id;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
