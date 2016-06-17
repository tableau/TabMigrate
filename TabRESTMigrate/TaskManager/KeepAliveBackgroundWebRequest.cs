using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// A background taks that runs intermittently to request data
/// </summary>
internal class KeepAliveBackgroundWebRequest : KeepAliveBackgroundTaskBase
{

    private readonly TableauServerUrls _serverUrls;
    private readonly TableauServerSignIn _signIn;
    /// <summary>
    /// Constructor
    /// </summary>
    public KeepAliveBackgroundWebRequest(TableauServerUrls serverUrls, TableauServerSignIn signIn) 
        : base(TimeSpan.FromSeconds(60 * 2)) //Choose a keep alive time
    {
        _serverUrls = serverUrls;
        _signIn = signIn;
    }

    /// <summary>
    /// Perform the background task
    /// </summary>
    protected override void PerformKeepAliveTask()
    {
        //If we are no longer signed in, then abort the background work
        if(!_signIn.IsSignedIn)
        {
            this.ExitAsync(); //Tell the thread to quit
        }

        var statusLog = _signIn.StatusLog;
        statusLog.AddStatusHeader("Performing keep alive web request");
        try
        {
            //Try to download the projects list
            var downloadProjectsList = new DownloadProjectsList(_serverUrls, _signIn);
            downloadProjectsList.ExecuteRequest();
        }
        catch(Exception exRunningError)
        {
            statusLog.AddError("Error running keep alive work: " + exRunningError.Message);
        }
    }

}
