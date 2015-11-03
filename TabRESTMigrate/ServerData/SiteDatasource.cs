using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Datasource in a Server's site
/// </summary>
class SiteDatasource : SiteDocumentBase
{
    /// <summary>
    /// The underlying source of the data (e.g. SQL Server? MySQL? Excel? CSV?)
    /// </summary>
    public readonly string Type;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

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
}
