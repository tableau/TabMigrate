using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Downloads a set of workbooks from server
/// </summary>
class DownloadWorkbooks : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Workbooks we've parsed from server results
    /// </summary>
    private readonly IEnumerable<SiteWorkbook> _workbooks;

    /// <summary>
    /// Local path where we are going to save downloaded workbooks to
    /// </summary>
    private readonly string _localSavePath;

    /// <summary>
    /// If not NULL, put downloads into directories named like the projects they belong to
    /// </summary>
    private readonly IProjectsList _downloadToProjectDirectories;

    /// <summary>
    /// If TRUE a companion XML file will be generated for each downloaded Workbook with additional
    /// information about it that is useful for uploads
    /// </summary>
    private readonly bool _generateInfoFile;

    /// <summary>
    /// May be NULL.  If not null, this is the list of sites users, so we can look up the user name
    /// </summary>
    private readonly KeyedLookup<SiteUser> _siteUserLookup;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    /// <param name="workbooks"></param>
    /// <param name="localSavePath"></param>
    /// <param name="projectsList"></param>
    /// <param name="generateInfoFile">TRUE = Generate companion file for each download that contains metadata (e.g. whether "show tabs" is selected, the owner, etc)</param>
    /// <param name="siteUsersLookup">If not NULL, use to look up the owners name, so we can write it into the InfoFile for the downloaded content</param>
    public DownloadWorkbooks(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login, 
        IEnumerable<SiteWorkbook> workbooks,
        string localSavePath,
        IProjectsList projectsList,
        bool generateInfoFile,
        KeyedLookup<SiteUser> siteUserLookup)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _workbooks = workbooks;
        _localSavePath = localSavePath;
        _downloadToProjectDirectories = projectsList;
        _generateInfoFile = generateInfoFile;
        _siteUserLookup = siteUserLookup;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public ICollection<SiteWorkbook> ExecuteRequest()
    {
        var statusLog = _onlineSession.StatusLog;
        var downloadedContent = new List<SiteWorkbook>();

        var workbooks = _workbooks;
        if (workbooks == null)
        {
            statusLog.AddError("NULL workbooks. Aborting download.");
            return null;
        }

        //Depending on the HTTP download file type we want different file extensions
        var typeMapper = new DownloadPayloadTypeHelper("twbx", "twb");

        foreach (var contentInfo in workbooks)
        {
            //Local path save the workbook
            string urlDownload = _onlineUrls.Url_WorkbookDownload(_onlineSession, contentInfo);
            statusLog.AddStatus("Starting Workbook download " + contentInfo.Name + " " + contentInfo.ToString());
            try
            {
                //Generate the directory name we want to download into
                var pathToSaveTo = FileIOHelper.EnsureProjectBasedPath(
                    _localSavePath, 
                    _downloadToProjectDirectories, 
                    contentInfo, 
                    this.StatusLog);

                var fileDownloaded = this.DownloadFile(urlDownload, pathToSaveTo, contentInfo.Name, typeMapper);
                var fileDownloadedNoPath = System.IO.Path.GetFileName(fileDownloaded);
                statusLog.AddStatus("Finished Workbook download " + fileDownloadedNoPath);

                //Add to the list of our downloaded workbooks, and save metadata
                if (!string.IsNullOrWhiteSpace(fileDownloaded))
                {
                    downloadedContent.Add(contentInfo);

                    //Generate the metadata file that has additional server provided information about the workbook
                    if(_generateInfoFile)
                    {
                        WorkbookPublishSettings.CreateSettingsFile(contentInfo, fileDownloaded, _siteUserLookup);
                    }
                }
                else
                {
                    //We should never hit this code; just being defensive
                    statusLog.AddError("Download error, no local file path for downloaded content");
                }
            }
            catch (Exception ex)
            {
                statusLog.AddError("Error during Workbook download " + contentInfo.Name + "\r\n  " + urlDownload + "\r\n  " + ex.ToString());
            }
        } //foreach

        return downloadedContent;
    }

}
