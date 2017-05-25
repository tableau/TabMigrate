using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace OnlineContentDownloader
{
    public partial class FormSiteExportImport : Form
    {
        const string DefaultTextDBCredentialsImport = "path to a database credentials XML file here... (optional)";

        /// <summary>
        /// If not NULL, indicates that we have command line parameters passed in that we want to run
        /// </summary>
        CommandLineParser _startupCommandLine;

        /// <summary>
        /// The running Online Downloader task
        /// </summary>
        TaskMaster _onlineTaskMaster;


        public FormSiteExportImport()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gives us the arbitrary command text from a textbox
        /// </summary>
        /// <param name="txtBox"></param>
        /// <returns></returns>
        private string ParseArbitraryCommandTextFromUi(TextBox txtBox, string defaultText)
        {
            var text = txtBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Trim();

            if (text == defaultText) return "";

            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRunExportAsync_Click(object sender, EventArgs e)
        {
            _onlineTaskMaster = null;
            try
            {
                var asyncTask = CreateAsyncExportTask(false);
                if (asyncTask != null)
                {
                    _onlineTaskMaster = asyncTask;
                    //Start the work running
                    StartRunningAsyncTask();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring export:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        /// <summary>
        /// Returns a file system path we can use to store information about the specified site
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <returns></returns>
        private string GeneratePathFromSiteUrl(TableauServerUrls siteUrl)
        {
            string appPath =
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TabMigrate");

            //Add the server name to the path
            appPath = Path.Combine(appPath, FileIOHelper.GenerateWindowsSafeFilename(siteUrl.ServerName));
            //Add the site name to the path
            string siteName = siteUrl.SiteUrlSegement;
            if(!string.IsNullOrEmpty(siteName))
            {
                appPath = Path.Combine(appPath, siteName);
            }

            FileIOHelper.CreatePathIfNeeded(appPath);
            return appPath;
        }



        /// <summary>
        /// Sanity cehck for sign in 
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="signInUser"></param>
        /// <param name="signInPassword"></param>
        /// <returns></returns>
        private bool ValidateSignInPossible(string siteUrl, string signInUser, string signInPassword)
        {
            var testSignInStatusLog = new TaskStatusLogs();
            testSignInStatusLog.SetStatusLoggingLevel(int.MinValue);
            try
            {
                TableauServerSignIn.VerifySignInPossible(siteUrl, signInUser, signInPassword, testSignInStatusLog);
            }
            catch 
            {
                MessageBox.Show("Sign in to your Tableau Server failed. Please check URL and credentials");
                textBoxStatus.Text = testSignInStatusLog.StatusText;
                textBoxErrors.Text = testSignInStatusLog.ErrorText;
                return false;
            }
            return true;
        }


        /// <summary>
        /// Creates the task we use to export a Tableau Server sites content to the local file system
        /// </summary>
        /// <returns></returns>
        private TaskMaster CreateAsyncExportTask(bool showPasswordInUi)
        {
            string siteUrl = txtUrlExportFrom.Text;
            string signInUser = txtIdExportFrom.Text;
            string signInPassword = txtPasswordExportFrom.Text;
            bool isSiteAdmin = chkExportUserIsAdmin.Checked;
            string exportOnlySingleProject = txtExportSingleProject.Text.Trim();
            string exportOnlyWithTag = txtExportOnlyTagged.Text.Trim();


            //----------------------------------------------------------------------
            //Sanity test the sign in.  If this fails, then there is no point in 
            //moving forward
            //----------------------------------------------------------------------
            bool signInTest = ValidateSignInPossible(siteUrl, signInUser, signInPassword);
            if (!signInTest)
            {
                return null;
            }

            var onlineUrls = TableauServerUrls.FromContentUrl(siteUrl, TaskMasterOptions.RestApiReponsePageSizeDefault);

            var nowTime = DateTime.Now;
            //Local path
            string localPathForSiteOutput = GeneratePathFromSiteUrl(onlineUrls);
            localPathForSiteOutput = FileIOHelper.PathDateTimeSubdirectory(localPathForSiteOutput, true, "siteExport", nowTime);

            //Log file
            string localPathForLogFile =
                Path.Combine(localPathForSiteOutput, "siteExport_log.txt");

            //Errors file
            string localPathForErrorsFile =
                Path.Combine(localPathForSiteOutput, "siteExport_errors.txt");

            //-----------------------------------------------------------------
            //Generate a command line
            //-----------------------------------------------------------------
            string commandLineAsText;
            CommandLineParser commandLineParsed;
            CommandLineParser.GenerateCommandLine_SiteExport(
                showPasswordInUi,
                localPathForSiteOutput,
                siteUrl,
                signInUser,
                signInPassword,
                isSiteAdmin,
                exportOnlySingleProject,
                exportOnlyWithTag,
                chkExportRemoveExportTag.Checked,
                localPathForLogFile,
                localPathForErrorsFile,
                chkExportContentsWithKeepAlive.Checked,
                chkVerboseLog.Checked,
                chkGenerateDownloadMetadataFiles.Checked,
                out commandLineAsText,
                out commandLineParsed);

            //Show the user the command line, so that they can copy/paste and run it
            textSiteExportCommandLine.Text = PathHelper.GetApplicaitonPath() + " " + commandLineAsText;

            //=====================================================================
            //Create the task
            //=====================================================================
            return TaskMaster.FromCommandLine(commandLineParsed);

        }


        /// <summary>
        /// Creates the task we use to export a Tableau Server sites content to the local file system
        /// </summary>
        /// <returns></returns>
        private TaskMaster CreateAsyncImportTask(bool showPasswordInUi)
        {
            string siteUrl = txtUrlImportTo.Text;
            string signInUser = txtIdImportTo.Text;
            string signInPassword = txtPasswordImportTo.Text;
            bool isSiteAdmin = chkImportIsSiteAdmin.Checked;
            string localPathImportFrom = txtSiteImportContentPath.Text;
            bool remapContentOwnership = chkImportRemapContentOwnership.Checked;

            //Check that this contains Workbooks or Data Sources; otherwise it's not a valid path with content
            if (!TaskMaster.IsValidImportFromDirectory(localPathImportFrom))
            {
                throw new Exception("The import directory specified does not contain datasources/workbooks sub directories. Import aborted.");
            }

            //If there is a DB credentials file path make sure it actually points to a file
            string pathDBCredentials = GetDBCredentialsImportPath();
            if(!string.IsNullOrWhiteSpace(pathDBCredentials))
            {
                if(!File.Exists(pathDBCredentials))
                {
                    throw new Exception("The path to the db credentials file does not exist, " + pathDBCredentials);
                }
            }

            //----------------------------------------------------------------------
            //Sanity test the sign in.  If this fails, then there is no point in 
            //moving forward
            //----------------------------------------------------------------------
            bool signInTest = ValidateSignInPossible(siteUrl, signInUser, signInPassword);
            if (!signInTest)
            {
                return null;
            }

            var onlineUrls = TableauServerUrls.FromContentUrl(siteUrl, TaskMasterOptions.RestApiReponsePageSizeDefault);

            //Local path
            string localPathForSiteOutput = GeneratePathFromSiteUrl(onlineUrls);

            //Output file
            var nowTime = DateTime.Now;
            string localPathForOutputFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteImport.csv", nowTime));

            //Log file
            string localPathForLogFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteImport_log.txt", nowTime));

            //Errors file
            string localPathForErrorsFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteImport_errors.txt", nowTime));

            //Manual steps file
            string localPathForManualStepsFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteImport_manualSteps.csv", nowTime));


            //-----------------------------------------------------------------
            //Generate a command line
            //-----------------------------------------------------------------
            string commandLineAsText;
            CommandLineParser commandLineParsed;
            CommandLineParser.GenerateCommandLine_SiteImport(
                 showPasswordInUi,
                 localPathImportFrom,
                 siteUrl,
                 signInUser,
                 signInPassword,
                 isSiteAdmin,
                 chkRemapWorkbookDataserverReferences.Checked,
                 pathDBCredentials,
                 localPathForLogFile,
                 localPathForErrorsFile,
                 localPathForManualStepsFile,
                 remapContentOwnership,
                 chkVerboseLog.Checked,
                 out commandLineAsText,
                 out commandLineParsed);

            //Show the user the command line, so that they can copy/paste and run it
            txtSiteImportCommandLineExample.Text = PathHelper.GetApplicaitonPath() + " " + commandLineAsText;

            //=====================================================================
            //Create the task
            //=====================================================================
            return TaskMaster.FromCommandLine(commandLineParsed);
        }


        /// <summary>
        /// Get an inventory of content on the specified server
        /// </summary>
        private TaskMaster CreateAsyncInventoryTask(bool showPasswordInUi)
        {
            string siteUrl = txtUrlInventoryFrom.Text;
            string signInUser = txtIdInventoryFromUserId.Text;
            string signInPassword = txtPasswordInventoryFrom.Text;
            bool isSystemAdmin = chkInventoryUserIsAdmin.Checked;
            var nowTime = DateTime.Now;

            //----------------------------------------------------------------------
            //Sanity test the sign in.  If this fails, then there is no point in 
            //moving forward
            //----------------------------------------------------------------------
            bool signInTest = ValidateSignInPossible(siteUrl, signInUser, signInPassword);
            if(!signInTest)
            {
                return null;
            }

            var onlineUrls = TableauServerUrls.FromContentUrl(siteUrl, TaskMasterOptions.RestApiReponsePageSizeDefault);

            //Local path
            string localPathForSiteOutput = GeneratePathFromSiteUrl(onlineUrls);

            //Output file
            string localPathForOutputFile = 
                Path.Combine(localPathForSiteOutput, 
                             FileIOHelper.FilenameWithDateTimeUnique("siteInventory.csv", nowTime));

            //Log file
            string localPathForLogFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteInventory_log.txt", nowTime));

            //Errors file
            string localPathForErrorsFile =
                Path.Combine(localPathForSiteOutput,
                             FileIOHelper.FilenameWithDateTimeUnique("siteInventory_errors.txt", nowTime));

            //-----------------------------------------------------------------
            //Generate a command line
            //-----------------------------------------------------------------
            string commandLineAsText;
            CommandLineParser commandLineParsed;
            CommandLineParser.GenerateCommandLine_SiteInventory(
                showPasswordInUi,
                localPathForOutputFile,
                siteUrl,
                signInUser,
                signInPassword,
                isSystemAdmin,
                localPathForLogFile,
                localPathForErrorsFile,
                chkGenerateInventoryTwb.Checked,
                chkVerboseLog.Checked,
                out commandLineAsText,
                out commandLineParsed);

            //Show the user the command line, so that they can copy/paste and run it
            txtInventoryExampleCommandLine.Text = PathHelper.GetApplicaitonPath() + " " + commandLineAsText;


            //=====================================================================
            //Create the task
            //=====================================================================
            return TaskMaster.FromCommandLine(commandLineParsed);
        }

        /// <summary>
        /// Called to start the background task running
        /// </summary>
        private void StartRunningAsyncTask()
        {
            var task = _onlineTaskMaster;
            if(task == null)
            {
                AppDiagnostics.Assert(false, "No task to run");
                return;
            }

            task.ExecuteTaskBegin();
            textBoxStatus.Text = "Started...";
            textBoxErrors.Text = "";
            btnAbortRun.Visible = true;
            timer1.Enabled = true;
        }


        /// <summary>
        /// TRUE if the applicaiton is running in interactive mode
        /// FALSE if the application is running in automated mode and should not interrupt the user in any way
        /// (e.g. no modal dialogs, no shelling files, etc)
        /// </summary>
        bool AllowUserInterruptions
        {
            get
            {
                var commandLine = _startupCommandLine;

                //If there is no command line, then the user can be interrupted by the UI
                if(commandLine == null) return true;
                //If there is a command line, and ExitWhenDone not specified, the user can be interrupted
                return !(commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_ExitWhenDone));
            }
        }

        /// <summary>
        /// TRUE: Application should exit when done
        /// FALSE: Application should stay running and interactive
        /// </summary>
        bool ExitWhenDoneRunningTask
        {
            get
            {
                var commandLine = _startupCommandLine;
                return (commandLine != null) 
                    && (commandLine.GetParameterValueAsBool(CommandLineParser.Parameter_ExitWhenDone));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            var taskMaster = _onlineTaskMaster;
            //Nothing to do... go to sleep
            if (taskMaster == null)
            {
                timer1.Enabled = false;
                btnAbortRun.Visible = false;
                return;
            }

            //-------------------------------------------------------------
            //If the work is done, clean up....
            //-------------------------------------------------------------
            if (taskMaster.IsDone)
            {
                //Stop the timer...
                timer1.Enabled = false;
                btnAbortRun.Visible = false;

                //Tell the process to exit?
                if (this.ExitWhenDoneRunningTask)
                {
                    ExitApplication();
                }
                else if (this.AllowUserInterruptions) //Show reports?
                {
                    //Show error reports and other results
                    AsyncTaskDone_Reporting(taskMaster);
                }

                return;
            }

            //Update normal status
            //Get the status from the backtround task
            string statusTextRuning = taskMaster.StatusLog.StatusText;
            statusTextRuning = DateTime.Now.ToString() + ": Running...\r\nsteps \r\n" + statusTextRuning;
            //Scroll to bottom
            textBoxStatus.Text = statusTextRuning;
            ScrollToEndOfTextbox(textBoxStatus);

            textBoxErrors.Text = taskMaster.StatusLog.ErrorText;
        }

        /// <summary>
        /// Called to exit the application
        /// </summary>
        private void ExitApplication()
        {
            this.Close();
        }


        /// <summary>
        /// Called after we have detected that the sync task has completed
        /// </summary>
        /// <param name="taskMaster"></param>
        void AsyncTaskDone_Reporting(TaskMaster taskMaster)
        {
            //Get the status from the backtround task
            string statusText = taskMaster.StatusLog.StatusText;

            string errorCountText = "";
            int errorCount = taskMaster.StatusLog.ErrorCount;
            if (errorCount > 0)
            {
                errorCountText = " " + errorCount.ToString() + " errors";
            }

            statusText = statusText + "\r\n\r\n" + DateTime.Now.ToString() + " Done." + errorCountText + "\r\n";
            textBoxStatus.Text = statusText;
            ScrollToEndOfTextbox(textBoxStatus);

            textBoxErrors.Text = taskMaster.StatusLog.ErrorText;

            //If there is a manual steps file, shell it
            var manualStepsFile = taskMaster.PathToManualStepsReport;
            if(!string.IsNullOrWhiteSpace(manualStepsFile))
            {
                AttemptToShellFile(manualStepsFile);
            }

            //If the job was to take a site inventory,show the report
            if (taskMaster.JobName == TaskMaster.JobName_SiteInventory)
            {
                //First, see if we have a generated *.twb file
                string inventoryFile = taskMaster.PathToSiteInventoryReportTwb;
                //Second, if we don't have a *.twb file, see if we have a *.csv file of the raw data
                if(string.IsNullOrWhiteSpace(inventoryFile))
                {
                    inventoryFile = taskMaster.PathToSiteInventoryReportCsv;
                }                    

                //If we have either file, shell it    
                if (!string.IsNullOrWhiteSpace(inventoryFile))
                {
                    AttemptToShellFile(inventoryFile);
                }
            }
            else if(taskMaster.JobName == TaskMaster.JobName_SiteExport)
            {
                //We want to shell the file explorer to show the path we have just exported to
                string exportDirectory = taskMaster.PathToExportTo;
                if(!string.IsNullOrWhiteSpace(exportDirectory))
                {
                    AttemptToShellFile(Path.Combine(exportDirectory, "."));
                }
            }
        }

        /// <summary>
        /// Open a file in the Windows shell (e.g. open a textfile or csv file)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool AttemptToShellFile(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch(Exception ex)
            {
                AppDiagnostics.Assert(false, "Failure to shell file: " + ex.Message);
                return false;
            }

            return true; //Success
        }

        /// <summary>
        /// Shows status text in the textboxes
        /// </summary>
        /// <param name="statusLog"></param>
        private void UpdateStatusText(TaskStatusLogs statusLog)
        {
            textBoxStatus.Text = statusLog.StatusText;
            ScrollToEndOfTextbox(textBoxStatus);

            textBoxErrors.Text = statusLog.ErrorText;
        }


        //Scroll to the end
        private static void ScrollToEndOfTextbox(TextBox textbox)
        {
            textbox.SelectionStart = textbox.Text.Length;
            textbox.ScrollToCaret();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Quit the work if its running...
            var task = _onlineTaskMaster;
            if(task != null)
            {
                task.Abort();
                _onlineTaskMaster = null;
            }

            Application.Exit();
        }


        /// <summary>
        /// Sets the checkbox value to true/false
        /// </summary>
        /// <param name="chkbox"></param>
        /// <param name="value"></param>
        private void SetCheckboxFromText(CheckBox chkbox, string value)
        {
            if (value == null) return;
            value = value.Trim().ToLower();
            if(value == "true") 
            {
                chkbox.Checked = true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(value == "false", "Expected true or false");
                chkbox.Checked = false;
            }

        }

        /// <summary>
        /// Starts an import into site process....
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRunAsyncImport_Click(object sender, EventArgs e)
        {
            _onlineTaskMaster = null;
            try
            {
                var asyncTask = CreateAsyncImportTask(false);
                if (asyncTask != null)
                {
                    _onlineTaskMaster = asyncTask;
                    //Start the work running
                    StartRunningAsyncTask();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring export:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Hides all the Inventory/Export/Import panels except the one specified.
        /// Positions and shows the current panel
        /// </summary>
        /// <param name="panelExportSite"></param>
        private void ShowSinglePanelHideOthers(Panel panelShowMe)
        {
            //Hide everything else
            HidePanelIfNotMatch(panelExportSite, panelShowMe);
            HidePanelIfNotMatch(panelImportSite, panelShowMe);
            HidePanelIfNotMatch(panelInventorySite, panelShowMe);
            HidePanelIfNotMatch(panelRunCommandLine, panelShowMe);

            if (panelShowMe != null)
            {
                //Position it
                panelShowMe.Left = 0;
                panelShowMe.Top = panelTopSplitter.Bottom + 5;
                panelShowMe.Width = this.ClientSize.Width;

                //Clean up visual settings we had in design mode to make the panels easy to handle
                panelShowMe.BorderStyle = BorderStyle.None;
                panelShowMe.BackColor = Color.White;
                //Show it
                panelShowMe.Visible = true;

                //Position display content around the panel
                splitContainerStatus.Top = panelShowMe.Bottom + 10;
                splitContainerStatus.Height = this.ClientSize.Height - splitContainerStatus.Top;
                panelTopSplitter.Visible = false;


            }
            else
            { 
                panelTopSplitter.Visible = false;
            }
        }

        /// <summary>
        /// Hide the panel if it does not match
        /// </summary>
        /// <param name="panelHideIfNotMatch"></param>
        /// <param name="panelMatchMe"></param>
        private void HidePanelIfNotMatch(Panel panelHideIfNotMatch, Panel panelMatchMe)
        {
            if(panelHideIfNotMatch != panelMatchMe)
            {
                panelHideIfNotMatch.Visible = false;
            }
        }

        private void btnShowInventoryFrom_Click(object sender, EventArgs e)
        {
//            panelInventorySite.Height = txtInventoryExampleCommandLine.Bottom + 10;
//            ShowSinglePanelHideOthers(panelInventorySite);
        }

        private void buttonRunInventorySite_Click(object sender, EventArgs e)
        {
            _onlineTaskMaster = null;
            try
            {
                var asyncTask = CreateAsyncInventoryTask(false);
                if (asyncTask != null)
                {
                    _onlineTaskMaster = asyncTask;
                    //Start the work running
                    StartRunningAsyncTask();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring inventory:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// Allows the start of the program to pass in command line arguments
        /// </summary>
        /// <param name="commandLine"></param>
        internal void SetStartupCommandLine(CommandLineParser commandLine)
        {
            _startupCommandLine = commandLine;
        }
        /// <summary>
        /// Called to run us in commandn line mode
        /// </summary>
        /// <param name="commandLine"></param>
        internal void RunStartupCommandLine()
        {
            //Set up the UI to indicate we are going to run a commandl ine
            ShowSinglePanelHideOthers(panelRunCommandLine);
            textBoxErrors.Text = "";
            textBoxStatus.Text = "";

            TaskMaster task = null;
            //Parse the details from the command line
            try
            {
                task = TaskMaster.FromCommandLine(_startupCommandLine);
            }
            catch(Exception exParseCommandLine)
            {
                textBoxErrors.Text = "Error parsing command line: " + exParseCommandLine;
            }

            _onlineTaskMaster = task;

            //Set the work running
            StartRunningAsyncTask();
        }

        /// <summary>
        /// Get the DB credentials path
        /// </summary>
        /// <returns></returns>
        private string GetDBCredentialsImportPath()
        {
            string dbCrendentialsPath = txtDBCredentialsImport.Text.Trim();
            if(dbCrendentialsPath != DefaultTextDBCredentialsImport)
            {
                return dbCrendentialsPath;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            txtDBCredentialsImport.Text = DefaultTextDBCredentialsImport;

            //Hide all the panels
            ShowSinglePanelHideOthers(null);
            PopulateChooseActionUI();

            if (_startupCommandLine != null)
            {
                RunStartupCommandLine();
            }
        }

        private const string ListAction_Default = "Choose an action";
        private const string ListAction_Inventory = "Generate CSV file of site's inventory";
        private const string ListAction_ExportSite = "Export site contents to local directory";
        private const string ListAction_ImportSite = "Upload from file system into site";
        private void PopulateChooseActionUI()
        {
            var items = comboBoxChooseAction.Items;
            items.Clear();
            items.Add(ListAction_Default);
            items.Add(ListAction_Inventory);
            items.Add(ListAction_ExportSite);
            items.Add(ListAction_ImportSite);

            comboBoxChooseAction.SelectedIndex = 0;
        }

        private void buttonRunInventorySiteCommandLine_Click(object sender, EventArgs e)
        {

        }

        private void btnLinkInventoryCommandLine_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //Create it, but don't run it...
                CreateAsyncInventoryTask(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring inventory:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBoxChooseAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedAction = comboBoxChooseAction.Text;
            if(selectedAction == ListAction_Inventory)
            {
                panelInventorySite.Height = txtInventoryExampleCommandLine.Bottom + 10;
                ShowSinglePanelHideOthers(panelInventorySite);
            }
            else if(selectedAction == ListAction_ExportSite)
            {
                panelExportSite.Height = textSiteExportCommandLine.Bottom + 10;
                ShowSinglePanelHideOthers(panelExportSite);
            }
            else if (selectedAction == ListAction_ImportSite)
            {
                panelImportSite.Height = txtSiteImportCommandLineExample.Bottom + 10;
                ShowSinglePanelHideOthers(panelImportSite);
            }
            else
            {
                ShowSinglePanelHideOthers(null);
            }
        }

        private void btnLinkExportSiteCommandLine_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //Create it, but don't run it...
                CreateAsyncExportTask(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring inventory:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Called to quit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAbortRun_Click(object sender, EventArgs e)
        {
            var taskMaster = _onlineTaskMaster;
            if(taskMaster != null)
            {
                taskMaster.StatusLog.AddError("User aborted execution");
                taskMaster.Abort(true);
            }
        }


        /// <summary>
        /// Called to get the import site command line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLinkImportCommandLine_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //Create the task but don't set it running
                var asyncTask = CreateAsyncImportTask(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conifiguring import:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chkImportRemapContentOwnership_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
