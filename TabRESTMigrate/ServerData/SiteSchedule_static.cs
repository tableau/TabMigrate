using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Information about a Subscription in a Server's site
/// </summary>
partial class SiteSchedule: IHasSiteItemId
{

    /// <summary>
    /// Returns the subset of the list of schedules that are site schedules
    /// </summary>
    /// <param name="schedules"></param>
    /// <returns></returns>
    internal static IEnumerable<SiteSchedule> FilterListToExtractSchedules(IEnumerable<SiteSchedule> schedules)
    {
        var extractSchedules = new List<SiteSchedule>();
        
        //Go through each schedule and determine if it's an extract refresh schedule
        foreach(var thisSchedule in schedules)
        {
            if(thisSchedule.IsExtractRefreshSchedule)
            {
                extractSchedules.Add(thisSchedule);
            }
        }
        return extractSchedules;
    }
}
