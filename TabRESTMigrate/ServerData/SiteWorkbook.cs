using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Workbook in a Server's site
/// </summary>
class SiteWorkbook : SiteDocumentBase
{
    public readonly bool ShowTabs;
    //Note: [2015-10-28] Datasources presently don't return this information, so we need to make this workbook specific
    public readonly string ContentUrl;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="workbookNode"></param>
    public SiteWorkbook(XmlNode workbookNode) : base(workbookNode)
    {
        if(workbookNode.Name.ToLower() != "workbook")
        {
            AppDiagnostics.Assert(false, "Not a workbook");
            throw new Exception("Unexpected content - not workbook");
        }

        //Note: [2015-10-28] Datasources presently don't return this information, so we need to make this workbook specific
        this.ContentUrl = workbookNode.Attributes["contentUrl"].Value;

        //Do we have tabs?
        this.ShowTabs = XmlHelper.SafeParseXmlAttribute_Bool(workbookNode, "showTabs", false);
    }


    public override string ToString()
    {
        return "Workbook: " + Name + "/" + ContentUrl + "/" + Id + ", Proj: " + ProjectId;
    }

}
