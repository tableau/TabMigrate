using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

public partial class WorkbookPublishSettings
{
    private const string XmlElement_WorkbookInfo = "WorkbookInfo";
    private const string XmlElement_ShowTabsInWorkbook = "ShowTabsInWorkbook";
    private const string XmlAttribute_Value = "value";
    private const string WorkbookSettingsSuffix = ".info.xml";


    /// <summary>
    /// TRUE if the file is an internal settings file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    internal static bool IsWorkbookSettingsFile(string filePath)
    {
        return filePath.EndsWith(WorkbookSettingsSuffix, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Save Workbook metadata in a XML file along-side the workbook file
    /// </summary>
    /// <param name="wb">Information about the workbook we have downloaded</param>
    /// <param name="localWorkbookPath">Local path to the twb/twbx of the workbook</param>
    internal static void CreateWorkbookSettingsFile(SiteWorkbook wb, string localWorkbookPath)
    {
        var xml = System.Xml.XmlWriter.Create(PathForWorkbookSettingsFile(localWorkbookPath));
        xml.WriteStartDocument();
            xml.WriteStartElement(XmlElement_WorkbookInfo);
                xml.WriteStartElement(XmlElement_ShowTabsInWorkbook);
                XmlHelper.WriteBooleanAttribute(xml, XmlAttribute_Value, wb.ShowTabs);
                xml.WriteEndElement(); //end: ShowTabs
            xml.WriteEndElement(); //end: WorkbookInfo
        xml.WriteEndDocument();
        xml.Close();
    }


    /// <summary>
    /// Look up any saved settings we have associated with a workbook on our local file systemm
    /// </summary>
    /// <param name="workbookWithPath"></param>
    /// <returns></returns>
    internal static  WorkbookPublishSettings GetSettingsForSavedWorkbook(string workbookWithPath)
    {
        //Sanity test: If the workbook is not there, then we probably have an incorrect path
        AppDiagnostics.Assert(File.Exists(workbookWithPath), "Underlying workbook does not exist");

        //Find the path to the settings file
        var pathToSettingsFile = PathForWorkbookSettingsFile(workbookWithPath);
        if(!File.Exists(pathToSettingsFile))
        {
            return GenerateDefaultSettings();
        }

        //===================================================================
        //We've got a setings file, let's parse it!
        //===================================================================
        bool showSheetsInTabs; 
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(pathToSettingsFile);

        showSheetsInTabs = ParseXml_GetShowSheetsAsTabs(xmlDoc);

        //Return the Settings file
        return new WorkbookPublishSettings(showSheetsInTabs); 
    }

    /// <summary>
    /// Looks for the ShowTabas information inside the XML document
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <returns>
    /// TRUE/FALSE - whether the workbook wants to show tabs in the viz
    /// </returns>
    static bool ParseXml_GetShowSheetsAsTabs(XmlDocument xmlDoc)
    {
        var xNodeShowTabs = xmlDoc.SelectSingleNode("//" + XmlElement_ShowTabsInWorkbook);

        //If there is no node, then return the default
        if(xNodeShowTabs == null)
        {
            return false;
        }

        return XmlHelper.SafeParseXmlAttribute_Bool(xNodeShowTabs, XmlAttribute_Value, false);
    }

    /// <summary>
    /// Default settings to use if no settings file is present
    /// </summary>
    internal static WorkbookPublishSettings GenerateDefaultSettings()
    {
        return new WorkbookPublishSettings(false);
    }

    /// <summary>
    /// Generates the path/filename of the Settings file that corresponds to the workbook path
    /// </summary>
    /// <param name="workbookPath"></param>
    /// <returns></returns>
    private static string PathForWorkbookSettingsFile(string workbookPath)
    {
        //Sanity test
        if(string.IsNullOrWhiteSpace(workbookPath))
        {
            AppDiagnostics.Assert(false, "missing path");
            throw new ArgumentNullException("missing path");
        }

        //Tag an extra extension to the file
        return workbookPath + WorkbookSettingsSuffix;
    }
}
