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
    const string ContentProjectId = "project-id";
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
                  ,ContentUrl             //4
                  ,ContentProjectId       //5
                  ,ContentProjectName     //6
                  ,ContentOwnerId         //7
                  ,WorkbookShowTabs       //8
                  ,ContentTags            //9
                  ,DeveloperNotes         //10
                           },
              new string[] { 
                  "workbook"                //1 
                  ,thisWorkbook.Id          //2
                  ,thisWorkbook.Name        //3
                  ,thisWorkbook.ContentUrl  //4
                  ,thisWorkbook.ProjectId   //5
                  ,thisWorkbook.ProjectName //6
                  ,thisWorkbook.OwnerId     //7
                  ,XmlHelper.BoolToXmlText(thisWorkbook.ShowTabs) //8
                  ,thisWorkbook.TagSetText  //9
                  ,thisWorkbook.DeveloperNotes //10
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
 