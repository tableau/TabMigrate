# TabMigrate


## What is TabMigrate?
TabMigrate is a lightweight tool for moving Tableau content between multiple Tableau Server environments, such as test and production Tableau Servers or sites. It can also be used to provision sites with Workbooks and Data Sources from a local file system. TabMigrate also allows you to produce a CSV file containing an inventory of the site’s users and content; useful for analysis in Tableau.

Often there is a need to copy a set of content from one Tableau Server environment into another. Sometimes this need is across different Tableau Servers (or Tableau Online), other times there is a need to copy some content from one site in a server into another site. Today, this can be complex and require manual steps. For example, if a published workbook utilizes published Data Sources it is often impossible without significant manual steps to move the data sources and content form one Tableau Server to another.

By comparison, Tableau Server’s tabAdmin tool offers a full “site import and export” for copying and replacing an entire site. This is powerful but heavy-weight, replacing the entire site, its users, its schedules, etc.
This tool offers a lightweight approach built on top of Tableau Server’s REST APIs. 
- It allows the contents (Workbooks, Data Sources, and Projects) to be downloaded from a Tableau Server site into your local file system.
- It allows this file system content to be transformed and uploaded back into another Tableau Server or Tableau Server site, along-side existing content.
- It generates command line arguments for the export and import operations, allowing you to quickly repeat or modify these steps.

### Video Introduction
There's a short video introduction available on Tableau's YouTube channel. You can [watch the full video](https://www.youtube.com/watch?v=zxeo_gBT8dk) or just jump to specific sections:<br />
* [Introduction (00:00)](https://www.youtube.com/watch?v=zxeo_gBT8dk)
* [What is TabMigrate? (00:32)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=00m32s)
* [Running TabMigrate in Visual Studio (01:23)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=01m23s)
* [Getting a Site's Inventory (02:30)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=02m30s)
* [Exporting Site Content (03:33)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=03m33s)
* [Uploading Content to a New Tableau Server (05:34)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=05m34s)
* [Remapping Workbook Server References (07:13)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=07m13s)
* [Showing Migrated Content in the New Server (09:18)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=09m18s)
* [Diving into the Source Code (09:42)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=09m42s)
* [Wrap-up & Getting the Code on GitHub (12:30)](https://www.youtube.com/watch?v=zxeo_gBT8dk#t=12m30s)

## Versions of Tableau Server
TabMigrate was written and tested with version 10.2 of Tableau Server. 
- It should work in all Tableau Server versions >= 10.1.  
- Earlier versions of TabMigrate work with earlier Tableau servers (check the releases)
- Moving forward, the application will be maintained and gain new features with each released version of Tableau Server, as the REST API set expands. If you need older versions, you can download the previous releases.


## Getting started with TabMigrate (for non-developers)
You do not need to download and compile the source code to use TabMigrate. Those interested in simply running the application can download and unzip the setup from https://github.com/tableau/TabMigrate/releases (hint: the latest release is in v1.02_2015_11_13_TabRESTMigrate.zip -> download, unzip, and rock on). 
Running setup.exe will install the TabMigrate application on your Windows machine. 

Application: The application can be run in either interactive (UI) or command line mode. When running in interactive mode the application will also show you the command line for performing all of the actions, making it easy to automate.  The application UI offers three top level options: 
   1. Generate an inventory of your site: This downloads information about your site into a *.csv file that can easily be loaded into Tableau Desktop or Excel.
   2. Export content from your site: You can either export your entire site (all the Workbooks and Data Sources), or choose an individual Project whose contents you want to export. Export will create file system directories for “workbooks” and “datasources” and download your sites content into subdirectories named after each Tableau Server site you export.
   3. Import content from your file system: You can bulk upload workbooks and datasources from your local file system into a Tableau Server site that you choose. This expects the same file system directory site export; file system directories are named for the Tableau Server projects that they will be published into.
NOTE: You can also specify database IDs and Passwords to use when publishing your Workbooks and Data Sources. Workbooks with live database connections REQUIRE passwords to be included during publish. These are specified by listing the database credentials in an XML (text) file.  Example:

```xml
<!-- Example file that shows how credentials can be declared. This file can be used as part of site import to supply needed workbook and datasource credentials-->
<xml>
     <credential 
          contentType="workbook" 
          contentProjectName="Test Site Import" 
	      contentName="test.twbx" 
		  dbUser="SimpleUser" 
	      dbPassword="q.123456" 
	      credentialIsEmbedded="false"> 
     </credential>
     <credential 
	      contentType="datasource" 
		  contentProjectName="Test Site Import" 
		  contentName="test2.tds" 
		  dbUser="SimpleUser3" 
		  dbPassword="q.12345678"> 
     </credential>
</xml>
```

### Safety tips 
The REST APIs used by this application allow you to upload, download, and otherwise modify your site’s content, workbooks, data sources, content tags, etc. So yes, it is certainly possible for you to overwrite existing content on server. A few tips:
-	“Generate site inventory” – This option does not perform write/upload/delete actions on your site, it should be “read only” in its behavior. It should have no negative side effects and is a great way to learn about and explore the capabilities of the application.
-	“Export site contents to local directory” – Be default, this option does not perform any write actions to your site and will be “read only” in its behavior. It downloads your Tableau Server site’s content to your local machines file system. NOTE: There are non-default options such as “[x] Remove tag from exported content” that will perform minor modifications your site’s content.
-	“Upload from file system into site” – This certainly will modify content on the site you specify. If there is existing content with the same name as content being uploaded it will overwrite it. Before running this command against a site with existing content, we recommend you run it using an empty site to verify the expected behavior.
-	To reduce the chance of accidently writing to the wrong site we recommend using a “site admin” (not system admin) account when possible. For best protection, the site admin account should only be a member of the single site you are uploading to.

## Getting started with TabMigrate (for developers)
Source code: The project is written in C# and should load into Visual Studio 2013 or newer, including the free Visual Studio Express for Desktop.             

### What’s particularly useful in the source code? 
The code demonstrates complex aspects of both the REST API and moving content between sites/servers. Someone working with the code will have a great base for calling any Tableau REST APIs from C#.
Examples of things that are hard without detailed working code:
- Using the REST api to sign in to your Tableau Server or site and including the session token in all subsequent REST API calls
- Packaging MIME messages to the REST API can be complex and challenging to debug
- Downloading and uploading both compressed *.twbx/*.tdsx and text *.twb/*.tds Workbooks and Data Sources requires interpretation of server response types
- Uploading large files in chunks requires exact formatting of data sent to the server
- Updating Workbook XML to point to data sources on a new server is detailed. The sample code shows how to: (i) unzip *.twbx files, (ii) update the Tableau Workbook’s XML so the data sources point to the new server/site, (iii) repackage the transformed workbook back into a compressed *.twbx that can be uploaded to Tableau Server.
- Associating database credentials with the Workbooks and Datasources you are publishing
- Querying for lists of Projects, Workbooks, Data Sources and users and interpreting the results

The source code also contains example files in a “_SampleFiles” subdirectory.
- CredentialExample.xml : You can use this as a template for files you generate to associate database credentials with Workbooks and Data Sources you are publishing.


## Is TabMigrate supported? 
A standard disclaimer: TabMigrate is made available AS-IS with no support and no warranty whatsoever. Using it you can accidentally modify or delete your content, just as you can by accidentally do so in the user interface. Despite efforts to write good and useful code there may be bugs that cause unexpected and undesirable behavior. The software is strictly “use at your own risk.”

The good news: This is intended to be a self-service tool. You are free to modify it in any way to meet your needs.
