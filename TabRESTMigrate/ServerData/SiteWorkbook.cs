using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Workbook in a Server's site
/// </summary>
class SiteWorkbook : SiteDocumentBase, IEditDataConnectionsSet
{
    public readonly bool ShowTabs;
    //Note: [2015-10-28] Datasources presently don't return this information, so we need to make this workbook specific
    public readonly string ContentUrl;


    /// <summary>
    /// If set, contains the set of data connections embedded in this workbooks
    /// </summary>
    private List<SiteConnection> _dataConnections;

    public ReadOnlyCollection<SiteConnection> DataConnections
    {
        get
        {
            var dataConnections = _dataConnections;
            if (dataConnections == null) return null;

            return dataConnections.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="workbookNode"></param>
    public SiteWorkbook(XmlNode workbookNode) : base(workbookNode)
    {
        if(workbookNode.Name.ToLower() != "workbook")
        {
            AppDiagnostics.Assert(false, "Not a workbook");
            throw new Exception("Unexpected content - not workbook");
        }

        //Note: [2015-10-28] Datasources presently don't return this information, so we need to make this workbook specific
        this.ContentUrl = workbookNode.Attributes["contentUrl"].Value;

        //Do we have tabs?
        this.ShowTabs = XmlHelper.SafeParseXmlAttribute_Bool(workbookNode, "showTabs", false);
    }


    public override string ToString()
    {
        return "Workbook: " + Name + "/" + ContentUrl + "/" + Id + ", Proj: " + ProjectId;
    }


    /// <summary>
    /// Interface for inserting the set of data connections associated with this content
    /// </summary>
    /// <param name="connections"></param>
    void IEditDataConnectionsSet.SetDataConnections(IEnumerable<SiteConnection> connections)
    {
        if(connections == null)
        {
            _dataConnections = null;
        }
        _dataConnections = new List<SiteConnection>(connections);

    }
}
