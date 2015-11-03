using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Generic threadsafe class for maintaining a log of items
/// </summary>
class Logger
{
    /// <summary>
    /// Stores an item of status
    /// </summary>
    private class StatusItem
    {
        public readonly string Text;
        public readonly DateTime When;

        public StatusItem(string text)
        {
            this.When = DateTime.Now;
            this.Text = text;
        }

        public override string ToString()
        {
            return this.When.ToString() + ", " + this.Text;
        }
    }

    //Thread lock
    private object _lockStatus = new object();

    List<StatusItem> _status = new List<StatusItem>();
    public string StatusText
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            lock (_lockStatus)
            {
                //No status?
                var statusList = _status;
                if (statusList.Count == 0)
                {
                    return "";
                }

                var previousWhen = _status[0].When;

                //Go in reverse order to show the last first
                for (int idx = 0; idx < statusList.Count; idx++)
                {
                    var thisItem = statusList[idx];
                    //For readability, add a blank line if the current time differs from the previous time by more than a defined-delta
                    var timeDeltaSeconds = Math.Abs((previousWhen - thisItem.When).TotalSeconds);
                    if(timeDeltaSeconds > 15)
                    {
                        sb.AppendLine();
                    }

                    //Append the line
                    sb.AppendLine(idx.ToString("000") + ",  " + statusList[idx]);
                    //Advance the date counter
                    previousWhen = thisItem.When;
                }
            }

            return sb.ToString();
        }
    }

    public int Count
    {
        get
        {
            return _status.Count;
        }
    }


    /// <summary>
    /// Add an item to the status list
    /// </summary>
    /// <param name="statusText"></param>
    public void AddStatus(string statusText)
    {
        var statusItem = new StatusItem(statusText);
        //string textWithTime = DateTime.Now.ToString() + ": " + statusText;
        lock (_lockStatus)
        {
            _status.Add(statusItem);
        }
    }

}
