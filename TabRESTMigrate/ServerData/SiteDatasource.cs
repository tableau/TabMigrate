using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Datasource in a Server's site
/// </summary>
class SiteDatasource : SiteDocumentBase, IEditDataConnectionsSet
{
    /// <summary>
    /// The underlying source of the data (e.g. SQL Server? MySQL? Excel? CSV?)
    /// </summary>
    public readonly string Type;

    /// <summary>
    /// If set, contains the set of data connections embedded in this workbooks
    /// </summary>
    private List<SiteConnection> _dataConnections;

    /// <summary>
    /// Return a set of data connections (if they were downloaded)
    /// </summary>
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
    /// <param name="datasourceNode"></param>
    public SiteDatasource(XmlNode datasourceNode) : base(datasourceNode)
    {
        if(datasourceNode.Name.ToLower() != "datasource")
        {
            AppDiagnostics.Assert(false, "Not a datasource");
            throw new Exception("Unexpected content - not datasource");
        }
        //Get the underlying data source type
        this.Type = datasourceNode.Attributes["type"].Value;

    }

    /// <summary>
    /// Text description
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return "Datasource: " + this.Name + "/" + this.Type + "/" + this.Id;
    }

    /// <summary>
    /// Interface for inserting the set of data connections associated with this content
    /// </summary>
    /// <param name="connections"></param>
    void IEditDataConnectionsSet.SetDataConnections(IEnumerable<SiteConnection> connections)
    {
        if (connections == null)
        {
            _dataConnections = null;
        }
        _dataConnections = new List<SiteConnection>(connections);
    }
}
