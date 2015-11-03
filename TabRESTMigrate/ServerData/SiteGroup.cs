using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Grou[ in a Server's site
/// </summary>
class SiteGroup
{
    public readonly string Id;
    public readonly string Name;
    List<SiteUser> _usersInGroup;
//    public readonly string DomainName;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Returns the list of users associated with this group
    /// </summary>
    public ICollection<SiteUser> Users
    {
        get
        {
            return _usersInGroup.AsReadOnly();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="projectNode"></param>
    public SiteGroup(XmlNode projectNode, IEnumerable<SiteUser> usersToPlaceInGroup )
    {
        //If we were passed in a set of users, store them
        var usersList = new List<SiteUser>();
        if(usersToPlaceInGroup != null)
        {
            usersList.AddRange(usersToPlaceInGroup);
        }
        _usersInGroup = usersList;


        if(projectNode.Name.ToLower() != "group")
        {
            AppDiagnostics.Assert(false, "Not a group");
            throw new Exception("Unexpected content - not group");
        }

        this.Id = projectNode.Attributes["id"].Value;
        this.Name = projectNode.Attributes["name"].Value;
//        this.DomainName = projectNode.Attributes["description"].Value;
    }


    public override string ToString()
    {
        return "Group: " + this.Name + "/" + this.Id;
    }

    /// <summary>
    /// Adds a set of users.  This is typically called when initializing this object.
    /// </summary>
    /// <param name="usersList"></param>
    internal void AddUsers(IEnumerable<SiteUser> usersList)
    {
        //Nothing to add?
        if (usersList == null)
        {
            return;
        }

        _usersInGroup.AddRange(usersList);
    }
}
