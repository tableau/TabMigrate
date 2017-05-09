using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a View in a Server's site
/// </summary>
class SiteView : IHasSiteItemId
{
    public readonly string Id;
    public readonly string OwnerId;
    public readonly string Name;
    public readonly string ContentUrl;
    public readonly string WorkbookId;
    public readonly Int64 TotalViewCount;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewNode"></param>
    public SiteView(XmlNode viewNode)
    {
        var sbDevNotes = new StringBuilder();

        if(viewNode.Name.ToLower() != "view")
        {
            AppDiagnostics.Assert(false, "Not a view");
            throw new Exception("Unexpected content - not view");
        }

        this.Id = viewNode.Attributes["id"].Value;
        this.Name = viewNode.Attributes["name"].Value;
        this.ContentUrl = viewNode.Attributes["contentUrl"].Value;

        //Namespace for XPath queries
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");

        //Get the user attributes
        var ownerNode = viewNode.SelectSingleNode("iwsOnline:owner", nsManager);
        this.OwnerId = ownerNode.Attributes["id"].Value;

        //Get information about the content being subscribed to (workbook or view)
        var workbookNode  = viewNode.SelectSingleNode("iwsOnline:workbook", nsManager);
        this.WorkbookId   = workbookNode.Attributes["id"].Value;

        //Get the schedule attibutes
        var usageNode = viewNode.SelectSingleNode("iwsOnline:usage", nsManager);
        this.TotalViewCount = System.Convert.ToInt64(usageNode.Attributes["totalViewCount"].Value);

        this.DeveloperNotes = sbDevNotes.ToString();
    }

    public override string ToString()
    {
        return "View: " + this.Id + "/" + this.Name;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
