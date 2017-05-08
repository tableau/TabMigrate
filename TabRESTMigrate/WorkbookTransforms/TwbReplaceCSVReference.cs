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
    private readonly string _oldDatasourceFilename;
    private readonly string _datasourceNewCsvPath;
    private readonly TaskStatusLogs _statusLog;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pathTwbInput">TWB we are going to load and transform</param>
    /// <param name="pathTwbOutput">Output path for transformed CSV</param>
    /// <param name="oldDatasourceFilename">Old filename for the data source (case insensitive)</param>
    /// <param name="newCsvPath">Path to CSV file that we want the data source to point to</param>
    /// <param name="statusLog">Log status and errors here</param>
    public TwbReplaceCSVReference(string pathTwbInput, string pathTwbOutput, string oldDatasourceFilename, string newCsvPath, TaskStatusLogs statusLog)
    {
        _pathToTwbInput = pathTwbInput;
        _pathToTwbOutput = pathTwbOutput;
        _oldDatasourceFilename = oldDatasourceFilename;
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
            RemapDatasourceCsvReference(xmlDoc, _oldDatasourceFilename, _datasourceNewCsvPath, _statusLog);

        //Write out the transformed XML document
        TableauPersistFileHelper.WriteTableauXmlFile(xmlDoc, _pathToTwbOutput);
        return foundReplaceItem;
    }

    /// <summary>
    /// Finds and changes a datasource reference inside a Workbook. Changes the CSV file the data source points to
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="oldDatasourceFilename">Filenane (without path) of the datasource we want to replace. Case insensitive</param>
    /// <param name="pathToTargetCsv"></param>
    /// <param name="statusLog"></param>
    private bool RemapDatasourceCsvReference(XmlDocument xmlDoc, string oldDatasourceFilename, string pathToTargetCsv, TaskStatusLogs statusLog)
    {
        int replaceItemCount = 0;
        string newCsvDirectory = Path.GetDirectoryName(_datasourceNewCsvPath);
        string newCsvFileName = Path.GetFileName(_datasourceNewCsvPath);
        string newDatasourceRelationName = Path.GetFileNameWithoutExtension(newCsvFileName) + "#csv";
        string newDatasourceRelationTable = "[" + newDatasourceRelationName + "]";

        var xDataSources = xmlDoc.SelectNodes("workbook/datasources/datasource");
        if(xDataSources != null)
        {
            //Look through the data sources
            foreach (XmlNode xnodeDatasource in xDataSources)
            {
                var xConnections = xnodeDatasource.SelectNodes(".//connection");
                if (xConnections != null)
                {
                    foreach (XmlNode xThisConnection in xConnections)
                    {
                        //If its a 'textscan' (CSV) and the file name matches the expected type, then this is a datasource's connection we want to remap 
                        //to point to a new CSV file
                        if ((XmlHelper.SafeParseXmlAttribute(xThisConnection, "class", "") == "textscan") &&
                            (string.Compare(XmlHelper.SafeParseXmlAttribute(xThisConnection, "filename", ""),
                                            oldDatasourceFilename, true) == 0))
                        {

                            //Find any relation nodes beneath the datasource
                            //Newer version of the document model put the textscan connection inside a federated data source
                            //to deal with that, we need to look upward from the connection and adjacent in the DOM to find the correct
                            //node to replace. This is done by looking at child nodes in the datasource
                            var xNodeAllConnectionRelations = xnodeDatasource.SelectNodes(".//relation");
                            XmlNode xNodeRelation = null;
                            if (xNodeAllConnectionRelations != null)
                            {
                                if(xNodeAllConnectionRelations.Count == 1)
                                {
                                    xNodeRelation = xNodeAllConnectionRelations[0];
                                }
                                else
                                {
                                    statusLog.AddError("CSV replacement. Expected 1 Relation in data source definition, actual " + xNodeAllConnectionRelations.Count.ToString());
                                }
                            }


                            //Only if we have all the elements need to replace, should we go ahead with the replacement
                            if ((xNodeRelation != null) && (xThisConnection != null))
                            {
                                //Point to the new directory/path
                                xThisConnection.Attributes["directory"].Value = newCsvDirectory;
                                xThisConnection.Attributes["filename"].Value = newCsvFileName;

                                xNodeRelation.Attributes["name"].Value = newDatasourceRelationName;
                                xNodeRelation.Attributes["table"].Value = newDatasourceRelationTable;

                                replaceItemCount++;
                            }
                        }

                    }//end: foreach xThisConnection
                }

            }//end foreach
        }//end if

        return replaceItemCount > 0;        
    }

}
