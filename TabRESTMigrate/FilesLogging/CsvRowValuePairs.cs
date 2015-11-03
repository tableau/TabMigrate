using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Data that is going into a CSV file as a set of column/value pairs
/// </summary>
class CsvRowValuePairs
{
    readonly Dictionary<string, string> _keyValuePairs = new Dictionary<string,string>();
  
    /// <summary>
    /// Called to add a set of key-values
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="values"></param>
    public CsvRowValuePairs(string [] keys, string [] values)
    {
        //Add them as key value pairs
        System.Diagnostics.Debug.Assert(keys.Length == values.Length, "Mismatch in keys/values");
        for(int idx = 0; idx < keys.Length; idx++)
        {
            _keyValuePairs.Add(keys[idx], values[idx]);
        }
    }

    /// <summary>
    /// If the dictionary contains the columnm return the value. Otherwise return ""
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    internal string GetColumnValue(string columnName)
    {
        if (_keyValuePairs.ContainsKey(columnName))
        {
            return _keyValuePairs[columnName];
        }
        return "";
    }
}
