using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Site in Server
/// </summary>
class SiteinfoSite
{
    public readonly string Id;
    public readonly string Name;
    public readonly string ContentUrl;
    public readonly string AdminMode;
    public readonly string State;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    public SiteinfoSite(XmlNode content)
    {
        if(content.Name.ToLower() != "site")
        {
            AppDiagnostics.Assert(false, "Not a site");
            throw new Exception("Unexpected content - not site");
        }

        this.Name = content.Attributes["name"].Value;
        this.Id = content.Attributes["id"].Value;
        this.ContentUrl = content.Attributes["contentUrl"].Value;
        this.AdminMode = content.Attributes["adminMode"].Value;
        this.State = content.Attributes["state"].Value;
    }
}
