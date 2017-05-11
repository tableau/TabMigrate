using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
/// <summary>
/// Efficent lookup for unevenly distributed sets of data
/// </summary>
class KeyedLookup<T> where T : IHasSiteItemId
{
    Dictionary<string, T> _dictionary = new Dictionary<string, T>();
    /// <summary>
    /// Adds a keyed item to the dictionary
    /// </summary>
    /// <param name="key"></param>
    /// <param name="item"></param>
    /// <param name="statusLogger">If non-NULL; then trap and record errors,  If NULL the error will get thrown upward</param>
    public void AddItem(string key, T item, TaskStatusLogs statusLogger = null)
    {
        //There are cases where building the dictionary may fail, such as if the incoming data has 
        //duplicate ID entries.  If we have a status logger, we want to log the error and then
        //continue onward
        try
        {
            _dictionary.Add(key, item);
        }
        catch (Exception exAddDictionaryItem)
        {
            //If we have an error logger, then log the error
            if (statusLogger != null)
            {
                string itemDescription = "null item";
                if(item != null)
                {
                    itemDescription = item.ToString();
                }
                statusLogger.AddError("Error building lookup dictionary. Item: " + itemDescription + ", " + exAddDictionaryItem.ToString());
            }
            else //Otherwise thrown the error upward
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Look up an item by key, return NULL if not found
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T FindItem(string key)
    {
        T outItem;
        bool found = _dictionary.TryGetValue(key, out outItem);
        if(!found)
        {
            return default(T);
        }
        return outItem;
    }


    /// <summary>
    /// Add the whole set of items
    /// </summary>
    /// <param name="items"></param>
    public KeyedLookup(IEnumerable<T> items, TaskStatusLogs statusLogger)
    {
        foreach(var thisItem in items)
        {
            AddItem(thisItem.Id, thisItem, statusLogger);
        }
    }
}
