using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Records errors during runs
/// </summary>
class TaskStatusLogs
{
    Logger _statusLog = new Logger();
    Logger _errorLog = new Logger();
    private int _minimumStatusLevel = 0;

    public void SetStatusLoggingLevel(int statusLevel)
    {
        _minimumStatusLevel = statusLevel;
    }

    public string StatusText
    {
        get
        {
            return _statusLog.StatusText;
        }
    }

    /// <summary>
    /// Add a header/splitter line
    /// </summary>
    public void AddStatusHeader(string headerText, int statusLevel = 0)
    {
        AddStatus("****************************************************************", statusLevel);
        AddStatus(headerText, statusLevel);
        AddStatus("****************************************************************", statusLevel);
    }

    public void AddStatus(string statusText, int statusLevel = 0)
    {
        if(statusLevel >= _minimumStatusLevel)
        {
            //Indent the lower status items
            string prefixText = "";
            if (statusLevel < 0) { prefixText = "     "; }

            _statusLog.AddStatus(prefixText + statusText);
        }
    }

    public int ErrorCount
    {
        get
        {
            return _errorLog.Count;
        }
    }
    public string ErrorText
    {
        get
        {
            return _errorLog.StatusText;
        }
    }

    public void AddError(string errorText)
    {
        _statusLog.AddStatus("Error: " + errorText);
        _errorLog.AddStatus(errorText);
    }
}
