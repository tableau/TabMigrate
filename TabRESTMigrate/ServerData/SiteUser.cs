using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a User in a Server's site
/// </summary>
partial class SiteUser : IHasSiteItemId
{
    public readonly string Name;
    public readonly string Id;
    public readonly string SiteRole;
    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userNode"></param>
    public SiteUser(XmlNode userNode)
    {
        if (userNode.Name.ToLower() != "user")
        {
            AppDiagnostics.Assert(false, "Not a user");
            throw new Exception("Unexpected content - not user");
        }

        this.Id = userNode.Attributes["id"].Value;
        this.Name = userNode.Attributes["name"].Value;
        this.SiteRole = userNode.Attributes["siteRole"].Value;
    }

    public override string ToString()
    {
        return "User: " + this.Name + "/" + this.Id + "/" + this.SiteRole;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
