using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Management class for site inventory
/// </summary>
class CustomerSiteInventory : CsvDataGenerator
{
    const string ContentType = "content-type";
    const string ContentUrl = "content-url";
    const string ContentConnectionServer = "connection-server";
    const string ContentConnectionType = "connection-type";
    const string ContentConnectionPort = "connection-port";
    const string ContentConnectionUserName = "connection-user-name";
    const string ContentProjectId = "project-id";
    const string ContentWorkbookId = "workbook-id";
    const string ContentViewCount = "view-count";
    const string ContentWorkbookName = "workbook-name";
    const string ContentViewId = "view-id";
    const string ContentProjectName = "project-name";
    const string ContentUserId = "user-id";
    const string ContentUserName= "user-name";
    const string ContentGroupId = "group-id";
    const string ContentGroupName = "group-name";
    const string ContentId = "id";
    const string ContentName = "name";
    const string ContentDescription = "description";
    const string ContentOwnerId = "owner-id";
    const string ContentOwnerName = "owner-name";
    const string ContentTags = "tags";
    const string ContentSubscriptionId = "subscription-id";
    const string ContentScheduleId = "schedule-id";
    const string ContentScheduleName = "schedule-name";
    const string ContentSubscriptionType = "subscription-type";
    const string WorkbookShowTabs = "workbook-show-tabs";
    const string SiteRole = "user-role";
    const string DeveloperNotes = "developer-notes";

    /// <summary>
    /// Efficent store for looking up user names
    /// </summary>
    private readonly KeyedLookup<SiteUser> _siteUserMapping;

    /// <summary>
    /// Efficent store for looking up views by ID
    /// </summary>
    private readonly KeyedLookup<SiteView> _siteViewMapping;

    /// <summary>
    /// Efficent store for looking up workbooks by ID
    /// </summary>
    private readonly KeyedLookup<SiteWorkbook> _siteWorkbookMapping;

    /// <summary>
    /// Status log data
    /// </summary>
    public readonly TaskStatusLogs StatusLog;

    /// <summary>
    /// Constructor.  Builds the data for the CSV file
    /// </summary>
    /// <param name="projects"></param>
    /// <param name="dataSources"></param>
    /// <param name="workbooks"></param>
    /// <param name="users"></param>
    /// <param name="groups"></param>
    public CustomerSiteInventory(
      IEnumerable<SiteProject> projects, 
      IEnumerable<SiteDatasource> dataSources,
      IEnumerable<SiteWorkbook> workbooks,
      IEnumerable<SiteView> views,
      IEnumerable<SiteUser> users,
      IEnumerable<SiteGroup> groups,
      IEnumerable<SiteSubscription> subscriptions,
      TaskStatusLogs statusLogger)
  {
        //Somewhere to store status logs
        if (statusLogger == null)
        {
            statusLogger = new TaskStatusLogs();
        }
        this.StatusLog = statusLogger;

        //If we have a user-set, put it into a lookup class so we can quickly look up user names when we write out other data
        //that has user ids
        if(users != null)
        {
            _siteUserMapping = new KeyedLookup<SiteUser>(users, statusLogger);
        }

        //Want to be able to map views to workbooks (e.g. in subscriptions)
        if(views != null)
        {
            _siteViewMapping = new KeyedLookup<SiteView>(views, statusLogger);
        }

        //Want to be able to map workbooks to workbook names (e.g. in subscriptions)
        if (workbooks != null)
        {
            _siteWorkbookMapping = new KeyedLookup<SiteWorkbook>(workbooks, statusLogger);
        }

      AddProjectsData(projects);
      AddDatasourcesData(dataSources);
      AddWorkbooksData(workbooks);
      AddUsersData(users);
      AddGroupsData(groups);
      AddViewsData(views);
      AddSubscriptionsData(subscriptions);
  }

    /// <summary>
    /// Attempt to look up the name of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private string helper_AttemptUserNameLookup(string userId)
    {
        try
        {
            return helper_AttemptUserNameLookup_inner(userId);
        }
        catch(Exception ex)
        {
            this.StatusLog.AddError("Error looking up user id, " + ex.Message);
            return "** Error in user lookup **"; //Continue onward
        }
    }

    /// <summary>
    /// Attempt to look up the name of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private string helper_AttemptUserNameLookup_inner(string userId)
    {
        var userIdMap = _siteUserMapping;
        //If we have no user mapping, then return a blank
        if(userIdMap == null)
        {
            return "";
        }

        //We always expect to find the user
        var user = userIdMap.FindItem(userId);
        if(user == null)
        {
            throw new Exception("User ID cannot be mapped '" + userId + "'");
        }
        return user.Name;
    }


    /// <summary>
    /// If we have cached views, look them up
    /// </summary>
    /// <param name="viewId"></param>
    /// <returns></returns>
    private SiteView helper_AttemptViewLookup(string viewId)
    {
        if (_siteViewMapping == null) return null;
        return _siteViewMapping.FindItem(viewId);
    }

    /// <summary>
    /// If we have cached workbooks, look them up
    /// </summary>
    /// <param name="viewId"></param>
    /// <returns></returns>
    private SiteWorkbook helper_AttemptWorkbookLookup(string workbookId)
    {
        if (_siteWorkbookMapping == null) return null;
        return _siteWorkbookMapping.FindItem(workbookId);
    }

    /// <summary>
    /// Add CSV for all the data sources
    /// </summary>
    /// <param name="dataSources"></param>
    private void AddDatasourcesData(IEnumerable<SiteDatasource> dataSources)
  {
      //No data sources? Do nothing.
      if (dataSources == null) return;

      //Add each data source as a row in the CSV file we will generate
      foreach (var thisDatasource in dataSources)
      {
          //Attempt to look up the owner name.  This will be blank if we do not have a users list
          string ownerName = helper_AttemptUserNameLookup(thisDatasource.OwnerId);

          this.AddKeyValuePairs(
              new string[] { 
                   ContentType         //1
                  ,ContentId           //2
                  ,ContentName         //3
                  ,ContentProjectId    //4
                  ,ContentProjectName  //5
                  ,ContentOwnerId      //6
                  ,ContentOwnerName    //7
                  ,ContentTags         //8
                  ,DeveloperNotes      //9
                  },
              new string[] { 
                   "datasource"                //1
                  ,thisDatasource.Id           //2
                  ,thisDatasource.Name         //3
                  ,thisDatasource.ProjectId    //4
                  ,thisDatasource.ProjectName  //5
                  ,thisDatasource.OwnerId      //6
                  ,ownerName                   //7
                  ,thisDatasource.TagSetText   //8
                  ,thisDatasource.DeveloperNotes //9
                  });
      }
  }

    /// <summary>
    /// Add CSV for all the data sources
    /// </summary>
    /// <param name="dataSources"></param>
  private void AddWorkbooksData(IEnumerable<SiteWorkbook> workbooks)
  {
      //None? Do nothing.
      if (workbooks == null) return;

      //Add each data source as a row in the CSV file we will generate
      foreach (var thisWorkbook in workbooks)
      {
          //Attempt to look up the owner name.  This will be blank if we do not have a users list
          string ownerName = helper_AttemptUserNameLookup(thisWorkbook.OwnerId);

          this.AddKeyValuePairs(
              new string[] { 
                   ContentType            //1 
                  ,ContentId              //2
                  ,ContentName            //3
                  ,ContentWorkbookId      //4
                  ,ContentWorkbookName    //5
                  ,ContentUrl             //6
                  ,ContentProjectId       //7
                  ,ContentProjectName     //8
                  ,ContentOwnerId         //9
                  ,ContentOwnerName       //10
                  ,WorkbookShowTabs       //11
                  ,ContentTags            //12
                  ,DeveloperNotes         //13
                           },
              new string[] { 
                  "workbook"                //1 
                  ,thisWorkbook.Id          //2
                  ,thisWorkbook.Name        //3
                  ,thisWorkbook.Id          //4
                  ,thisWorkbook.Name        //5
                  ,thisWorkbook.ContentUrl  //6
                  ,thisWorkbook.ProjectId   //7
                  ,thisWorkbook.ProjectName //8
                  ,thisWorkbook.OwnerId     //9
                  ,ownerName                //10
                  ,XmlHelper.BoolToXmlText(thisWorkbook.ShowTabs) //11
                  ,thisWorkbook.TagSetText  //12
                  ,thisWorkbook.DeveloperNotes //13
                           });

          //If we have workbooks connections information then log that
          AddWorkbookConnectionData(thisWorkbook);
      }
  }

/// <summary>
/// Add data source connection data
/// </summary>
/// <param name="thisWorkbook"></param>
private void AddWorkbookConnectionData(SiteWorkbook thisWorkbook)
{
    var dataConnections = thisWorkbook.DataConnections;
    if(dataConnections == null)
    {
        return;
    }

    //Write out details for each data connection
    foreach (var thisConnection in dataConnections)
    {
        this.AddKeyValuePairs(
            new string[] { 
                ContentType               //1 
                ,ContentId                //2
                ,ContentConnectionType    //3
                ,ContentConnectionServer  //4
                ,ContentConnectionPort    //5
                ,ContentConnectionUserName//6
                ,ContentWorkbookId        //7
                ,ContentWorkbookName      //8
                ,ContentProjectId         //9
                ,ContentProjectName       //10
                ,ContentOwnerId           //11
                ,DeveloperNotes           //12
                        },
            new string[] { 
                "data-connection"              //1 
                ,thisConnection.Id             //2
                ,thisConnection.ConnectionType //3
                ,thisConnection.ServerAddress  //4
                ,thisConnection.ServerPort     //5
                ,thisConnection.UserName       //6
                ,thisWorkbook.Id               //7
                ,thisWorkbook.Name             //8
                ,thisWorkbook.ProjectId        //9
                ,thisWorkbook.ProjectName      //10
                ,thisWorkbook.OwnerId          //11
                ,thisWorkbook.DeveloperNotes   //12
                        });
    }
}

    /// <summary>
    /// Add CSV rows for all the projects data
    /// </summary>
    /// <param name="projects"></param>
    private void AddProjectsData(IEnumerable<SiteProject> projects)
    {
        //No data to add? do nothing.
        if (projects == null) return;

        //Add each project as a row in the CSV file we will generate
        foreach (var thisProject in projects)
        {
            this.AddKeyValuePairs(
                new string[] { 
                    ContentType               //1
                    ,ContentId                 //2
                    ,ContentName               //3
                    ,ContentProjectId          //4
                    ,ContentProjectName        //5
                    ,ContentDescription        //6
                    ,DeveloperNotes            //7
                                },      
                new string[] { 
                     "project"                  //1
                    ,thisProject.Id             //2
                    ,thisProject.Name           //3
                    ,thisProject.Id             //4
                    ,thisProject.Name           //5
                    ,thisProject.Description    //6
                    ,thisProject.DeveloperNotes //7
                                });       
        } 
    }

    /// <summary>
    /// Add CSV rows for all the views data
    /// </summary>
    /// <param name="views"></param>
    private void AddViewsData(IEnumerable<SiteView> views)
    {
        //No data to add? do nothing.
        if (views == null) return;

        //Add each project as a row in the CSV file we will generate
        foreach (var thisView in views)
        {
            this.AddKeyValuePairs(
                new string[] {
                    ContentType                //1
                    ,ContentId                 //2
                    ,ContentName               //3
                    ,ContentUrl                //4
                    ,ContentOwnerId            //5
                    ,ContentWorkbookId         //6
                    ,ContentViewCount          //7
                    ,DeveloperNotes            //8
                                },
                new string[] {
                     "view"                     //1
                    ,thisView.Id                //2
                    ,thisView.Name              //3
                    ,thisView.ContentUrl        //4
                    ,thisView.OwnerId           //5
                    ,thisView.WorkbookId        //6
                    ,thisView.TotalViewCount.ToString() //7  UNDONE: Could expland CSV generation to be integers and strings, rather than make this a string   
                    ,thisView.DeveloperNotes    //8
                                });
        }
    }

    /// <summary>
    /// Add CSV rows for all the subscriptions data
    /// </summary>
    /// <param name="subscriptions"></param>
    private void AddSubscriptionsData(IEnumerable<SiteSubscription> subscriptions)
    {
        //No data to add? do nothing.
        if (subscriptions == null) return;

        //Add each subscription as a row in the CSV file we will generate
        foreach (var thisSubscription in subscriptions)
        {

            string contentUrl = "";
            string thisSubscriptionViewId = "";
            string thisSubscriptionWorkbookId = "";

            if(thisSubscription.ContentType == "View")
            {
                thisSubscriptionViewId = thisSubscription.ContentId;
                //If we have view information, then look up the workbook id
                var thisView = helper_AttemptViewLookup(thisSubscription.ContentId);
                if(thisView != null)
                {
                    thisSubscriptionWorkbookId = thisView.WorkbookId;
                    contentUrl = thisView.ContentUrl;
                }
            }
            else if(thisSubscription.ContentType == "Workbook")
            {
                thisSubscriptionWorkbookId = thisSubscription.ContentId;
            }
            else
            {
                this.StatusLog.AddError("Unknown subscription type: " + thisSubscription.ContentType);
            }

            //If we have workbook information, look up the workbook and get its name
            string thisWorkbookName = "";
            string thisWorkbookProjectId = "";
            if(!string.IsNullOrWhiteSpace(thisSubscriptionWorkbookId))
            {
                var thisWorkbook = helper_AttemptWorkbookLookup(thisSubscriptionWorkbookId);
                if(thisWorkbook != null)
                {
                    thisWorkbookName = thisWorkbook.Name;
                    thisWorkbookProjectId = thisWorkbook.ProjectId;

                    //If the subscription is for a Workbook, and we have that workbook then grab the content URL
                    if (thisSubscription.ContentType == "Workbook")
                    {
                        contentUrl = thisWorkbook.ContentUrl;
                    }
                }
            }
            

            this.AddKeyValuePairs(
                new string[] {
                ContentType               //1
                ,ContentId                //2
                ,ContentSubscriptionId    //3
                ,ContentDescription       //4
                ,ContentOwnerId           //5
                ,ContentOwnerName         //6
                ,ContentScheduleId        //7
                ,ContentScheduleName      //8
                ,ContentSubscriptionType  //9
                ,ContentWorkbookId        //10
                ,ContentViewId            //11
                ,ContentWorkbookName      //12
                ,ContentProjectId         //13
                ,ContentUrl               //14
                ,DeveloperNotes           //15
                },
                new string[] {
                 "subscription"                  //1
                ,thisSubscription.Id             //2
                ,thisSubscription.Id             //3
                ,thisSubscription.Subject        //4
                ,thisSubscription.UserId         //5
                ,thisSubscription.UserName       //6
                ,thisSubscription.ScheduleId     //7
                ,thisSubscription.ScheduleName   //8
                ,thisSubscription.ContentType    //9
                ,thisSubscriptionWorkbookId      //10
                ,thisSubscriptionViewId          //11
                ,thisWorkbookName                //12
                ,thisWorkbookProjectId           //13
                ,contentUrl                      //14
                ,thisSubscription.DeveloperNotes //15
                });
        }
    }

    /// <summary>
    /// Add CSV rows for all the groups data
    /// </summary>
    /// <param name="groups"></param>
    private void AddGroupsData(IEnumerable<SiteGroup> groups)
    {
        //No data to add? do nothing.
        if (groups == null) return;

        //Add each project as a row in the CSV file we will generate
        foreach (var thisGroup in groups)
        {
            this.AddKeyValuePairs(
                new string[] { 
                     ContentType               //1
                    ,ContentId                 //2
                    ,ContentName               //3
                    ,ContentGroupId            //4
                    ,ContentGroupName          //5
                    ,DeveloperNotes            //6
                             },
                new string[] { 
                     "group"                   //1
                    ,thisGroup.Id              //2
                    ,thisGroup.Name            //3
                    ,thisGroup.Id              //4
                    ,thisGroup.Name            //5
                    ,thisGroup.DeveloperNotes  //6
                               });

            //-------------------------------------------------------------------------
            //Add the set of users that are members of the group
            //-------------------------------------------------------------------------
            AddGroupUsersData(thisGroup);
        }
    }

    /// <summary>
    /// CSV rows for each member of a group
    /// </summary>
    /// <param name="group"></param>
    private void AddGroupUsersData(SiteGroup group)
    {
        var usersInGroup = group.Users;
        //Nothing to add?
        if(usersInGroup == null)
        {
            return;
        }

        string groupId = group.Id;
        string groupName = group.Name;

        foreach(var thisUser in usersInGroup)
        {
            this.AddKeyValuePairs(
                new string[] { 
                    ContentType               //1
                   ,ContentId                 //2
                   ,ContentName               //3
                   ,SiteRole                  //4
                   ,ContentUserId             //5
                   ,ContentUserName           //6
                   ,ContentGroupId            //7
                   ,ContentGroupName          //8
                   ,DeveloperNotes            //9
                             },
                new string[] { 
                    "group-member"            //1
                   ,thisUser.Id               //2
                   ,thisUser.Name             //3
                   ,thisUser.SiteRole         //4
                   ,thisUser.Id               //5
                   ,thisUser.Name             //6
                   ,groupId                   //7
                   ,groupName                 //8
                   ,thisUser.DeveloperNotes   //9
                               });

        }
    }

    /// <summary>
    /// Add CSV rows for all the users data
    /// </summary>
    /// <param name="projects"></param>
    private void AddUsersData(IEnumerable<SiteUser> users)
    {
        //No data to add? do nothing.
        if (users == null) return;

        //Add each project as a row in the CSV file we will generate
        foreach (var thisUser in users)
        {
            this.AddKeyValuePairs(
                new string[] { 
                    ContentType         //1 
                    ,ContentId          //2
                    ,ContentName        //3
                    ,SiteRole           //4
                    ,ContentUserId      //5
                    ,ContentUserName    //6
                    ,DeveloperNotes     //7
                             },
                new string[] { 
                    "user"                   //1
                    ,thisUser.Id             //2
                    ,thisUser.Name           //3
                    ,thisUser.SiteRole       //4
                    ,thisUser.Id             //5
                    ,thisUser.Name           //6
                    ,thisUser.DeveloperNotes //7
                             });
        }
    }
}
 