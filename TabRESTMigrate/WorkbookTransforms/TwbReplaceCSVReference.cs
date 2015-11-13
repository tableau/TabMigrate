using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Xml;

/// <summary>
/// Replaces the CSV file reference in a Workbooks data soruce
/// </summary>
class TwbReplaceCSVReference
{
    private readonly string _pathToTwbInput;
    private readonly string _pathToTwbOutput;
    private readonly string _datasourceName;
    private readonly string _datasourceNewCsvPath;
    private readonly TaskStatusLogs _statusLog;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pathTwbInput">TWB we are going to load and transform</param>
    /// <param name="pathTwbOutput">Output path for transformed CSV</param>
    /// <param name="dataSourceName">Name of data source inside path</param>
    /// <param name="newCsvPath">Path to CSV file that we want the data source to point to</param>
    /// <param name="statusLog">Log status and errors here</param>
    public TwbReplaceCSVReference(string pathTwbInput, string pathTwbOutput, string dataSourceName, string newCsvPath, TaskStatusLogs statusLog)
    {
        _pathToTwbInput = pathTwbInput;
        _pathToTwbOutput = pathTwbOutput;
        _datasourceName = dataSourceName;
        _datasourceNewCsvPath = newCsvPath;
        _statusLog = statusLog;
    }


    /// <summary>
    /// Run the file transformation and output the remapped file
    /// </summary>
    /// <returns>TRUE if we found at least one reference to replace. FALSE no replacements were found</returns>
    public bool Execute()
    {
        //4. Load XML for *.twb file
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(_pathToTwbInput);

        bool foundReplaceItem = 
            RemapDatasourceCsvReference(xmlDoc, _datasourceName, _datasourceNewCsvPath, _statusLog);

        //Write out the transformed XML document
        TableauPersistFileHelper.WriteTableauXmlFile(xmlDoc, _pathToTwbOutput);
        return foundReplaceItem;
    }

    /// <summary>
    /// Finds and changes a datasource reference inside a Workbook. Changes the CSV file the data source points to
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="datasourceName"></param>
    /// <param name="pathToTargetCsv"></param>
    /// <param name="statusLog"></param>
    private bool RemapDatasourceCsvReference(XmlDocument xmlDoc, string datasourceName, string pathToTargetCsv, TaskStatusLogs statusLog)
    {
        int replaceItemCount = 0;
        string newCsvDirectory = Path.GetDirectoryName(_datasourceNewCsvPath);
        string newCsvFileName = Path.GetFileName(_datasourceNewCsvPath);
        string newDatasourceRelationName = Path.GetFileNameWithoutExtension(newCsvFileName) + "#csv";
        string newDatasourceRelationTable = "[" + newDatasourceRelationName + "]";
        string seekDatasourceCaption = _datasourceName;

        var xDataSources = xmlDoc.SelectNodes("workbook/datasources/datasource");
        if(xDataSources != null)
        {
            //Look through the data sources
            foreach (XmlNode xnodeDatasource in xDataSources)
            {
                //If the data source is matching the caption we are looking for
                if(XmlHelper.SafeParseXmlAttribute(xnodeDatasource, "caption", "") == seekDatasourceCaption)
                {
                    var xnodeConnection = xnodeDatasource.SelectSingleNode("connection");
                    //It should be 'textscan', it would be unexpected if it were not
                    if(XmlHelper.SafeParseXmlAttribute(xnodeConnection, "class", "") == "textscan")
                    {
                        //Point to the new directory/path
                        xnodeConnection.Attributes["directory"].Value = newCsvDirectory;
                        xnodeConnection.Attributes["filename"].Value = newCsvFileName;

                        //And it's got a Relation we need to update
                        var xNodeRelation = xnodeConnection.SelectSingleNode("relation");
                        xNodeRelation.Attributes["name"].Value = newDatasourceRelationName;
                        xNodeRelation.Attributes["table"].Value = newDatasourceRelationTable;

                        replaceItemCount++;
                    }
                    else
                    {
                        _statusLog.AddError("Data source remap error. Expected data source to be 'textscan'");
                    }
                }//end if
            }//end foreach
        }//end if

        return replaceItemCount > 0;        
    }

}
