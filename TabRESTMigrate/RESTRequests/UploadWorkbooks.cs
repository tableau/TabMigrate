using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Uploads a directory full of workbooks 
/// </summary>
class UploadWorkbooks : TableauServerSignedInRequestBase
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
            projectName = FileIOHelper.ReverseGenerateWindowsSafeFilename(Path.GetFileName(currentContentPath));
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

                    bool wasFileUploaded = AttemptUploadSingleFile(thisFilePath, projectIdForUploads, dbCredentialsIfAny);
                    if (wasFileUploaded) { countSuccess++; }
                }
                catch (Exception ex)
                {
                    countFailure++;
                    StatusLog.AddError("Error uploading workbook " + thisFilePath + ". " + ex.Message);
                }
            }
        }

        //If we are recuring, then look in the subdirectories too
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
    /// Sanity testing on whether the file being uploaded is worth uploading
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <returns></returns>
    bool IsValidUploadFile(string localFilePath)
    {
        //Ignore temp files, since we know we don't want to upload them
        var fileExtension = Path.GetExtension(localFilePath).ToLower();
        if ((fileExtension == ".tmp") || (fileExtension == ".temp"))
        {
            StatusLog.AddStatus("Ignoring temp file, " + localFilePath, -10);
            return false;
        }

        //These are the only kinds of data sources we know about...
        if ((fileExtension != ".twb") && (fileExtension != ".twbx"))
        {
            StatusLog.AddError("File is not a data source: " + localFilePath);
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
    private bool AttemptUploadSingleFile(
        string thisFilePath, 
        string projectIdForUploads,
        CredentialManager.Credential dbCredentials)
    {
        //Assume it's a file we should try to upload
        if (_remapWorkbookReferences)
        {
            return AttemptUploadSingleFile_ReferencesRemapped(thisFilePath, projectIdForUploads, dbCredentials);
        }
        else
        {
            return AttemptUploadSingleFile_Inner(thisFilePath, projectIdForUploads, dbCredentials);
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
        CredentialManager.Credential dbCredentials)
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
            success = AttemptUploadSingleFile_Inner(pathToRemapFile, projectIdForUploads, dbCredentials);
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
            success = AttemptUploadSingleFile_Inner(pathTwbxRemappedOutput, projectIdForUploads, dbCredentials);

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
        CredentialManager.Credential dbCredentials)
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

        this.StatusLog.AddStatus("File chunks upload successful. Next step, make it a published workbook", -10);
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(localFilePath);
            string uploadType = RemoveFileExtensionDot(Path.GetExtension(localFilePath).ToLower());
            var workbook = FinalizePublish(uploadSessionId, fileName, uploadType, projectId, dbCredentials);
            StatusLog.AddStatus("Upload content details: " + workbook.ToString(), -10);
            StatusLog.AddStatus("Success! Uploaded workbook " + Path.GetFileName(localFilePath));
        }
        catch (Exception exPublishFinalize)
        {
            this.StatusLog.AddError("Unexpected error finalizing publish of file " + localFilePath + ", " + exPublishFinalize.Message);
            LogManualAction_UploadWorkbook(localFilePath);
            throw exPublishFinalize;
        }
        return true;     //Success
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
        CredentialManager.Credential dbCredentials)
    {
        //See definition: http://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_ref.htm#Publish_Workbook%3FTocPath%3DAPI%2520Reference%7C_____29
        var sb = new StringBuilder();

        //Build the XML part of the MIME message we will post up to server
        var xmlSettings = new XmlWriterSettings();
        xmlSettings.OmitXmlDeclaration = true;
        var xmlWriter = XmlWriter.Create(sb, xmlSettings);
        xmlWriter.WriteStartElement("tsRequest");
            xmlWriter.WriteStartElement("workbook");
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
                StatusLog.AddError("Workbook upload, error parsing XML resposne " + parseXml.Message + "\r\n" + workbookXml.InnerXml);
                return null;
            }
        }
    }
}
