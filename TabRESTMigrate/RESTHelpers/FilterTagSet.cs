using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Filtering methods for sorting content based on tags
/// </summary>
/// <typeparam name="T"></typeparam>
class FilterTagSet<T> where T : ITagSetInfo
{
    /// <summary>
    /// Keeps only the members of the set that have a matching project id
    /// </summary>
    /// <param name="items"></param>
    /// <param name="tagText"></param>
    /// <param name="nullMeansNoFilter">TRUE: Blank filter criteria means return all. FALSE: Blank filter criteria means return none</param>
    /// <returns></returns>
    public static ICollection<T> KeepOnlyTagged(ICollection<T> items, string tagText, bool nullMeansNoFilter)
    {
        //See if a blank filter implies we should return the full set
        if((nullMeansNoFilter) && (string.IsNullOrWhiteSpace(tagText)))
        {
            return items;
        }

        var listOut = new List<T>();
        foreach (var thisItem in items)
        {
            if(thisItem.IsTaggedWith(tagText))
            {
                listOut.Add(thisItem);
            }
        }

        return listOut;
    }
}
