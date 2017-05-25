using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Creates the set of server specific URLs
/// </summary>
class TableauServerUrls : ITableauServerSiteInfo
{
    private readonly ServerVersion _serverVersion;
    /// <summary>
    /// What version of Server do we thing we are talking to? (URLs and APIs may differ)
    /// </summary>
    public ServerVersion ServerVersion
    {
        get
        {
            return _serverVersion;
        }
    }

    /// <summary>
    /// Url for API login
    /// </summary>
    public readonly string UrlLogin;

    /// <summary>
    /// Url for log out
    /// </summary>
    public readonly string UrlLogout;

    /// <summary>
    /// Template for URL to acess workbooks list
    /// </summary>
    private readonly string _urlListWorkbooksForUserTemplate;
    private readonly string _urlListWorkbookConnectionsTemplate;
    private readonly string _urlListDatasourcesTemplate;
    private readonly string _urlListProjectsTemplate;
    private readonly string _urlListSubscriptionsTemplate;
    private readonly string _urlListSchedulesTemplate;
    private readonly string _urlListTasksInScheduleTemplate;
    private readonly string _urlListViewsTemplate;
    private readonly string _urlListGroupsTemplate;
    private readonly string _urlListUsersTemplate;
    private readonly string _urlListUsersInGroupTemplate;
    private readonly string _urlDownloadWorkbookTemplate;
    private readonly string _urlDownloadDatasourceTemplate;
    private readonly string _urlSiteInfoTemplate;
    private readonly string _urlInitiateUploadTemplate;
    private readonly string _urlAppendUploadChunkTemplate;
    private readonly string _urlFinalizeUploadDatasourceTemplate;
    private readonly string _urlFinalizeUploadWorkbookTemplate;
    private readonly string _urlCreateProjectTemplate;
    private readonly string _urlDeleteWorkbookTagTemplate;
    private readonly string _urlDeleteDatasourceTagTemplate;
    private readonly string _urlUpdateWorkbookTemplate;
    private readonly string _urlUpdateDatasourceTemplate;

    /// <summary>
    /// Server url with protocol
    /// </summary>
    public readonly string ServerUrlWithProtocol;
    public readonly string ServerProtocol;

    /// <summary>
    /// Part of the URL that designates the site id
    /// </summary>
    public readonly string SiteUrlSegement;

    public readonly string ServerName;

    public readonly int PageSize = 1000;
    public const int UploadFileChunkSize = 8000000; //8MB
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serverNameWithProtocol"></param>
    /// <param name="siteUrlSegment"></param>
    public TableauServerUrls(string protocol, string serverName, string siteUrlSegment, int pageSize, ServerVersion serverVersion)
    {
        //Cannonicalize the protocol
        protocol = protocol.ToLower();

        this.ServerProtocol = protocol;

        this.PageSize = pageSize;
        string serverNameWithProtocol = protocol + serverName;
        this._serverVersion = serverVersion;
        this.SiteUrlSegement = siteUrlSegment;
        this.ServerName = serverName;
        this.ServerUrlWithProtocol                 = serverNameWithProtocol;
        this.UrlLogin                              = serverNameWithProtocol + "/api/2.3/auth/signin";
        this.UrlLogout                             = serverNameWithProtocol + "/api/2.3/auth/signout";
        this._urlListWorkbooksForUserTemplate      = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/users/{{iwsUserId}}/workbooks?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListWorkbookConnectionsTemplate   = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/workbooks/{{iwsWorkbookId}}/connections";
        this._urlListDatasourcesTemplate           = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/datasources?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListProjectsTemplate              = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/projects?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListSubscriptionsTemplate         = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/subscriptions?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListSchedulesTemplate             = serverNameWithProtocol + "/api/2.3/schedules?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListTasksInScheduleTemplate       = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/schedules/{{iwsScheduleId}}/extracts?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListViewsTemplate                 = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/views?includeUsageStatistics=true&pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListGroupsTemplate                = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/groups?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListUsersTemplate                 = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/users?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}";
        this._urlListUsersInGroupTemplate          = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/groups/{{iwsGroupId}}/users?pageSize={{iwsPageSize}}&pageNumber={{iwsPageNumber}}"; 
        this._urlDownloadDatasourceTemplate        = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/datasources/{{iwsRepositoryId}}/content";
        this._urlDownloadWorkbookTemplate          = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/workbooks/{{iwsRepositoryId}}/content";
        this._urlSiteInfoTemplate                  = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}";
        this._urlInitiateUploadTemplate            = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/fileUploads";
        this._urlAppendUploadChunkTemplate         = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/fileUploads/{{iwsUploadSession}}";
        this._urlFinalizeUploadDatasourceTemplate  = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/datasources?uploadSessionId={{iwsUploadSession}}&datasourceType={{iwsDatasourceType}}&overwrite=true";
        this._urlFinalizeUploadWorkbookTemplate    = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/workbooks?uploadSessionId={{iwsUploadSession}}&workbookType={{iwsWorkbookType}}&overwrite=true";
        this._urlCreateProjectTemplate             = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/projects";
        this._urlDeleteWorkbookTagTemplate         = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/workbooks/{{iwsWorkbookId}}/tags/{{iwsTagText}}";
        this._urlDeleteDatasourceTagTemplate       = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/datasources/{{iwsDatasourceId}}/tags/{{iwsTagText}}";
        this._urlUpdateWorkbookTemplate            = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/workbooks/{{iwsWorkbookId}}";
        this._urlUpdateDatasourceTemplate          = serverNameWithProtocol + "/api/2.3/sites/{{iwsSiteId}}/datasources/{{iwsDatasourceId}}";
        //Any server version specific things we want to do?
        switch (serverVersion)
        {
            case ServerVersion.server8:
                throw new Exception("This app does not support v8 Server");
            case ServerVersion.server9:
                break;
            default:
                AppDiagnostics.Assert(false, "Unknown server version");
                throw new Exception("Unknown server version");
        }
    }

    //Parse out the http:// or https://
    private static string GetProtocolFromUrl(string url)
    {
        const string protocolIndicator = "://";
        int idxProtocol = url.IndexOf(protocolIndicator);
        if(idxProtocol < 1)
        {
            throw new Exception("No protocol found in " + url);
        }

        string protocol = url.Substring(0, idxProtocol + protocolIndicator.Length);

        return protocol.ToLower();
    }

    /// <summary>
    /// Parse out the server-user and site name from the content URL
    /// </summary>
    /// <param name="userContentUrl">e.g. https://online.tableausoftware.com/t/tableausupport/workbooks</param>
    /// 
    /// <returns></returns>
    public static TableauServerUrls FromContentUrl(string userContentUrl, int pageSize)
    {
        userContentUrl = userContentUrl.Trim();
        string foundProtocol = GetProtocolFromUrl(userContentUrl);

        //Find where the server name ends
        string urlAfterProtocol = userContentUrl.Substring(foundProtocol.Length);
        var urlParts = urlAfterProtocol.Split('/');
        string serverName = urlParts[0];

        string siteUrlSegment;
        ServerVersion serverVersion;
        //Check for the site specifier.  Infer the server version based on this URL
        if (urlParts.Length == 1)
        {
            //The user has just specified the root of the server without a path.
            //Therefore, use hte default site.
            siteUrlSegment = ""; //Default site
            serverVersion = ServerVersion.server9;
        }
        else if((urlParts[1] == "#") && (urlParts[2] == "site"))
        {
            siteUrlSegment = urlParts[3];
            serverVersion = ServerVersion.server9;
        }
        else if (urlParts[1] == "#")
        {
            siteUrlSegment = ""; //Default site
            serverVersion = ServerVersion.server9;
        }
        else
        {
            throw new Exception("Site URL not recognized as Tableau Server");
        }
         
        return new TableauServerUrls(foundProtocol, serverName, siteUrlSegment, pageSize, serverVersion);
    }

    /// <summary>
    /// The URL to get site info
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <returns></returns>
    public string Url_SiteInfo(TableauServerSignIn logInInfo)
    {
        string workingText = _urlSiteInfoTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// The URL to start na upload
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <returns></returns>
    public string Url_InitiateFileUpload(TableauServerSignIn logInInfo)
    {
        string workingText = _urlInitiateUploadTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// The URL to start a upload
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <returns></returns>
    public string Url_AppendFileUploadChunk(TableauServerSignIn logInInfo, string uploadSession)
    {
        string workingText = _urlAppendUploadChunkTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        workingText = workingText.Replace("{{iwsUploadSession}}", uploadSession);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }


    /// <summary>
    /// URL to finish publishing a datasource
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <param name="uploadSession"></param>
    /// <param name="datasourceType"></param>
    /// <returns></returns>
    public string Url_FinalizeDataSourcePublish(TableauServerSignIn logInInfo, string uploadSession, string datasourceType)
    {

        string workingText = _urlFinalizeUploadDatasourceTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        workingText = workingText.Replace("{{iwsUploadSession}}", uploadSession);
        workingText = workingText.Replace("{{iwsDatasourceType}}", datasourceType);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL to finish publishing a datasource
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <param name="uploadSession"></param>
    /// <param name="datasourceType"></param>
    /// <returns></returns>
    public string Url_FinalizeWorkbookPublish(TableauServerSignIn logInInfo, string uploadSession, string workbookType)
    {

        string workingText = _urlFinalizeUploadWorkbookTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        workingText = workingText.Replace("{{iwsUploadSession}}", uploadSession);
        workingText = workingText.Replace("{{iwsWorkbookType}}", workbookType);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Workbooks list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_WorkbooksListForUser(TableauServerSignIn session, string userId, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListWorkbooksForUserTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsUserId}}", userId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Workbook's data source connections list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_WorkbookConnectionsList(TableauServerSignIn session, string workbookId)
    {
        string workingText = _urlListWorkbookConnectionsTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsWorkbookId}}", workbookId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for a Datasource's connections list
    /// </summary>
    /// <param name="session"></param>
    /// <param name="datasourceId"></param>
    /// <returns></returns>
    internal string Url_DatasourceConnectionsList(TableauServerSignIn session, string datasourceId)
    {
        throw new NotImplementedException("2015-11-16, Tableau Server does not yet have a REST API to support this call");
    }


    /// <summary>
    /// URL for the Datasources list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_DatasourcesList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListDatasourcesTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for creating a project
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_CreateProject(TableauServerSignIn session)
    {
        string workingText = _urlCreateProjectTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for deleting a tag from a workbook
    /// </summary>
    /// <param name="session"></param>
    /// <param name="workbookId"></param>
    /// <param name="tagText">Tag we want to delete</param>
    /// <returns></returns>
    public string Url_DeleteWorkbookTag(TableauServerSignIn session, string workbookId, string tagText)
    {
        string workingText = _urlDeleteWorkbookTagTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsWorkbookId}}", workbookId);
        workingText = workingText.Replace("{{iwsTagText}}", tagText);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for updating workbook metadata (e.g. owners, show tabs)
    /// </summary>
    /// <param name="session"></param>
    /// <param name="workbookId"></param>
    /// <param name="tagText">Tag we want to delete</param>
    /// <returns></returns>
    public string Url_UpdateWorkbook(TableauServerSignIn session, string workbookId)
    {
        string workingText = _urlUpdateWorkbookTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsWorkbookId}}", workbookId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for updating datasource metadata (e.g. owner id)
    /// </summary>
    /// <param name="session"></param>
    /// <param name="workbookId"></param>
    /// <param name="tagText">Tag we want to delete</param>
    /// <returns></returns>
    public string Url_UpdateDatasource(TableauServerSignIn session, string datasourceId)
    {
        string workingText = _urlUpdateDatasourceTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsDatasourceId}}", datasourceId);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for deleting a tag from a datasource
    /// </summary>
    /// <param name="session"></param>
    /// <param name="workbookId"></param>
    /// <param name="tagText">Tag we want to delete</param>
    /// <returns></returns>
    public string Url_DeleteDatasourceTag(TableauServerSignIn session, string datasourceId, string tagText)
    {
        string workingText = _urlDeleteDatasourceTagTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsDatasourceId}}", datasourceId);
        workingText = workingText.Replace("{{iwsTagText}}", tagText);
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }


    /// <summary>
    /// URL for the Projects list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_ProjectsList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListProjectsTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Subscriptions list
    /// </summary>
    /// <param name="session"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    public string Url_SubscriptionsList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListSubscriptionsTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Schedules list
    /// </summary>
    /// <param name="session"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    public string Url_SchedulesList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListSchedulesTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for a list of tasks inside a schedule
    /// </summary>
    /// <param name="session"></param>
    /// <param name="scheduleId"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    public string Url_TasksExtractRefreshesForScheduleList(TableauServerSignIn session, string scheduleId, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListTasksInScheduleTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsScheduleId}}", scheduleId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Subscriptions list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_ViewsList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListViewsTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Groups list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_GroupsList(TableauServerSignIn session, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListGroupsTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL for the Users list
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_UsersList(TableauServerSignIn logInInfo, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListUsersTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL to get the list of Users in a Group
    /// </summary>
    /// <param name="logInInfo"></param>
    /// <param name="groupId"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    public string Url_UsersListInGroup(TableauServerSignIn logInInfo, string groupId, int pageSize, int pageNumber = 1)
    {
        string workingText = _urlListUsersInGroupTemplate.Replace("{{iwsSiteId}}", logInInfo.SiteId);
        workingText = workingText.Replace("{{iwsGroupId}}", groupId);
        workingText = workingText.Replace("{{iwsPageSize}}", pageSize.ToString());
        workingText = workingText.Replace("{{iwsPageNumber}}", pageNumber.ToString());
        ValidateTemplateReplaceComplete(workingText);

        return workingText;
    }

    /// <summary>
    /// URL to download a workbook
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_WorkbookDownload(TableauServerSignIn session, SiteWorkbook contentInfo)
    {
        string workingText = _urlDownloadWorkbookTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsRepositoryId}}", contentInfo.Id);

        ValidateTemplateReplaceComplete(workingText);
        return workingText;
    }

    /// <summary>
    /// URL to download a datasource
    /// </summary>
    /// <param name="siteUrlSegment"></param>
    /// <returns></returns>
    public string Url_DatasourceDownload(TableauServerSignIn session, SiteDatasource contentInfo)
    {
        string workingText = _urlDownloadDatasourceTemplate;
        workingText = workingText.Replace("{{iwsSiteId}}", session.SiteId);
        workingText = workingText.Replace("{{iwsRepositoryId}}", contentInfo.Id);

        ValidateTemplateReplaceComplete(workingText);
        return workingText;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static bool ValidateTemplateReplaceComplete(string str)
    {
        if (str.Contains("{{iws"))
        {
            AppDiagnostics.Assert(false, "Template has incomplete parts that need to be replaced");
            return false;
        }

        return true;
    }

    string ITableauServerSiteInfo.ServerName
    {
        get 
        {
            return this.ServerName; 
        }
    }

    ServerProtocol ITableauServerSiteInfo.Protocol
    {
        get 
        {
            if (this.ServerProtocol == "https://") return global::ServerProtocol.https;
            if (this.ServerProtocol == "http://") return global::ServerProtocol.http;
            throw new Exception("Unknown protocol " + this.ServerProtocol);
        }
    }

    string ITableauServerSiteInfo.SiteId
    {
        get 
        {
            return this.SiteUrlSegement;
        }
    }

    string ITableauServerSiteInfo.ServerNameWithProtocol
    {
        get
        {
            return this.ServerUrlWithProtocol;
        }
    }

}
