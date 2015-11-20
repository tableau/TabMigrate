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
    public void AddItem(string key, T item)
    {
        _dictionary.Add(key, item);
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
    public KeyedLookup(IEnumerable<T> items)
    {
        foreach(var thisItem in items)
        {
            AddItem(thisItem.Id, thisItem);
        }
    }
}
