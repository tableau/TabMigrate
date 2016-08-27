using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

internal partial class DatasourcePublishSettings
{
    private const string XmlElement_DatasourceInfo = "DatasourceInfo";
    private const string DatasourceSettingsSuffix = WorkbookPublishSettings.WorkbookSettingsSuffix;

    /// <summary>
    /// TRUE if the file is an internal settings file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    internal static bool IsSettingsFile(string filePath)
    {
        return filePath.EndsWith(DatasourceSettingsSuffix, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Save Datasource metadata in a XML file along-side the workbook file
    /// </summary>
    /// <param name="wb">Information about the workbook we have downloaded</param>
    /// <param name="localDatasourcePath">Local path to the twb/twbx of the workbook</param>
    /// <param name="userLookups">If non-NULL contains the mapping of users/ids so we can look up the owner</param>
    internal static void CreateSettingsFile(SiteDatasource ds, string localDatasourcePath, KeyedLookup<SiteUser> userLookups)
    {

        string contentOwnerName = null; //Start off assuming we have no content owner information
        if (userLookups != null)
        {
            contentOwnerName = WorkbookPublishSettings.helper_LookUpOwnerId(ds.OwnerId, userLookups);
        }

        var xml = System.Xml.XmlWriter.Create(PathForSettingsFile(localDatasourcePath));
        xml.WriteStartDocument();
            xml.WriteStartElement(XmlElement_DatasourceInfo);

                //If we have an owner name, write it out
                if (!string.IsNullOrWhiteSpace(contentOwnerName))
                {
                  XmlHelper.WriteValueElement(xml,  WorkbookPublishSettings.XmlElement_ContentOwner, contentOwnerName);
                }
            xml.WriteEndElement(); //end: WorkbookInfo
        xml.WriteEndDocument();
        xml.Close();
    }

    /// <summary>
    /// Generates the path/filename of the Settings file that corresponds to the datasource path
    /// </summary>
    /// <param name="datasourcePath"></param>
    /// <returns></returns>
    private static string PathForSettingsFile(string datasourcePath)
    {
        return WorkbookPublishSettings.PathForSettingsFile(datasourcePath);
    }


    /// <summary>
    /// Look up any saved settings we have associated with a datasource on our local file systemm
    /// </summary>
    /// <param name="datasourceWithPath"></param>
    /// <returns></returns>
    internal static DatasourcePublishSettings GetSettingsForSavedDatasource(string datasourceWithPath)
    {
        //Sanity test: If the datasource is not there, then we probably have an incorrect path
        AppDiagnostics.Assert(File.Exists(datasourceWithPath), "Underlying datasource does not exist");

        //Find the path to the settings file
        var pathToSettingsFile = PathForSettingsFile(datasourceWithPath);
        if (!File.Exists(pathToSettingsFile))
        {
            return new DatasourcePublishSettings(null);
        }

        //===================================================================
        //We've got a setings file, let's parse it!
        //===================================================================
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(pathToSettingsFile);

        //Show sheets
        string ownerName = WorkbookPublishSettings.ParseXml_GetOwnerName(xmlDoc);

        //Return the Settings data
        return new DatasourcePublishSettings(ownerName);
    }

}
