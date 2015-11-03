using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Helper class.  Files downloaded from Tableau Server can be either binary (octet-stream) or XML (application/xml),
/// for example a *.TWBX is binary (it is a *.zip file) and a *.TWB is XML.  
/// 
/// This is a helper class that mapps the returned payload type to the appropriate file system extension.  
/// 
/// It is used for helping with downloads of Workbooks (*.twb vs. *.twbx) and Datasources (*.tds vs. *.tdsx)
/// </summary>
public class DownloadPayloadTypeHelper
{
    private Dictionary<string, string> _mapContent = new Dictionary<string, string>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fileExtensionIfBinary"></param>
    /// <param name="fileExtensionIfXml"></param>
    public DownloadPayloadTypeHelper(string fileExtensionIfBinary, string fileExtensionIfXml)
    {
        fileExtensionIfBinary = EnsureFileExensionFormat(fileExtensionIfBinary);
        fileExtensionIfXml = EnsureFileExensionFormat(fileExtensionIfXml);

        //Common mappings we expect
        _mapContent.Add("application/octet-stream", fileExtensionIfBinary);
        _mapContent.Add("application/xml", fileExtensionIfXml);
    }

    /// <summary>
    /// Given a content type, return the corresponding file extension we want to save the content as
    /// </summary>
    /// <param name="contentType"></param>
    /// <returns></returns>
    public string GetFileExtension(string contentType)
    {
        return _mapContent[contentType];
    }

    /// <summary>
    /// Adds a "." if we need it
    /// </summary>
    /// <param name="extension"></param>
    /// <returns></returns>
    private static string EnsureFileExensionFormat(string extension)
    {
        extension = extension.Trim();
        if (extension[0] != '.') { extension = "." + extension; }

        return extension;
    }

}
