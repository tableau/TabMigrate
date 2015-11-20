using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Project in a Server's site
/// </summary>
class SiteProject : IHasSiteItemId
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="projectNode"></param>
    public SiteProject(XmlNode projectNode)
    {
        var sbDevNotes = new StringBuilder();

        if(projectNode.Name.ToLower() != "project")
        {
            AppDiagnostics.Assert(false, "Not a project");
            throw new Exception("Unexpected content - not project");
        }

        this.Id = projectNode.Attributes["id"].Value;
        this.Name = projectNode.Attributes["name"].Value;

        var descriptionNode = projectNode.Attributes["description"];
        if(descriptionNode != null)
        {
            this.Description = descriptionNode.Value;
        }
        else
        {
            this.Description = "";
            sbDevNotes.AppendLine("Project is missing description attribute");
        }

        this.DeveloperNotes = sbDevNotes.ToString();
    }

    public SiteProject(string name, string Id)
    {
        this.Name = name;
        this.Id = Id;
    }

    public override string ToString()
    {
        return "Project: " + this.Name + "/" + this.Id;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
