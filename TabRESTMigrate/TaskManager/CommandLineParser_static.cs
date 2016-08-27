using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// Gives parsed values for the command line arguments
/// </summary>
partial class CommandLineParser
{

    public const string Parameter_Command               = "-command";             //Specifies the top level command to the REST applicaiton
    public const string Parameter_FromSiteUrl           = "-fromSiteUrl";         //URL to site we are accessing
    public const string Parameter_FromUserId            = "-fromSiteUserId";      //User ID we are downloading content from
    public const string Parameter_FromUserPassword      = "-fromSiteUserPassword";//Password for user id 
    public const string Parameter_FromSiteIsSiteAdmin   = "-fromSiteIsSiteAdmin";   //Is the user id a Site Admin account?
    public const string Parameter_FromSiteIsSystemAdmin = "-fromSiteIsSystemAdmin"; //Is the user id a System Admin account?
    public const string Parameter_ToSiteUrl             = "-toSiteUrl";           //URL to site we are accessing
    public const string Parameter_ToUserId              = "-toSiteUserId";        //User ID we are downloading content from
    public const string Parameter_ToUserPassword        = "-toSiteUserPassword";  //Password for user id 
    public const string Parameter_ToSiteIsSystemAdmin   = "-toSiteIsSystemAdmin"; //Is the user id a System Admin account?
    public const string Parameter_ToSiteIsSiteAdmin     = "-toSiteIsSiteAdmin";   //Is the user id a Site Admin account?
    public const string Parameter_ExportSingleProject   = "-exportSingleProject"; //If specified, only a single projects content will be exported
    public const string Parameter_ExportOnlyWithTag     = "-exportOnlyTagged";    //If specified, only content with the specified tag will be exported
    public const string Parameter_RemoveTagAfterExport  = "-exportOnlyTaggedRemoveTag"; //If specified, we will remove the tag from any exported content 
    public const string Parameter_DBCredentialsFile     = "-dbCredentialsFile";   //If specified, this points to the file we should get upload DB credentials from
    public const string Parameter_LogFile               = "-logFile";             //File for log output
    public const string Parameter_LogVerbose            = "-logVerbose";          //Verbose logging level
    public const string Parameter_BackgroundKeepAlive   = "-backgroundKeepAlive"; //Send periodic background requests to ensure the server session is kept alive
    public const string Parameter_GenerateInventoryTwb  = "-generateInventoryTwb";//Create a Tableau Workbook with inventory data
    public const string Parameter_ErrorsFile            = "-errorFile";           //File for error output
    public const string Parameter_ManualStepsFile       = "-manualStepsFile";     //File for recording manual steps for tasks that could not be automatically completed
    public const string Parameter_InventoryOutputFile   = "-inventoryOutputFile"; //Where the inventory output goes
    public const string Parameter_ExportDirectory       = "-exportDirectory";     //Where the site gets exported to
    public const string Parameter_ImportDirectory       = "-importDirectory";     //Where the site gets imported from
    public const string Parameter_ExitWhenDone          = "-exitWhenDone";        //When running as command line, if 'true' we will exit when the work is done
    public const string Parameter_ImportAssignContentOwnership  = "-remapContentOwnership"; //On site import, look for content metadata files that tell us what owner to assign the content to
    public const string Parameter_RemapDataserverReferences     = "-remapDataserverReferences"; //On site import, workbook XML should be examined and have data server references updated to point to the target server/site
    public const string Parameter_GenerateInfoFilesForDownloads = "-downloadInfoFiles"; //Downloaded Workbooks/Datasources will get companion XML files with additional metadata that can be used during uploads (e.g. show tabs in workbooks)

    //Get an inventory of the running server
    public const string ParameterValue_Command_Inventory = "siteInventory";
    public const string ParameterValue_Command_Export    = "siteExport";
    public const string ParameterValue_Command_Import    = "siteImport";
    public const string ParameterValue_True              = "true";
    public const string ParameterValue_False             = "false";

    //Standard dummy text we want to use for obscured passwords
    public const string DummyText_Password = "*****";


    /// <summary>
    /// TRUE if we believe there is enough information in the command line to proceed with running the headless task
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    public static bool HasUseableCommandLine(CommandLineParser commandLine)
    {
        //If the command line does not contain a "-command" argument, then we know there's nothing we can do
        if(string.IsNullOrWhiteSpace(commandLine.GetParameterValue(Parameter_Command)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gives us an example command line for getting a site inventory
    /// </summary>
    /// <param name="pathToOutputFile"></param>
    /// <param name="siteUrl"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSystemAdmin"></param>
    /// <param name="commandLineOut">Command line as text ready to show the user</param>
    /// <param name="parsedCommandLine">Command line as set of </param>
    public static void GenerateCommandLine_SiteInventory(
        bool showPasswordInUi,
        string pathToOutputFile, 
        string siteUrl, 
        string userName, 
        string password, 
        bool isSystemAdmin,
        string pathToLogFile,
        string pathToErrorsFile,
        bool generateTwbFile,
        bool logVerbose,
        out string commandLineOut,
        out CommandLineParser parsedCommandLine)
    {
        string uiPassword = helper_DetermineIfDummyPasswordNeeded(showPasswordInUi, password);
        //We will return a the list of ordered command line arguments as an array
        var arguments = new List<string>();

        var sb = new StringBuilder();
        helper_AppendParameter(arguments, sb, Parameter_Command              , ParameterValue_Command_Inventory); //Command is 'take inventory'
        helper_AppendParameter(arguments, sb, Parameter_FromUserId           , userName);
        helper_AppendParameter(arguments, sb, Parameter_FromUserPassword     , password, uiPassword);
        helper_AppendParameter(arguments, sb, Parameter_InventoryOutputFile  , pathToOutputFile);
        helper_AppendParameter(arguments, sb, Parameter_FromSiteUrl          , siteUrl);
        helper_AppendParameter(arguments, sb, Parameter_FromSiteIsSystemAdmin, helper_BoolToText(isSystemAdmin));
        helper_AppendParameter(arguments, sb, Parameter_LogVerbose           , helper_BoolToText(logVerbose));

        //Do we want to generate a *.twb file that goes along with the inventory file
        if(generateTwbFile)
        {
            helper_AppendParameter(arguments, sb, Parameter_GenerateInventoryTwb, helper_BoolToText(true));
        }


        //Log file?
        if (!string.IsNullOrWhiteSpace(pathToLogFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_LogFile, pathToLogFile);
        }

        //Errors file?
        if (!string.IsNullOrWhiteSpace(pathToErrorsFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_ErrorsFile, pathToErrorsFile);
        }

        //In the command line we generate, by default include the 'exit on finish'
        helper_AppendParameter(arguments, sb, Parameter_ExitWhenDone, helper_BoolToText(true));

        //Return the raw command line, quoted values as necessary
        commandLineOut =  sb.ToString();

        //Return the arugments as an array
        parsedCommandLine = new CommandLineParser(arguments);
    }


    /// <summary>
    /// Gives us an example command line for getting a site inventory
    /// </summary>
    /// <param name="pathToExportDir"></param>
    /// <param name="siteUrl"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSiteAdmin"></param>
    /// <param name="commandLineOut">Command line as text ready to show the user</param>
    /// <param name="parsedCommandLine">Command line as set of </param>
    public static void GenerateCommandLine_SiteExport(
        bool showPasswordInUi,
        string pathToExportDir,
        string siteUrl,
        string userName,
        string password,
        bool isSiteAdmin,
        string exportSingleProject,
        string exportOnlyWithTag,
        bool removeExportTag,
        string pathToLogFile,
        string pathToErrorsFile,
        bool backgroundKeepAliveRequests,
        bool logVerbose,
        bool generateDownloadInfoFiles,
        out string commandLineOut,
        out CommandLineParser parsedCommandLine)
    {
        string uiPassword = helper_DetermineIfDummyPasswordNeeded(showPasswordInUi, password);
        //We will return a the list of ordered command line arguments as an array
        var arguments = new List<string>();

        var sb = new StringBuilder();
        helper_AppendParameter(arguments, sb, Parameter_Command, ParameterValue_Command_Export);
        helper_AppendParameter(arguments, sb, Parameter_FromUserId, userName);
        helper_AppendParameter(arguments, sb, Parameter_FromUserPassword, password, uiPassword);
        helper_AppendParameter(arguments, sb, Parameter_ExportDirectory, pathToExportDir);
        helper_AppendParameter(arguments, sb, Parameter_FromSiteUrl, siteUrl);
        helper_AppendParameter(arguments, sb, Parameter_FromSiteIsSystemAdmin, helper_BoolToText(isSiteAdmin));
        helper_AppendParameter(arguments, sb, Parameter_BackgroundKeepAlive, helper_BoolToText(backgroundKeepAliveRequests));
        helper_AppendParameter(arguments, sb, Parameter_LogVerbose, helper_BoolToText(logVerbose));
        helper_AppendParameter(arguments, sb, Parameter_GenerateInfoFilesForDownloads, helper_BoolToText(generateDownloadInfoFiles));

        //Export only a single project?
        if (!string.IsNullOrWhiteSpace(exportSingleProject))
        {
            helper_AppendParameter(arguments, sb, Parameter_ExportSingleProject, exportSingleProject);
        }

        //Export only if tagged
        if (!string.IsNullOrWhiteSpace(exportOnlyWithTag))
        {
            helper_AppendParameter(arguments, sb, Parameter_ExportOnlyWithTag, exportOnlyWithTag);
            helper_AppendParameter(arguments, sb, Parameter_RemoveTagAfterExport, helper_BoolToText(removeExportTag));
        }


        //Log file?
        if (!string.IsNullOrWhiteSpace(pathToLogFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_LogFile, pathToLogFile);
        }

        //Errors file?
        if (!string.IsNullOrWhiteSpace(pathToErrorsFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_ErrorsFile, pathToErrorsFile);
        }

        //In the command line we generate, by default include the 'exit on finish'
        helper_AppendParameter(arguments, sb, Parameter_ExitWhenDone, helper_BoolToText(true));

        //Return the raw command line, quoted values as necessary
        commandLineOut = sb.ToString();

        //Return the arugments as an array
        parsedCommandLine = new CommandLineParser(arguments);
    }

    /// <summary>
    /// Site import from local file system
    /// </summary>
    /// <param name="showPasswordInUi"></param>
    /// <param name="pathToImportFrom"></param>
    /// <param name="siteUrl"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="isSiteAdmin"></param>
    /// <param name="remapDataServerReferences"></param>
    /// <param name="pathToDbCredentialsFile"></param>
    /// <param name="pathToLogFile"></param>
    /// <param name="pathToErrorsFile"></param>
    /// <param name="pathToManualStepsFile"></param>
    /// <param name="remapContentOwnership"></param>
    /// <param name="logVerbose"></param>
    /// <param name="commandLineOut"></param>
    /// <param name="parsedCommandLine"></param>
    internal static void GenerateCommandLine_SiteImport(
        bool showPasswordInUi,
        string pathToImportFrom,
        string siteUrl,
        string userName,
        string password,
        bool isSiteAdmin,
        bool remapDataServerReferences,
        string pathToDbCredentialsFile,
        string pathToLogFile,
        string pathToErrorsFile,
        string pathToManualStepsFile,
        bool remapContentOwnership,
        bool logVerbose,
        out string commandLineOut,
        out CommandLineParser parsedCommandLine)
    {
        //We will return a the list of ordered command line arguments as an array
        var arguments = new List<string>();
        string uiPassword = helper_DetermineIfDummyPasswordNeeded(showPasswordInUi, password);

        var sb = new StringBuilder();
        helper_AppendParameter(arguments, sb, Parameter_Command, ParameterValue_Command_Import);
        helper_AppendParameter(arguments, sb, Parameter_ToUserId, userName);
        helper_AppendParameter(arguments, sb, Parameter_ToUserPassword, password, uiPassword);
        helper_AppendParameter(arguments, sb, Parameter_ImportDirectory, pathToImportFrom);
        helper_AppendParameter(arguments, sb, Parameter_ToSiteUrl, siteUrl);
        helper_AppendParameter(arguments, sb, Parameter_ToSiteIsSiteAdmin, helper_BoolToText(isSiteAdmin));
        helper_AppendParameter(arguments, sb, Parameter_RemapDataserverReferences, helper_BoolToText(remapDataServerReferences));
        helper_AppendParameter(arguments, sb, Parameter_ImportAssignContentOwnership, helper_BoolToText(remapContentOwnership));
        helper_AppendParameter(arguments, sb, Parameter_LogVerbose, helper_BoolToText(logVerbose));

        //Credentials file?
        if (!string.IsNullOrWhiteSpace(pathToDbCredentialsFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_DBCredentialsFile, pathToDbCredentialsFile);
        }

        //Log file?
        if (!string.IsNullOrWhiteSpace(pathToLogFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_LogFile, pathToLogFile);
        }

        //Errors file?
        if (!string.IsNullOrWhiteSpace(pathToErrorsFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_ErrorsFile, pathToErrorsFile);
        }

        //Manual steps file?
        if(!string.IsNullOrWhiteSpace(pathToManualStepsFile))
        {
            helper_AppendParameter(arguments, sb, Parameter_ManualStepsFile, pathToManualStepsFile);
        }


        //In the command line we generate, by default include the 'exit on finish'
        helper_AppendParameter(arguments, sb, Parameter_ExitWhenDone, helper_BoolToText(true));

        //Return the raw command line, quoted values as necessary
        commandLineOut = sb.ToString();

        //Return the arugments as an array
        parsedCommandLine = new CommandLineParser(arguments);
    }

    /// <summary>
    /// Determines whether we are showing the dummy password or not
    /// </summary>
    /// <param name="showPasswordInUi"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    private static string helper_DetermineIfDummyPasswordNeeded(bool showPasswordInUi, string password)
    {
      if(showPasswordInUi)
      {
          return password;
      }
      return DummyText_Password;
    }


    /// <summary>
    /// Simple conversion bool to text
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    private static string helper_BoolToText(bool b)
    {
        if(b) return "true";
        return "false";
    }

    /// <summary>
    /// Appends a paramter and value
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="sb"></param>
    /// <param name="param"></param>
    /// <param name="value"></param>
    private static void helper_AppendParameter(List<string> arguments, StringBuilder sb, string param, string value)
    {
        //By default use the same value for the the arguments list and the string builder
        helper_AppendParameter(arguments, sb, param, value, value);
    }
    /// <summary>
    /// Appends a paramter and value
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="param"></param>
    /// <param name="value"></param>
    private static void helper_AppendParameter(List<string> arguments, StringBuilder sb, string param, string value, string valueForStringBuilder)
    {
        //Cannonicalize: Parameters all start with "-"
        if (param[0] != '-')
        {
            param = "-" + param;
        }

        value = value.Trim();
        //Add a space if there is already content in the command line
        if(sb.Length > 0)
        {
            sb.Append(" ");
        }

        sb.Append(param);

        sb.Append(" ");

        //If the value has any spaces, it needs quotes
        bool valueNeedsQuote = value.Contains(' ');
        if(valueNeedsQuote)
        {
            sb.Append("\"");  //Start qoute
        }

        sb.Append(valueForStringBuilder);

        if (valueNeedsQuote) 
        {
            sb.Append("\"");  //End qoute
        }

        //Append the arugments to the list
        arguments.Add(param);
        arguments.Add(value);
    }
}
