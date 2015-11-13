using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Paths we care about
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Path to the applicaiton
    /// </summary>
    /// <returns></returns>
    public static string GetApplicaitonPath()
    {
        //Gets the path to the application
        return System.Reflection.Assembly.GetExecutingAssembly().Location;
    }


    /// <summary>
    /// Get's the directory the application is running in
    /// </summary>
    /// <returns></returns>
    public static string GetApplicaitonDirectory()
    {
        return Path.GetDirectoryName(GetApplicaitonPath());
    }

    /// <summary>
    /// Path to the template file we want to use for the Inventory Workbook
    /// </summary>
    /// <returns></returns>
    public static string GetInventoryTwbTemplatePath()
    {
        return Path.Combine(GetApplicaitonDirectory(), "_SampleFiles\\SiteInventory.twb");
    }

    /// <summary>
    /// Inventory *.twb files are named to match the *.csv files they use.  
    /// This function generates the *.twb name/path based on the *.csv name/path
    /// </summary>
    /// <param name="pathCsv"></param>
    /// <returns></returns>
    public static string GetInventoryTwbPathMatchingCsvPath(string pathCsv)
    {
        string pathDir = Path.GetDirectoryName(pathCsv);
        string fileNameNoExtension = Path.GetFileNameWithoutExtension(pathCsv);
        string pathTwbOut = Path.Combine(pathDir, fileNameNoExtension + ".twb");
        return pathTwbOut;
    }
}
