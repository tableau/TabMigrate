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
    const string ContentWorkbookName = "workbook-name";
    const string ContentProjectName = "project-name";
    const string ContentUserId = "user-id";
    const string ContentUserName= "user-name";
    const string ContentGroupId = "group-id";
    const string ContentGroupName = "group-name";
    const string ContentId = "id";
    const string ContentName = "name";
    const string ContentDescription = "description";
    const string ContentOwnerId = "owner-id";
    const string ContentTags = "tags";
    const string WorkbookShowTabs = "workbook-show-tabs";
    const string SiteRole = "user-role";
    const string DeveloperNotes = "developer-notes";

   /// <summary>
   /// Constructor.  Builds the data for the CSV file
   /// </summary>
   /// <param name="projects"></param>
   /// <param name="dataSources"></param>
  public CustomerSiteInventory(IEnumerable<SiteProject> projects, 
      IEnumerable<SiteDatasource> dataSources,
      IEnumerable<SiteWorkbook> workbooks,
      IEnumerable<SiteUser> users,
      IEnumerable<SiteGroup> groups)
  {
      AddProjectsData(projects);
      AddDatasourcesData(dataSources);
      AddWorkbooksData(workbooks);
      AddUsersData(users);
      AddGroupsData(groups);
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
          this.AddKeyValuePairs(
              new string[] { 
                   ContentType         //1
                  ,ContentId           //2
                  ,ContentName         //3
                  ,ContentProjectId    //4
                  ,ContentProjectName  //5
                  ,ContentOwnerId      //6
                  ,ContentTags         //7
                  ,DeveloperNotes      //8
                  },
              new string[] { 
                   "datasource"                //1
                  ,thisDatasource.Id           //2
                  ,thisDatasource.Name         //3
                  ,thisDatasource.ProjectId    //4
                  ,thisDatasource.ProjectName  //5
                  ,thisDatasource.OwnerId      //6
                  ,thisDatasource.TagSetText   //7
                  ,thisDatasource.DeveloperNotes //8
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
                  ,WorkbookShowTabs       //10
                  ,ContentTags            //11
                  ,DeveloperNotes         //12
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
                  ,XmlHelper.BoolToXmlText(thisWorkbook.ShowTabs) //10
                  ,thisWorkbook.TagSetText  //11
                  ,thisWorkbook.DeveloperNotes //12
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
 