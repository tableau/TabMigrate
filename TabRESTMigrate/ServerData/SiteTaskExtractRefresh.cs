using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Schedule in a Server's site
/// </summary>
partial class SiteTaskExtractRefresh : IHasSiteItemId
{
    public readonly string Id;
    public readonly string ScheduleId;
    public readonly string PriorityText;
    public readonly string WorkbookId;  //Either WorkbookId or DatasourceId will be a valid GUID
    public readonly string DatasourceId;
    public readonly string RefreshType;
    public readonly string RefreshContentType;


    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="scheduleNode"></param>
    public SiteTaskExtractRefresh(XmlNode extractNode, string scheduleId)
    {
        if(string.IsNullOrWhiteSpace(scheduleId))
        {
            throw new ArgumentException("Schedule ID is requried for the extract refresh task object");
        }
        this.ScheduleId = scheduleId;

        var sbDevNotes = new StringBuilder();

        if(extractNode.Name.ToLower() != "extract")
        {
            AppDiagnostics.Assert(false, "Not an extract task");
            throw new Exception("Unexpected content - not Not an extract task");
        }

        this.Id = extractNode.Attributes["id"].Value;
        this.RefreshType = extractNode.Attributes["type"].Value;
        this.PriorityText = extractNode.Attributes["priority"].Value;

        //Namespace for XPath queries
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");

        //=============================================================
        //Determine whether the refresh targets a workbook or datasource
        //=============================================================
        var workbookNode = extractNode.SelectSingleNode("iwsOnline:workbook", nsManager);
        if (workbookNode != null)
        {
            this.WorkbookId = workbookNode.Attributes["id"].Value;
            this.RefreshContentType = "Workbook";
        }

        var datasourceNode = extractNode.SelectSingleNode("iwsOnline:datasource", nsManager);
        if (datasourceNode != null)
        {
            this.DatasourceId = datasourceNode.Attributes["id"].Value;
            this.RefreshContentType = "Datasource";
        }

        this.DeveloperNotes = sbDevNotes.ToString();
    }

    public override string ToString()
    {
        return "Extract task: " + this.Id;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
