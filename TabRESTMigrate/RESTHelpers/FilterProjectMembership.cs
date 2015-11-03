using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Filtering methods for sorting content into projects that own them
/// </summary>
/// <typeparam name="T"></typeparam>
class FilterProjectMembership<T> where T : IHasProjectId
{
    /// <summary>
    /// Keeps only the members of the set that have a matching project id
    /// </summary>
    /// <param name="items"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static ICollection<T> KeepOnlyProjectMembers(ICollection<T> items, SiteProject project, bool nullMeansNoFilter)
    {
        //See if a blank filter implies we should return the full set
        if((nullMeansNoFilter) && (project == null))
        {
            return items;
        }

        var projectId = project.Id;
        var listOut = new List<T>();
        foreach (var thisItem in items)
        {
            if(thisItem.ProjectId == projectId)
            {
                listOut.Add(thisItem);
            }
        }

        return listOut;
    }
}
