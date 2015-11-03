using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The list of a Tableau Server Site's projects we have downloaded
/// </summary>
class DownloadProjectsList : TableauServerSignedInRequestBase, IProjectsList
{

    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;

    /// <summary>
    /// Projects we've parsed from server results
    /// </summary>
    private List<SiteProject> _projects;
    public IEnumerable<SiteProject> Projects
    {
        get
        {
            var ds = _projects;
            if (ds == null) return null;
            return ds.AsReadOnly();
        }
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public DownloadProjectsList(TableauServerUrls onlineUrls, TableauServerSignIn login)
        : base(login)
    {
        _onlineUrls = onlineUrls;
    }

    /// <summary>
    /// Request the data from Online
    /// </summary>
    /// <param name="serverName"></param>
    public void ExecuteRequest()
    {
        var onlineProjects = new List<SiteProject>();

        int numberPages = 1; //Start with 1 page (we will get an updated value from server)
        //Get subsequent pages
        for (int thisPage = 1; thisPage <= numberPages; thisPage++)
        {
            try
            {
                ExecuteRequest_ForPage(onlineProjects, thisPage, out numberPages);
            }
            catch (Exception exPageRequest)
            {
                StatusLog.AddError("Projects error during page request: " + exPageRequest.Message);
            }
        }

        _projects = onlineProjects;
    }

    /// <summary>
    /// Get a page's worth of Projects listing
    /// </summary>
    /// <param name="onlineProjects"></param>
    /// <param name="pageToRequest">Page # we are requesting (1 based)</param>
    /// <param name="totalNumberPages">Total # of pages of data that Server can return us</param>
    private void ExecuteRequest_ForPage(List<SiteProject> onlineProjects, int pageToRequest, out int totalNumberPages)
    {
        int pageSize = _onlineUrls.PageSize;
        //Create a web request, in including the users logged-in auth information in the request headers
        var urlQuery = _onlineUrls.Url_ProjectsList(_onlineSession, pageSize, pageToRequest);
        var webRequest = CreateLoggedInWebRequest(urlQuery);
        webRequest.Method = "GET";

        _onlineSession.StatusLog.AddStatus("Web request: " + urlQuery, -10);
        var response = GetWebReponseLogErrors(webRequest, "get projects list");
        var xmlDoc = GetWebResponseAsXml(response);

        //Get all the project nodes
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");
        var projects = xmlDoc.SelectNodes("//iwsOnline:project", nsManager);

        //Get information for each of the data sources
        foreach (XmlNode itemXml in projects)
        {
            try
            {
                var proj = new SiteProject(itemXml);
                onlineProjects.Add(proj);

                SanityCheckProject(proj, itemXml);
            }
            catch
            {
                AppDiagnostics.Assert(false, "Project parse error");
                _onlineSession.StatusLog.AddError("Error parsing project: " + itemXml.OuterXml);
            }
        } //end: foreach

        //-------------------------------------------------------------------
        //Get the updated page-count
        //-------------------------------------------------------------------
        totalNumberPages = DownloadPaginationHelper.GetNumberOfPagesFromPagination(
            xmlDoc.SelectSingleNode("//iwsOnline:pagination", nsManager),
            pageSize);
    }

    /// <summary>
    /// Does sanity checking and error logging on missing data in projects
    /// </summary>
    /// <param name="project"></param>
    private void SanityCheckProject(SiteProject project, XmlNode xmlNode)
    {
        if(string.IsNullOrWhiteSpace(project.Id))
        {
            _onlineSession.StatusLog.AddError(project.Name + " is missing a project ID. Not returned from server! xml=" + xmlNode.OuterXml);
        }
    }


    /// <summary>
    /// Finds a project with matching name
    /// </summary>
    /// <param name="findProjectName"></param>
    /// <returns></returns>
    public SiteProject FindProjectWithName(string findProjectName)
    {
        foreach(var proj in _projects)
        {
            if(proj.Name == findProjectName)
            {
                return proj;
            }
        }

        return null; //Not found
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    SiteProject IProjectsList.FindProjectWithId(string projectId)
    {
        foreach(var proj in _projects)
        {
            if (proj.Id == projectId) { return proj; }
        }

        return null;
    }

    /// <summary>
    /// Adds a project to the list
    /// </summary>
    /// <param name="newProject"></param>
    internal void AddProject(SiteProject newProject)
    {
        _projects.Add(newProject);
    }
}
