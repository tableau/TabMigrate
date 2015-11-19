using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Management class for customer actions 
/// </summary>
class CsvDataGenerator
{
    List<string> _knownKeys = new List<string>();
    List<CsvRowValuePairs> _customerActions = new List<CsvRowValuePairs>();

    public int Count
    {
        get
        {
            return _customerActions.Count;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public CsvDataGenerator()
    {
    }

    /// <summary>
    /// Takes in an array of string of the format
    ///  "key:value" and parse them into keys/values arrays
    /// </summary>
    /// <param name="parseLines"></param>
    public void AddKeyValuePairs(string[] parseLines)
    {
        var keys = new string [parseLines.Length];
        var values = new string[parseLines.Length];
        for (int idx = 0; idx < parseLines.Length; idx++)
        {
            string thisLine = parseLines[idx];
            int idxFindSplitter = thisLine.IndexOf(":");
            Debug.Assert(idxFindSplitter > 0, ": must be there, and must be at least 2nd char");
            string thisKey = thisLine.Substring(0, idxFindSplitter);
            string thisValue = thisLine.Substring(idxFindSplitter + 1);

            keys[idx] = thisKey.Trim().ToLower();
            values[idx] = thisValue.Trim();
        }

        AddKeyValuePairs(keys, values);
    }
    /// <summary>
    /// Add the customer action
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="values"></param>
    public void AddKeyValuePairs(string [] keys, string [] values)
    {
        //Normalize the key names, and make sure they are in our existing list of keys
        for(int idxKey = 0; idxKey < keys.Length; idxKey++)
        {
            //Cannonicalize it
            var thisKey = keys[idxKey];
            thisKey = thisKey.Trim().ToLower();
            keys[idxKey] = thisKey; 

            //Add it to the list of keys
            if(!_knownKeys.Contains(thisKey))
            {
                _knownKeys.Add(thisKey);
            }
        }

        //Add the customer action data to the list of customer actions
        _customerActions.Add(new CsvRowValuePairs(keys, values));
    }

    /// <summary>
    /// Generates the text of the CSV file that has columns/rows for all the actions loggs
    /// </summary>
    /// <param name="addRowColumn"></param>
    /// <returns></returns>
    public string GenerateCSVText(bool addRowIdColumn)
    {
        var sb = new StringBuilder();
        //--------------------------------------------------
        //Header row - list all the column headers
        //--------------------------------------------------
        var headerNotFirstItem = new SimpleLatch();
        //If we want to count rows (for ordering), then add the column
        if (addRowIdColumn)
        {
            AppendCSVValue(sb, "row-id", headerNotFirstItem);
        }
        foreach(var keyName in _knownKeys)
        {
            AppendCSVValue(sb, keyName, headerNotFirstItem);
        }
        EndCSVLine(sb);

        //For each content row, look up the values for each column
        int idxRowCount = 1;
        foreach(var row in _customerActions)
        {
            var notFirstItem = new SimpleLatch();
            //Include row count?
            if (addRowIdColumn)
            {
                AppendCSVValue(sb, idxRowCount.ToString(), notFirstItem);
            }
            //Add each of the column values, in the same order as the header row
            foreach (var columnName in _knownKeys)
            {
                AppendCSVValue(sb, row.GetColumnValue(columnName), notFirstItem);
            }
            EndCSVLine(sb);
            idxRowCount++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Ends a CSV item
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="isFirstItem"></param>
//    private static void EndCSVLine(StringBuilder sb, ref bool isFirstItem)
    private static void EndCSVLine(StringBuilder sb)
    {
        sb.AppendLine();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="appendValue"></param>
    /// <param name="isFirstItem"></param>
    private static void AppendCSVValue(StringBuilder sb, string appendValue, SimpleLatch notFirstItem)
    {
        //Normalize the input
        if (appendValue == null)
        {
            appendValue = "";
        }

        //Add a preceeding comma if we are not the first item
        if (notFirstItem.Value)
        {
            sb.Append(",");
        }
        var escapedValue = appendValue;

        var needsQuoting = new SimpleLatch();
        escapedValue = ReplaceIfExists(escapedValue, "\"", "\"\"", needsQuoting); //Replace any single " with ""  (CSV convention)

        //escapedValue = escapedValue.Replace("\"", "\"\""); //Replace any single " with ""  (CSV convention)
        escapedValue = escapedValue.Replace("\n", " "); //Remove newlines
        escapedValue = escapedValue.Replace("\r", " "); //Remove carrage returns

        //If it has a comma it needs to be quoted
        if(escapedValue.Contains(","))
        {
            needsQuoting.Trigger();
        }
        //bool containsComma = escapedValue.Contains(",");

        if (needsQuoting.Value) { sb.Append("\""); } //Start quote
        sb.Append(escapedValue);
        if (needsQuoting.Value) { sb.Append("\""); } //End quote
        notFirstItem.Trigger(); //No longer the first item
    }

    /// <summary>
    /// Replace any 'find' with 'replace'.  Trigger the latch if a replace occured
    /// </summary>
    /// <param name="text"></param>
    /// <param name="find"></param>
    /// <param name="replace"></param>
    /// <param name="triggerIfFound"></param>
    /// <returns></returns>
    private static string ReplaceIfExists(string text, string find, string replace, SimpleLatch triggerIfFound)
    {
        if (text == null) return "";
        //
        if(text.IndexOf(find) == -1)
        {
            return text;
        }

        //If there's text to replace, then replace it
        string outText = text.Replace(find, replace);

        //If the text changed, trigger the latch
        if (outText != text)
        {
            //Set the latch
            triggerIfFound.Trigger();
        }

        return outText;
    }

    /// <summary>
    /// Generates a CSV file
    /// </summary>
    /// <param name="filePath"></param>
    internal void GenerateCSVFile(string filePath)
    {
        string csvContents = GenerateCSVText(true);
        System.IO.File.WriteAllText(filePath, csvContents, Encoding.UTF8);
    }
}
 