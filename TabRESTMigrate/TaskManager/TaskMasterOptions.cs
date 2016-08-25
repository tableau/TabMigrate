using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

/// <summary>
/// The set of options and parameters that gets passed into the task manager
/// </summary>
public partial class TaskMasterOptions
{
    private Dictionary<string, string> _optionMapper = new Dictionary<string, string>();
    //True of the option is specified
    public bool IsOptionSet(string optionName)
    {
        string optionValue;
        bool exists = _optionMapper.TryGetValue(optionName, out optionValue);
        return exists;
    }

    /// <summary>
    /// Get the value of an option
    /// </summary>
    /// <param name="optionName"></param>
    /// <returns>
    /// NULL if note specified
    /// </returns>
    public string GetOptionValue(string optionName)
    {
        string optionValue;
        bool exists = _optionMapper.TryGetValue(optionName, out optionValue);
        if (!exists) return null;
        return optionValue;
    }
    /// <summary>
    /// Adds a dictionary value
    /// </summary>
    /// <param name="optionName"></param>
    public void AddOption(string optionName, string optionValue = "")
    {
        _optionMapper.Add(optionName, optionValue);
    }
}
