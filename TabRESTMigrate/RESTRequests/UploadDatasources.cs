using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Uploads all the Data Sources in a directory tree
/// </summary>
class UploadDatasources : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _localUploadPath;
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
    /// <param name="localUploadPath"></param>
    public UploadDatasources(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        CredentialManager credentialManager,
        string localUploadPath,
        UploadBehaviorProjects uploadProjectBehavior,
        CustomerManualActionManager manualActions,
        bool attemptOwnershipAssignment,
        IEnumerable<SiteUser> siteUsers,
        int uploadChunkSizeBytes = TableauServerUrls.UploadFileChunkSize,
        int uploadChunkDelaySeconds = 0)
        : base(login)
    {
        System.Diagnostics.Debug.Assert(uploadChunkSizeBytes > 0, "Chunck size must be positive");

        _onlineUrls = onlineUrls;
        _localUploadPath = localUploadPath;
        _uploadProjectBehavior = uploadProjectBehavior;
        _credentialManager = credentialManager;
        _manualActions = manualActions;
        if(_manualActions == null)
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
    /// Upload all the datasource files
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        //Helper object to track project Ids and create projects as needed
        var projectListHelper = new ProjectFindCreateHelper(_onlineUrls, this._onlineSession, _uploadProjectBehavior);

        var statusLog = this.StatusLog;
        int countSuccess = 0;
        int countFailure = 0;

        statusLog.AddStatus("Uploading datasources");
        UploadDirectoryToServer(_localUploadPath, _localUploadPath, projectListHelper, true, out countSuccess, out countFailure);
        this.StatusLog.AddStatus("Datasources upload done.  Success: " + countSuccess.ToString() + ", Failure: " + countFailure.ToString());
    }

    /// <summary>
    /// Uploads the contents of a directory to server
    /// </summary>
    /// <param name="rootContentPath"></param>
    /// <param name="currentContentPath"></param>
    /// <param name="recurseDirectories"></param>
    private void UploadDirectoryToServer(
        string rootContentPath, 
        string currentContentPath, 
        ProjectFindCreateHelper projectsList, 
        bool recurseDirectories, 
        out int countSuccess, 
        out int countFailure)
    {
        countSuccess = 0;
        countFailure = 0;

        //Look up the project name based on directory name, and creating a project on demand
        string projectName;
        if(rootContentPath == currentContentPath) //If we are in the root upload directory, then assume any content goes to the Default project
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
                    var publishSettings = DatasourcePublishSettings.GetSettingsForSavedDatasource(thisFilePath);

                    //Do the file upload
                    bool wasFileUploaded = AttemptUploadSingleFile(thisFilePath, projectIdForUploads, dbCredentialsIfAny, publishSettings);
                    if (wasFileUploaded) { countSuccess++; }
                }
                catch (Exception ex)
                {
                    countFailure++;
                    StatusLog.AddError("Error uploading datasource " + thisFilePath + ". " + ex.Message);
                    LogManualAction_UploadDataSource(thisFilePath);
                }
            }
        }

        //If we are running recursive , then look in the subdirectories too
        if (recurseDirectories)
        {
            int subDirSuccess;
            int subDirFailure;
            foreach (var subDirectory in Directory.GetDirectories(currentContentPath))
            {
                UploadDirectoryToServer(rootContentPath, subDirectory, projectsList, true, out subDirSuccess, out subDirFailure);
                countSuccess += subDirSuccess;
                countFailure += subDirFailure;
            }
        }
    }

    /// <summary>
    /// Log a required manual action
    /// </summary>
    /// <param name="filePath"></param>
    private void LogManualAction_UploadDataSource(string filePath)
    {
        //Log a manual action
        _manualActions.AddKeyValuePairs(new string[]
                {
                    "action  : Open datasource in Tableau Desktop and attempt manual upload"
                  , "content : Data Source"
                  , "cause   : Possibly the datasource requires an OAuth credential to be uploaded"
                  , "path    : " + filePath
                });
    }

    /// <summary>
    /// Sanity testing on whether the file being uploaded is worth uploading
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <returns></returns>
    bool IsValidUploadFile(string localFilePath)
    {
        //If the file is a custom settings file for the workbook, then ignore it
        if (DatasourcePublishSettings.IsSettingsFile(localFilePath))
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

        //These are the only kinds of data sources we know about...
        if((fileExtension != ".tds") && (fileExtension != ".tdsx"))
        {
            StatusLog.AddError("File is not a data source: " + localFilePath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to upload a single file a Tableau Server, and then make it a published data source
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <param name="projectId"></param>
    /// <param name="dbCredentials">If not NULL, then these are the DB credentials we want to associate with the content we are uploading</param>
    /// <returns></returns>
    private bool AttemptUploadSingleFile(
        string localFilePath, 
        string projectId,
        CredentialManager.Credential dbCredentials,
        DatasourcePublishSettings publishSettings)
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

        SiteDatasource dataSource = null;
        this.StatusLog.AddStatus("File chunks upload successful. Next step, make it a published datasource", -10);
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(localFilePath);
            string uploadType = RemoveFileExtensionDot(Path.GetExtension(localFilePath).ToLower());
            dataSource = FinalizePublish(
                uploadSessionId,
                FileIOHelper.Undo_GenerateWindowsSafeFilename(fileName), //[2016-05-06] If the name has escapted characters, unescape them
                uploadType, 
                projectId, 
                dbCredentials);
            StatusLog.AddStatus("Upload content details: " + dataSource.ToString(), -10);
            StatusLog.AddStatus("Success! Uploaded datasource " + Path.GetFileName(localFilePath));
        }
        catch (Exception exPublishFinalize)
        {
            this.StatusLog.AddError("Unexpected error finalizing publish of file " + localFilePath + ", " + exPublishFinalize.Message);
            throw exPublishFinalize; ;

        }

        //See if we want to reassign ownership of the datasource
        if (_attemptOwnershipAssignment)
        {
            try
            {
                AttemptOwnerReassignment(dataSource, publishSettings, _siteUsers);
            }
            catch (Exception exOwnershipAssignment)
            {
                this.StatusLog.AddError("Unexpected error reassigning ownership of published datasource " + dataSource.Name + ", " + exOwnershipAssignment.Message);
                LogManualAction_ReassignOwnership(dataSource.Name);
                throw exOwnershipAssignment;
            }
        }

        return true;     //Success
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
    /// After a file has been uploaded in chunks, we need to make a call to COMMIT the file to server as a published Data Source
    /// </summary>
    /// <param name="uploadSessionId"></param>
    /// <param name="publishedContentName"></param>
    private SiteDatasource FinalizePublish(
        string uploadSessionId, 
        string publishedContentName, 
        string publishedContentType, 
        string projectId,
        CredentialManager.Credential dbCredentials)
    {
        //See definition: http://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Publish_Datasource%3FTocPath%3DAPI%2520Reference%7C_____29
        var sb = new StringBuilder();
        var xmlWriter = XmlWriter.Create(sb, XmlHelper.XmlSettingsForWebRequests);
        xmlWriter.WriteStartElement("tsRequest");
            xmlWriter.WriteStartElement("datasource");
            xmlWriter.WriteAttributeString("name", publishedContentName);

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
            xmlWriter.WriteEndElement(); // </datasource>
            //Currently not supporting <connectionCredentials>
        xmlWriter.WriteEndElement(); // </tsRequest>
        xmlWriter.Close();

        var xmlText = sb.ToString(); //Get the XML text out

        //Generate the MIME message
        var mimeGenerator = new MimeWriterXml(xmlText);

        //Create a web request to push the 
        var urlFinalizeUpload = _onlineUrls.Url_FinalizeDataSourcePublish(_onlineSession, uploadSessionId, publishedContentType);

        //NOTE: The publish finalization step can take several minutes, because server needs to unpack the uploaded ZIP and file it away.
        //      For this reason, we pass in a long timeout
        var webRequest = this.CreateAndSendMimeLoggedInRequest(urlFinalizeUpload, "POST", mimeGenerator, TableauServerWebClient.DefaultLongRequestTimeOutMs); 
        var response = GetWebReponseLogErrors(webRequest, "finalize datasource publish");
        using (response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            //Get all the datasource node from the response
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var dataSourceXml = xmlDoc.SelectSingleNode("//iwsOnline:datasource", nsManager);

            try
            {
                return new SiteDatasource(dataSourceXml);
            }
            catch(Exception parseXml)
            {
                StatusLog.AddError("Data source upload, error parsing XML response " + parseXml.Message + "\r\n" + dataSourceXml.InnerXml);
                return null;
            }
        }
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
        return credentialManager.FindDatasourceCredential(
            contentFileName,
            projectName);
    }

    /// <summary>
    /// Assign ownership
    /// </summary>
    /// <param name="datasource"></param>
    /// <param name="publishSettings"></param>
    /// <param name="siteUsers"></param>
    /// <returns>TRUE: The server content has the correct owner now.  FALSE: We were unable to give the server content the correct owner</returns>
    private bool AttemptOwnerReassignment(SiteDatasource datasource, DatasourcePublishSettings publishSettings, IEnumerable<SiteUser> siteUsers)
    {
        this.StatusLog.AddStatusHeader("Attempting ownership assignement for Datasource " + datasource.Name + "/" + datasource.Id);

        //Something went wrong if we don't have a set of site users to do the look up
        if (siteUsers == null)
        {
            throw new ArgumentException("No set of site users provided for lookup");
        }

        //Look the local meta data to see what the desired name is
        var desiredOwnerName = publishSettings.OwnerName;
        if (string.IsNullOrEmpty(desiredOwnerName))
        {
            this.StatusLog.AddStatus("Skipping owner assignment. The local file system has no metadata with an owner information for " + datasource.Name);
            LogManualAction_ReassignOwnership(datasource.Name, "none specified", "No client ownership information was specified");
            return true; //Since there is no ownership preference stated locally, then ownership we assigned during upload was fine.
        }

        //Look on the list of users in the target site/server, and try to find a match
        //
        //NOTE: We are doing a CASE INSENSITIVE name comparison. We assume that there are not 2 users with the same name on server w/differet cases
        //      Because if this, we want to be flexible and allow that our source/destination servers may have the user name specified with a differnt 
        //      case.  -- This is less secure than a case-sensitive comparison, but is almost always what we want when porting content between servers
        var desiredServerUser = SiteUser.FindUserWithName(siteUsers, desiredOwnerName, StringComparison.InvariantCultureIgnoreCase);

        if (desiredServerUser == null)
        {
            this.StatusLog.AddError("The local file has a workbook/user mapping: " + datasource.Name + "/" + desiredOwnerName + ", but this user does not exist on the target site");
            LogManualAction_ReassignOwnership(datasource.Name, desiredOwnerName, "The target site does not contain a user name that matches the owner specified by the local metadata");
            return false; //Not a run time error, but we have manual steps to perform
        }

        //If the server content is already correct, then there is nothing to do
        if (desiredServerUser.Id == datasource.OwnerId)
        {
            this.StatusLog.AddStatus("Workbook " + datasource.Name + "/" + datasource.Id + ", already has correct ownership. No update requried");
            return true;
        }

        //Lets tell server to update the owner
        var changeOwnership = new SendUpdateDatasourceOwner(_onlineUrls, _onlineSession, datasource.Id, desiredServerUser.Id);
        SiteDatasource updatedDatasource;
        try
        {
            this.StatusLog.AddStatus("Server request to change Datasource ownership, ds: " + datasource.Name + "/" + datasource.Id + ", user:" + desiredServerUser.Name + "/" + desiredServerUser.Id);
            updatedDatasource = changeOwnership.ExecuteRequest();
        }
        catch (Exception exChangeOnwnerhsip)
        {
            throw exChangeOnwnerhsip; //Unexpected error, send it upward
        }

        //Sanity check the result we got back: Double check to make sure we have the expected owner.
        if (updatedDatasource.OwnerId != desiredServerUser.Id)
        {
            this.StatusLog.AddError("Unexpected server error! Updated workbook Owner Id does not match expected. ds: "
                + datasource.Name + "/" + datasource.Id + ", "
                + "expected user: " + desiredServerUser.Id + ", "
                + "actual user: " + updatedDatasource.OwnerId
                );
        }

        return true;
    }

    /// <summary>
    /// A manual step will be required to set the owner of the content
    /// </summary>
    /// <param name="filePath"></param>
    private void LogManualAction_ReassignOwnership(string datasourceName, string ownerName = "", string cause = "")
    {
        //Cannonicalize
        if (string.IsNullOrEmpty(ownerName))
        {
            ownerName = "";
        }

        if (string.IsNullOrEmpty(cause))
        {
            cause = "Either the datasource did not have a local metadata file with ownership information, or we could not match the user name on server";
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
                "Datasource",
                 cause,
                 datasourceName,
                 ownerName
            }
            );
    }

}

