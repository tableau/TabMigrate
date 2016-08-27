using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Uploads a directory full of workbooks 
/// </summary>
partial class UploadWorkbooks : TableauServerSignedInRequestBase
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Path we are uploading from
    /// </summary>
    private readonly string _localUploadPath;

    /// <summary>
    /// If TRUE, Workbooks are modified to map their references to point to the server they are being uploaded to
    /// </summary>
    private readonly bool _remapWorkbookReferences;

    /// <summary>
    /// File system path to perform temp work in (e.g. file remapping)
    /// </summary>
    private readonly string _localPathTempWorkspace;


    /// <summary>
    /// Do we create new projects on server if they are not there?  Fallback behavior if we cannot create projects
    /// </summary>
    private readonly UploadBehaviorProjects _uploadProjectBehavior;
    private readonly CustomerManualActionManager _manualActions; //Tracks any manual actions we discover we need
    private readonly int _uploadChunkSizeBytes;  //Max size of upload chunks
    private readonly int _uploadChunkDelaySeconds; //Delay afte reach chunk
    /// <summary>
    ///We will use this to find any stored credentials we need to upload 
    /// </summary>
    private readonly CredentialManager _credentialManager;

    /// <summary>
    /// TRUE: After the upload, attempt to reassign the owner of the content
    /// </summary>
    private readonly bool _attemptOwnershipAssignment;

    /// <summary>
    /// List of users in the site (used for looking up user-ids, based on the user names and mapping ownership)
    /// </summary>
    private readonly IEnumerable<SiteUser> _siteUsers;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="credentialManager">Set of database credentials to attach to associated content being published</param>
    /// <param name="localUploadPath">Path to upload from</param>
    /// <param name="remapWorkbookReferences">TRUE if we want to modify the workbooks to point datasource/other references to the new server</param>
    /// <param name="localPathTempWorkspace">Path to perform local file work in</param>
    /// <param name="uploadProjectBehavior">Instructions on whether to map content into projects</param>
    /// <param name="manualActions">Any manual actions that need to be performed by the user are written here</param>
    /// <param name="attemptOwnershipAssignment">TRUE: After upload attempt to reassign the ownership of the content based on local metadata we have</param>
    /// <param name="siteUsers">List of users to perform ownership assignement with</param>
    /// <param name="uploadChunkDelaySeconds">For testing, a delay we can inject</param>
    public UploadWorkbooks(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        CredentialManager credentialManager,
        string localUploadPath,
        bool remapWorkbookReferences,
        string localPathTempWorkspace,
        UploadBehaviorProjects uploadProjectBehavior,
        CustomerManualActionManager manualActions,
        bool attemptOwnershipAssignment,
        IEnumerable<SiteUser> siteUsers,
        int uploadChunkSizeBytes = TableauServerUrls.UploadFileChunkSize,
        int uploadChunkDelaySeconds = 0)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _localUploadPath = localUploadPath;
        _remapWorkbookReferences = remapWorkbookReferences;
        _localPathTempWorkspace = localPathTempWorkspace;
        _uploadProjectBehavior = uploadProjectBehavior;
        _manualActions = manualActions;
        _credentialManager = credentialManager; 
        if (_manualActions == null)
        {
            _manualActions = new CustomerManualActionManager();
        }

        //If we are going to attempt to reassign ownership after publication we'll need this information
        _attemptOwnershipAssignment = attemptOwnershipAssignment;
        _siteUsers = siteUsers;

        //Test parameters
        _uploadChunkSizeBytes = uploadChunkSizeBytes;
        _uploadChunkDelaySeconds = uploadChunkDelaySeconds;
    }

    /// <summary>
    /// Upload all the workbook files
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        //Helper object to track project Ids and create projects as needed
        var projectListHelper = new ProjectFindCreateHelper(_onlineUrls, this._onlineSession, _uploadProjectBehavior);

        var statusLog = this.StatusLog;
        int countSuccess = 0;
        int countFailure = 0;

        statusLog.AddStatus("Uploading workbooks");
        UploadDirectoryToServer(_localUploadPath, _localUploadPath, projectListHelper, true, out countSuccess, out countFailure);
        this.StatusLog.AddStatus("Workbooks upload done.  Success: " + countSuccess.ToString() + ", Failure: " + countFailure.ToString());
    }

    /// <summary>
    /// Looks to see if there are database associated credentils that should be associated with the
    /// content being uploaded
    /// </summary>
    /// <param name="contentFileName">Filename without path</param>
    /// <param name="projectName"></param>
    /// <returns></returns>
    private CredentialManager.Credential helper_DetermineContentCredential(
        string contentFileName, 
        string projectName)
    {
        var credentialManager = _credentialManager;
        //If there is no credential manager, there can be no credentials for any uploaded content
        if (credentialManager == null) 
        { 
            return null; 
        }
        return credentialManager.FindWorkbookCredential(
            contentFileName,
            projectName);
    }
    /// <summary>
    /// Uploads the content of the current directory
    /// </summary>
    /// <param name="rootContentPath"></param>
    /// <param name="currentContentPath"></param>
    /// <param name="recurseDirectories"></param>
    private void UploadDirectoryToServer(
        string rootContentPath, 
        string currentContentPath, 
        ProjectFindCreateHelper projectsList, 
        bool recurseDirectories, 
        out int countSuccess, out int countFailure)
    {
        countSuccess = 0;
        countFailure = 0;

        //--------------------------------------------------------------------------------------------
        //Look up the project name based on directory name, and creating a project on demand
        //--------------------------------------------------------------------------------------------
        string projectName;
        if (rootContentPath == currentContentPath) //If we are in the root upload directory, then assume any content goes to the Default project
        {
            projectName = ""; //Default project
        }
        else
        {
            projectName = FileIOHelper.Undo_GenerateWindowsSafeFilename(Path.GetFileName(currentContentPath));
        }

        //Start off with no project ID -- we'll look it up as needed
        string projectIdForUploads = null;

        //-------------------------------------------------------------------------------------
        //Upload the files from local directory to server
        //-------------------------------------------------------------------------------------
        foreach (var thisFilePath in Directory.GetFiles(currentContentPath))
        {
            bool isValidUploadFile = IsValidUploadFile(thisFilePath);

            if (isValidUploadFile)
            {
                //If we don't yet have a project ID, then get one
                if (string.IsNullOrWhiteSpace(projectIdForUploads))
                {
                    projectIdForUploads = projectsList.GetProjectIdForUploads(projectName);
                }

                try
                {
                    //See if there are any credentials we want to publish with the content
                    var dbCredentialsIfAny = helper_DetermineContentCredential(
                        Path.GetFileName(thisFilePath),
                        projectName);

                    //See what content specific settings there may be for this workbook
                    var publishSettings = helper_DetermineContentPublishSettings(thisFilePath);

                    bool wasFileUploaded = AttemptUploadSingleFile(thisFilePath, projectIdForUploads, dbCredentialsIfAny, publishSettings);
                    if (wasFileUploaded) { countSuccess++; }
                }
                catch (Exception ex)
                {
                    countFailure++;
                    StatusLog.AddError("Error uploading workbook " + thisFilePath + ". " + ex.Message);
                }
            }
        }

        //If we are running recursive , then look in the subdirectories too
        if(recurseDirectories)
        {
            int subDirSuccess;
            int subDirFailure;
            foreach(var subDirectory in Directory.GetDirectories(currentContentPath))
            {
                UploadDirectoryToServer(rootContentPath, subDirectory, projectsList, true, out subDirSuccess, out subDirFailure);
                countSuccess += subDirSuccess;
                countFailure += subDirFailure;
            }
        }
    }


    /// <summary>
    /// Look up settings associated with this content
    /// </summary>
    /// <param name="workbookWithPath"></param>
    /// <returns></returns>
    WorkbookPublishSettings helper_DetermineContentPublishSettings(string workbookWithPath)
    {
        return WorkbookPublishSettings.GetSettingsForSavedWorkbook(workbookWithPath);
    }

    /// <summary>
    /// Sanity testing on whether the file being uploaded is worth uploading
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <returns></returns>
    bool IsValidUploadFile(string localFilePath)
    {
        //If the file is a custom settings file for the workbook, then ignore it
        if(WorkbookPublishSettings.IsSettingsFile(localFilePath))
        {
            return false; //Nothing to do, it's just a settings file
        }

        //Ignore temp files, since we know we don't want to upload them
        var fileExtension = Path.GetExtension(localFilePath).ToLower();
        if ((fileExtension == ".tmp") || (fileExtension == ".temp"))
        {
            StatusLog.AddStatus("Ignoring temp file, " + localFilePath, -10);
            return false;
        }        

        //These are the only kinds of files we know about...
        if ((fileExtension != ".twb") && (fileExtension != ".twbx"))
        {
            StatusLog.AddError("File is not a workbook: " + localFilePath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thisFilePath"></param>
    /// <param name="projectIdForUploads"></param>
    /// <param name="dbCredentials">If not NULL, then these are the DB credentials we want to associate with the content we are uploading</param>
    /// <param name="publishSettings">Workbook publish settings (e.g. whether to show tabs in vizs)</param>
    private bool AttemptUploadSingleFile(
        string thisFilePath, 
        string projectIdForUploads,
        CredentialManager.Credential dbCredentials,
        WorkbookPublishSettings publishSettings)
    {
        //Assume it's a file we should try to upload
        if (_remapWorkbookReferences)
        {
            return AttemptUploadSingleFile_ReferencesRemapped(thisFilePath, projectIdForUploads, dbCredentials, publishSettings);
        }
        else
        {
            return AttemptUploadSingleFile_Inner(thisFilePath, projectIdForUploads, dbCredentials, publishSettings);
        }
    }

    /// <summary>
    /// Makes a copy of the file; remaps the Workbook references to the server, uploads the remapped file
    /// </summary>
    /// <param name="thisFilePath"></param>
    /// <param name="projectIdForUploads"></param>
    private bool AttemptUploadSingleFile_ReferencesRemapped(
        string thisFilePath, 
        string projectIdForUploads,
        CredentialManager.Credential dbCredentials,
        WorkbookPublishSettings publishSettings)
    {
        bool success = false;

        string filename = Path.GetFileName(thisFilePath);
        string pathToRemapFile = Path.Combine(_localPathTempWorkspace, filename);
        File.Copy(thisFilePath, pathToRemapFile, true); //Copy the file

        string fileType = Path.GetExtension(filename).ToLower();
        if(fileType == ".twb")
        {
            //Remap the references in the file
            var twbRemapper = new TwbDataSourceEditor(pathToRemapFile, pathToRemapFile, _onlineUrls, this.StatusLog);
            twbRemapper.Execute();
            success = AttemptUploadSingleFile_Inner(pathToRemapFile, projectIdForUploads, dbCredentials, publishSettings);
        }
        else if(fileType == ".twbx")
        {
            //Make sure we have a directory to unzip to
            var pathUnzip = Path.Combine(_localPathTempWorkspace, "unzipped");
            if(Directory.Exists(pathUnzip))
            {
                Directory.Delete(pathUnzip, true);
            }
            Directory.CreateDirectory(pathUnzip);

            var twbxRemapper = new TwbxDataSourceEditor(pathToRemapFile, pathUnzip, _onlineUrls, this.StatusLog);
            string pathTwbxRemappedOutput = twbxRemapper.Execute();
            //Upload the remapped file
            success = AttemptUploadSingleFile_Inner(pathTwbxRemappedOutput, projectIdForUploads, dbCredentials, publishSettings);

            //Clean-up and delete the whole unzipped directory
            Directory.Delete(pathUnzip, true);
        }
        else
        {
            //We should never hit this... bad content
            this.StatusLog.AddError("Error Workbook upload - Expected Workbook filetype! " + filename);
        }

        //Delete the remap file
        File.Delete(pathToRemapFile);
        return success;
    }

    /// <summary>
    /// Attempts to upload a single file a Tableau Server, and then make it a published workbook
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <returns></returns>
    private bool AttemptUploadSingleFile_Inner(
        string localFilePath, 
        string projectId, 
        CredentialManager.Credential dbCredentials,
        WorkbookPublishSettings publishSettings)
    {
        string uploadSessionId;
        try
        {
            var fileUploader = new UploadFile(_onlineUrls, _onlineSession, localFilePath, _uploadChunkSizeBytes, _uploadChunkDelaySeconds);
            uploadSessionId = fileUploader.ExecuteRequest();
        }
        catch (Exception exFileUpload)
        {
            this.StatusLog.AddError("Unexpected error attempting to upload file " + localFilePath + ", " + exFileUpload.Message);
            throw exFileUpload;
        }

        SiteWorkbook workbook;
        this.StatusLog.AddStatus("File chunks upload successful. Next step, make it a published workbook", -10);
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(localFilePath);
            string uploadType = RemoveFileExtensionDot(Path.GetExtension(localFilePath).ToLower());
            workbook = FinalizePublish(
                uploadSessionId,
                FileIOHelper.Undo_GenerateWindowsSafeFilename(fileName), //[2016-05-06] If the name has escapted characters, unescape them 
                uploadType, 
                projectId, 
                dbCredentials,
                publishSettings);
            StatusLog.AddStatus("Upload content details: " + workbook.ToString(), -10);
            StatusLog.AddStatus("Success! Uploaded workbook " + Path.GetFileName(localFilePath));
        }
        catch (Exception exPublishFinalize)
        {
            this.StatusLog.AddError("Unexpected error finalizing publish of file " + localFilePath + ", " + exPublishFinalize.Message);
            LogManualAction_UploadWorkbook(localFilePath);
            throw exPublishFinalize;
        }

        //See if we want to reassign ownership of the workbook
        if(_attemptOwnershipAssignment)
        {
            try
            {
                AttemptOwnerReassignment(workbook, publishSettings, _siteUsers);
            }
            catch (Exception exOwnershipAssignment)
            {
                this.StatusLog.AddError("Unexpected error reassigning ownership of published workbook " + workbook.Name + ", " + exOwnershipAssignment.Message);
                LogManualAction_ReassignOwnership(workbook.Name);
                throw exOwnershipAssignment;
            }
        }

        return true;     //Success
    }

    /// <summary>
    /// Assign ownership
    /// </summary>
    /// <param name="workbook"></param>
    /// <param name="publishSettings"></param>
    /// <param name="siteUsers"></param>
    /// <returns>TRUE: The server content has the correct owner now.  FALSE: We were unable to give the server content the correct owner</returns>
    private bool AttemptOwnerReassignment(SiteWorkbook workbook, WorkbookPublishSettings publishSettings, IEnumerable<SiteUser> siteUsers)
    {
        this.StatusLog.AddStatusHeader("Attempting ownership assignement for Workbook " + workbook.Name + "/" + workbook.Id);

        //Something went wrong if we don't have a set of site users to do the look up
        if (siteUsers == null)
        {
            throw new ArgumentException("No set of site users provided for lookup");
        }

        //Look the local meta data to see what the desired name is
        var desiredOwnerName = publishSettings.OwnerName;
        if(string.IsNullOrEmpty(desiredOwnerName))
        {
            this.StatusLog.AddStatus("Skipping owner assignment. The local file system has no metadata with an owner information for " + workbook.Name);
            LogManualAction_ReassignOwnership(workbook.Name, "none specified", "No client ownership information was specified");
            return true; //Since there is no ownership preference stated locally, then ownership we assigned during upload was fine.
        }

        //Look on the list of users in the target site/server, and try to find a match
        //
        //NOTE: We are doing a CASE INSENSITIVE name comparison. We assume that there are not 2 users with the same name on server w/differet cases
        //      Because if this, we want to be flexible and allow that our source/destination servers may have the user name specified with a differnt 
        //      case.  -- This is less secure than a case-sensitive comparison, but is almost always what we want when porting content between servers
        var desiredServerUser = SiteUser.FindUserWithName(siteUsers, desiredOwnerName, StringComparison.InvariantCultureIgnoreCase);

        if(desiredServerUser == null)
        {
            this.StatusLog.AddError("The local file has a workbook/user mapping: " + workbook.Name + "/" + desiredOwnerName + ", but this user does not exist on the target site");
            LogManualAction_ReassignOwnership(workbook.Name, desiredOwnerName, "The target site does not contain a user name that matches the owner specified by the local metadata");
            return false; //Not a run time error, but we have manual steps to perform
        }

        //If the server content is already correct, then there is nothing to do
        if(desiredServerUser.Id == workbook.OwnerId)
        {
            this.StatusLog.AddStatus("Workbook " + workbook.Name + "/" + workbook.Id + ", already has correct ownership. No update requried");
            return true; 
        }

        //Lets tell server to update the owner
        var changeOwnership = new SendUpdateWorkbookOwner(_onlineUrls, _onlineSession, workbook.Id, desiredServerUser.Id);
        SiteWorkbook updatedWorkbook;
        try
        {
            this.StatusLog.AddStatus("Server request to change Workbook ownership, wb: " + workbook.Name + "/" + workbook.Id + ", user:" + desiredServerUser.Name + "/" + desiredServerUser.Id);
            updatedWorkbook = changeOwnership.ExecuteRequest();
        }
        catch (Exception exChangeOnwnerhsip)
        {
            throw exChangeOnwnerhsip; //Unexpected error, send it upward
        }

        //Sanity check the result we got back: Double check to make sure we have the expected owner.
        if(updatedWorkbook.OwnerId != desiredServerUser.Id)
        {
            this.StatusLog.AddError("Unexpected server error! Updated workbook Owner Id does not match expected. wb: "
                + workbook.Name + "/" + workbook.Id + ", "
                + "expected user: " + desiredServerUser.Id + ", "
                + "actual user: " + updatedWorkbook.OwnerId
                );
        }

        return true;
    }


    /// <summary>
    /// Log a required manual action
    /// </summary>
    /// <param name="filePath"></param>
    private void LogManualAction_UploadWorkbook(string filePath)
    {
        //Log a manual action
        _manualActions.AddKeyValuePairs(new string[]
                {
                    "action  : Open workbook in Tableau Desktop and attempt manual upload"
                  , "content : Workbook"
                  , "cause   : Possibly the workbook contains live database connections that need credentials so that thumbnail images can get generated"
                  , "path    : " + filePath
                });
    }


    /// <summary>
    /// A manual step will be required to set the owner of the content
    /// </summary>
    /// <param name="filePath"></param>
    private void LogManualAction_ReassignOwnership(string workbookName, string ownerName = "", string cause = "")
    {
        //Cannonicalize
        if(string.IsNullOrEmpty(ownerName))
        {
            ownerName = "";
        }

        if (string.IsNullOrEmpty(cause))
        {
            cause = "Either the workbook did not have a local metadata file with ownership information, or we could not match the user name on server";
        }

        _manualActions.AddKeyValuePairs
            (
            new string[]
            {
                "action",
                "content",
                "cause",
                "name",
                "desired-owner"
            },
            new string[]
            {
                "Reassign owner of Workbook on server",
                "Workbook",
                 cause,
                 workbookName,
                 ownerName
            }
            );
    }


    /// <summary>
    /// If the file extension has a leading '.', remove it.
    /// </summary>
    /// <param name="txtIn"></param>
    /// <returns></returns>
    private string RemoveFileExtensionDot(string txtIn)
    {
        if (string.IsNullOrWhiteSpace(txtIn)) return "";
        if (txtIn[0] == '.') return txtIn.Substring(1);
        return txtIn;
    }

    /// <summary>
    /// After a file has been uploaded in chunks, we need to make a call to COMMIT the file to server as a published Workbook
    /// </summary>
    /// <param name="uploadSessionId"></param>
    /// <param name="publishedContentName"></param>
    private SiteWorkbook FinalizePublish(
        string uploadSessionId, 
        string publishedContentName, 
        string publishedContentType, 
        string projectId,
        CredentialManager.Credential dbCredentials,
        WorkbookPublishSettings publishSettings)
    {
        //See definition: http://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Publish_Workbook%3FTocPath%3DAPI%2520Reference%7C_____29
        var sb = new StringBuilder();

        //Build the XML part of the MIME message we will post up to server
        var xmlWriter = XmlWriter.Create(sb, XmlHelper.XmlSettingsForWebRequests);
        xmlWriter.WriteStartElement("tsRequest");
            xmlWriter.WriteStartElement("workbook");
            xmlWriter.WriteAttributeString("name", publishedContentName);
            xmlWriter.WriteAttributeString("showTabs", XmlHelper.BoolToXmlText(publishSettings.ShowTabs));

                //If we have an associated database credential, write it out
        if (dbCredentials != null)
                {
                    CredentialXmlHelper.WriteCredential(
                        xmlWriter, 
                        dbCredentials);
                }

                xmlWriter.WriteStartElement("project"); //<project>
                xmlWriter.WriteAttributeString("id", projectId);
                xmlWriter.WriteEndElement();            //</project>
            xmlWriter.WriteEndElement(); // </workbook>
            //Currently not supporting <connectionCredentials>
        xmlWriter.WriteEndElement(); // </tsRequest>
        xmlWriter.Close();

        var xmlText = sb.ToString(); //Get the XML text out

        //Generate the MIME message and pack the XML into it
        var mimeGenerator = new MimeWriterXml(xmlText);

        //Create a web request to POST the MIME message to server to finalize the publish
        var urlFinalizeUpload = _onlineUrls.Url_FinalizeWorkbookPublish(_onlineSession, uploadSessionId, publishedContentType);

        //NOTE: The publish finalization step can take several minutes, because server needs to unpack the uploaded ZIP and file it away.
        //      For this reason, we pass in a long timeout
        var webRequest = this.CreateAndSendMimeLoggedInRequest(urlFinalizeUpload, "POST", mimeGenerator, TableauServerWebClient.DefaultLongRequestTimeOutMs); 
        var response = GetWebReponseLogErrors(webRequest, "finalize workbook publish");
        using (response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            //Get all the workbook nodes
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var workbookXml = xmlDoc.SelectSingleNode("//iwsOnline:workbook", nsManager);

            try
            {
                return new SiteWorkbook(workbookXml);
            }
            catch(Exception parseXml)
            {
                StatusLog.AddError("Workbook upload, error parsing XML response " + parseXml.Message + "\r\n" + workbookXml.InnerXml);
                return null;
            }
        }
    }
}
