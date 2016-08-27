using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

partial class SiteUser : IHasSiteItemId
{
    /// <summary>
    /// Look through a set of users for a user with a specific name
    /// </summary>
    /// <param name="siteUsers"></param>
    /// <param name="findName"></param>
    /// <param name="compareMode"></param>
    /// <returns> NULL = No matching user found.  Otherwise returns the user with the matching name
    /// </returns>
    public static SiteUser FindUserWithName(IEnumerable<SiteUser> siteUsers, string findName, StringComparison compareMode = StringComparison.InvariantCultureIgnoreCase)
    {
        foreach(var thisUser in siteUsers)
        {
            //If its a match return the user
            if(string.Compare(thisUser.Name, findName, compareMode) == 0)
            {
                return thisUser;
            }
        }

        return null; //no found
    }
}
