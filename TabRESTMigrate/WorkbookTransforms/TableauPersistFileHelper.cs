using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Xml;

/// <summary>
/// Helper functions for outputting Workbook (*.twb) and Datasource (*.tds) fles
/// </summary>
static class TableauPersistFileHelper
{

    /// <summary>
    /// Output an XML file in the format that Tableau expects
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="pathToOutput"></param>
    public static void WriteTableauXmlFile(XmlDocument xmlDoc, string pathToOutput)
    {
        //[2015-03-20]   Presently Server will error if it gets a TWB (XML) document uploaded that has a Byte Order Marker
        //                  (this will however work if the TWB is within a TWBX).  
        //                  To accomodate we need to write out XML without a BOM
        var utf8_noBOM = new System.Text.UTF8Encoding(false);
        using (var textWriter = new XmlTextWriter(pathToOutput, utf8_noBOM))                   
        {
            xmlDoc.WriteTo(textWriter);
            textWriter.Close();
        }

        //Write out the modified XML
//        var textWriter = new XmlTextWriter(_pathToTwbOutput, Encoding.UTF8);  //[2015-03-20] FAILS in TWB uploads (works in TWBX uploads) because Server errrors if it hits the byte order marker
//        var textWriter = new XmlTextWriter(_pathToTwbOutput, Encoding.ASCII); //[2015-03-20] Succeeds; but too limiting.  We want UTF-8
//        using (textWriter)
//        {
//            xmlDoc.WriteTo(textWriter);
//            textWriter.Close();
//        }
    }

}
