using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Manages the download of a set of data sources from a Tableau Server site
/// </summary>
class DownloadDatasources : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Datasources we've parsed from server results
    /// </summary>
    private readonly IEnumerable<SiteDatasource> _datasources;

    /// <summary>
    /// Local directory to save to
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
    /// <param name="Datasources"></param>
    /// <param name="localSavePath"></param>
    /// <param name="projectsList"></param>
    /// <param name="generateInfoFile">TRUE = Generate companion file for each download that contains metadata (e.g. whether "show tabs" is selected, the owner, etc)</param>
    /// <param name="siteUsersLookup">If not NULL, use to look up the owners name, so we can write it into the InfoFile for the downloaded content</param>
    public DownloadDatasources(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login, 
        IEnumerable<SiteDatasource> Datasources,
        string localSavePath,
        IProjectsList projectsList,
        bool generateInfoFile,
        KeyedLookup<SiteUser> siteUserLookup)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _datasources = Datasources;
        _localSavePath = localSavePath;
        _downloadToProjectDirectories = projectsList;
        _generateInfoFile = generateInfoFile;
        _siteUserLookup = siteUserLookup;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public List<SiteDatasource> ExecuteRequest()
    {
        var statusLog = _onlineSession.StatusLog;
        var downloadedContent = new List<SiteDatasource>();

        //Depending on the HTTP download file type we want different file extensions
        var typeMapper = new DownloadPayloadTypeHelper("tdsx", "tds");

        var datasources = _datasources;
        if (datasources == null)
        {
            statusLog.AddError("NULL datasources. Aborting download.");
            return null;
        }

        //For each datasource, download it and save it to the local file system
        foreach (var dsInfo in datasources)
        {
            //Local path save the workbook
            string urlDownload = _onlineUrls.Url_DatasourceDownload(_onlineSession, dsInfo);
            statusLog.AddStatus("Starting Datasource download " + dsInfo.Name);
            try
            {
                //Generate the directory name we want to download into
                var pathToSaveTo = FileIOHelper.EnsureProjectBasedPath(
                    _localSavePath,
                    _downloadToProjectDirectories,
                    dsInfo,
                    this.StatusLog);

                var fileDownloaded = this.DownloadFile(urlDownload, pathToSaveTo, dsInfo.Name, typeMapper);
                var fileDownloadedNoPath = System.IO.Path.GetFileName(fileDownloaded);
                statusLog.AddStatus("Finished Datasource download " + fileDownloadedNoPath);

                //Add to the list of our downloaded data sources
                if(!string.IsNullOrEmpty(fileDownloaded))
                {
                    downloadedContent.Add(dsInfo);

                    //Generate the metadata file that has additional server provided information about the workbook
                    if (_generateInfoFile)
                    {
                        DatasourcePublishSettings.CreateSettingsFile(dsInfo, fileDownloaded, _siteUserLookup);
                    }
                }
                else
                {
                    //We should never hit this code; just being defensive
                    statusLog.AddError("Download error, no local file path for downloaded content");
                }
            }
            catch(Exception ex)
            {
                statusLog.AddError("Error during Datasource download " + dsInfo.Name + "\r\n  " + urlDownload + "\r\n  " + ex.ToString());
            }
        } //foreach


        //Return the set of successfully downloaded content
        return downloadedContent;
    }
}
