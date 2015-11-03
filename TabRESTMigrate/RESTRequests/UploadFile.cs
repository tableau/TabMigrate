using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Uploads a single file to the server...
/// </summary>
class UploadFile : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    private readonly string _localUploadPath;
    private readonly int _uploadChunkSize;
    private readonly int _uploadChunkDelay;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="localUploadPath"></param>
    /// <param name="uploadChunkSize"></param>
    /// <param name="uploadChunkDelay"></param>
    public UploadFile(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        string localUploadPath,
        int uploadChunkSize = 8000000,
        int uploadChunkDelay = 0)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _localUploadPath = localUploadPath;
        _uploadChunkSize = uploadChunkSize;
        _uploadChunkDelay = uploadChunkDelay;

    }

    /// <summary>
    /// Uploads a single file to a Tableau Server
    /// </summary>
    /// <returns>The ID of the uploaded file; to be used in subsequent calls</returns>
    public string ExecuteRequest()
    {
        TimeSpan uploadDuration;
        return ExecuteRequest(out uploadDuration);

    }
    /// <summary>
    /// Uploads a single file to a Tableau Server
    /// </summary>
    /// <returns>The ID of the uploaded file; to be used in subsequent calls</returns>
    public string ExecuteRequest(out TimeSpan uploadDuration)
    {
        DateTime uploadStartTime;
        DateTime uploadEndTime;

        var statusLog = this.StatusLog;
        string fileToUpload = _localUploadPath;

        //Sanity check.
        if(!File.Exists(fileToUpload))
        {
            statusLog.AddError("Aborting. Could not find file " + _localUploadPath);
            uploadDuration = TimeSpan.FromSeconds(0);
            return null;
        }

        uploadStartTime = DateTime.Now;
        statusLog.AddStatus("Intiating file upload " + fileToUpload);
        var uploadSessionId = RequestUploadSessionId();

        UploadFileInChunks(fileToUpload, uploadSessionId);
        //Determine the upload duration
        uploadEndTime = DateTime.Now;
        uploadDuration = uploadEndTime - uploadStartTime;
        return uploadSessionId;
    }

    /// <summary>
    /// Uploads a file in N-MB chunks to a Tableau Server
    /// </summary>
    /// <param name="fileToUpload"></param>
    /// <param name="uploadSessionId">Upload Session ID to use</param>
    private void UploadFileInChunks(string fileToUpload, string uploadSessionId)
    {
//        const int max_chunk_size = 8 * 1000000; //N MB
        int max_chunk_size = _uploadChunkSize;
        System.Diagnostics.Debug.Assert(max_chunk_size > 0, "Non positive chunk size");

        byte[] readbuffer = new byte[max_chunk_size];
        var openFile = File.OpenRead(fileToUpload);
        using(openFile)
        {
            int readBytes;
            do 
            {
                readBytes = openFile.Read(readbuffer, 0, max_chunk_size);
                if (readBytes > 0)
                {
                    UploadSingleChunk(uploadSessionId, readbuffer, readBytes);
                }

                ConsiderSleepDelay(); //See if we have an enforced sleep delay
            } while(readBytes > 0);
            openFile.Close();
        }
    }

    /// <summary>
    /// See if we have an enforced sleep delay
    /// </summary>
    private void ConsiderSleepDelay()
    {
        //See if we have a testing delay we want to do after every chunk
        int uploadChunkDelay = _uploadChunkDelay;
        if (uploadChunkDelay > 0)
        {
            this.StatusLog.AddStatus("Forced delay " + uploadChunkDelay + " seconds...");
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(uploadChunkDelay));
        }
    }

    /// <summary>
    /// Uploads a single chunk
    /// </summary>
    /// <param name="uploadSessionId"></param>
    private void UploadSingleChunk(string uploadSessionId, byte [] uploadDataBuffer, int numBytes)
    {
        var urlAppendChunk = _onlineUrls.Url_AppendFileUploadChunk(_onlineSession, uploadSessionId);

        var uploadChunkAsMime = new MimeWriterFileUploadChunk(uploadDataBuffer, numBytes);
        var webRequest = this.CreateAndSendMimeLoggedInRequest(urlAppendChunk, "PUT", uploadChunkAsMime); //NOTE: This command requires a PUT not a GET
        var response = this.GetWebReponseLogErrors(webRequest, "upload file chunk");
        using(response)
        {
            var xmlDoc = GetWebResponseAsXml(response);

            //Get all the workbook nodes
            var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
            var chunkUploadXml = xmlDoc.SelectSingleNode("//iwsOnline:fileUpload", nsManager);
            var verifySessionId = chunkUploadXml.Attributes["uploadSessionId"].Value;
            var fileSizeMB = chunkUploadXml.Attributes["fileSize"].Value;

            if(verifySessionId != uploadSessionId)
            {
                this.StatusLog.AddError("Upload sessions do not match! " + uploadSessionId + "/" + verifySessionId);
            }

            //Log verbose status
            this.StatusLog.AddStatus("Upload chunk status " + verifySessionId + " / " + fileSizeMB + " MB", -10);
        }

    }


    /// <summary>
    /// Get an upload sessiosn Id
    /// </summary>
    /// <returns></returns>
    private string RequestUploadSessionId()
    {
        var urlInitiateFileUpload = _onlineUrls.Url_InitiateFileUpload(_onlineSession);

        var webRequest = this.CreateLoggedInWebRequest(urlInitiateFileUpload, "POST"); //NOTE: This command requires a POST not a GET
        var response = GetWebReponseLogErrors(webRequest, "get datasources list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the workbook nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var uploadInfo = xmlDoc.SelectSingleNode("//iwsOnline:fileUpload", nsManager);
        var sessionId = uploadInfo.Attributes["uploadSessionId"].Value;

        AppDiagnostics.Assert(!string.IsNullOrWhiteSpace(sessionId), "Empty upload session id?");
        return sessionId;
    }
}
