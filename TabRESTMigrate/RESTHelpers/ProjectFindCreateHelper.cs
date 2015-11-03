using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Helper class for managing access to project Ids and creating server site projects on demand
/// </summary>
class ProjectFindCreateHelper : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly UploadBehaviorProjects _uploadProjectBehavior;
    private readonly DownloadProjectsList _projectsList;
    //    private readonly 
        /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="onlineUrls"></param>
    /// <param name="login"></param>
    public ProjectFindCreateHelper(
        TableauServerUrls onlineUrls, 
        TableauServerSignIn login,
        UploadBehaviorProjects uploadProjectBehavior)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _uploadProjectBehavior = uploadProjectBehavior;

        //Ask server for the list of projects
        var projectsList = new DownloadProjectsList(_onlineUrls, _onlineSession);
        projectsList.ExecuteRequest();
        _projectsList = projectsList;
    }


    /// <summary>
    /// Looks up the default project ID
    /// </summary>
    /// <returns></returns>
    public string GetProjectIdForUploads(string projectName)
    {
        //If the project name is empty - look for the default project
        if (string.IsNullOrEmpty(projectName))
        {
            goto find_default_project;
        }

        //Look for the matching project
        var project = _projectsList.FindProjectWithName(projectName);
        if (project != null)
        {
            return project.Id;
        }

        //If the option is specified; attempt to create the project
        if (_uploadProjectBehavior.AttemptProjectCreate)
        {
            //Create the project name
            var createProject = new SendCreateProject(_onlineUrls, _onlineSession, projectName);
            try
            {
                var newProject = createProject.ExecuteRequest();
                _projectsList.AddProject(newProject);
                return newProject.Id;
            }
            catch (Exception exCreateProject)
            {
                this.StatusLog.AddError("Failed attempting to create project '" + projectName + "', " + exCreateProject.Message);
            }
        }

        //If we are not allowed to fall back the default project then error
        if (!_uploadProjectBehavior.UseDefaultProjectIfNeeded)
        {
            throw new Exception("Not allowed to use default project");
        }

        this.StatusLog.AddStatus("Project name not found '" + projectName + "'. Reverting to default project", -10);

    find_default_project:
        //If all else fails, fall back to using the default project
        var defaultProject = _projectsList.FindProjectWithName("default"); //Find the default project
        if (defaultProject != null) return defaultProject.Id;

        defaultProject = _projectsList.FindProjectWithName("Default"); 
        if (defaultProject != null) return defaultProject.Id;

        defaultProject = _projectsList.FindProjectWithName(""); //Try empty
        if (defaultProject != null) return defaultProject.Id;

        //Default project not found. Choosing any project
        this.StatusLog.AddError("Default project not found. Reverting to any project");
        foreach (var thisProj in _projectsList.Projects)
        {
            return thisProj.Id;
        }

        StatusLog.AddError("Upload could not find a project ID to use");
        throw new Exception("Aborting. Upload Datasources could not find a project ID to use");
    }



}
