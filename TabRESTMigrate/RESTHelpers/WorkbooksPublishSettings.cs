using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

public partial class WorkbookPublishSettings
{
    /// <summary>
    /// TRUE: The published workbook should show tabs to navigate the sheets/dashboards/stories
    /// FALSE: No Tabs (each sheet/dashboard/story is accessed by its URL)
    /// </summary>
    public readonly bool ShowTabs = false;

    public WorkbookPublishSettings(bool showTabs)
    {
        this.ShowTabs = showTabs;
    }
}
