using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

internal partial class WorkbookPublishSettings
{
    /// <summary>
    /// TRUE: The published workbook should show tabs to navigate the sheets/dashboards/stories
    /// FALSE: No Tabs (each sheet/dashboard/story is accessed by its URL)
    /// </summary>
    public readonly bool ShowTabs = false;


    /// <summary>
    /// The name of the owner of the content (NULL if unknown)
    /// </summary>
    public readonly string OwnerName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="showTabs"></param>
    /// <param name="ownerName"></param>
    public WorkbookPublishSettings(bool showTabs, string ownerName)
    {
        this.ShowTabs = showTabs;
        this.OwnerName = ownerName;
    }
}
