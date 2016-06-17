using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// Base class for "keep alive" work that runs in a background interval
/// </summary>
internal abstract class KeepAliveBackgroundTaskBase
{
    /// <summary>
    /// Class that wraps atomic reference to a date/time
    /// </summary>
    class LastUpdateTime
    {
        public readonly DateTime Timestamp;
        public LastUpdateTime()
        {
            this.Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Inhereting classes must implement this method.  It is the work that needs to be performed to keep us alive
    /// </summary>
    protected abstract void PerformKeepAliveTask();

    /// <summary>
    /// Constructor
    /// </summary>
    public KeepAliveBackgroundTaskBase(TimeSpan keepAliveInterval)
    {
        _keepAliveInterval = keepAliveInterval;
    }
    
    private readonly SimpleLatch _exitThreadLatch = new SimpleLatch();
    private LastUpdateTime _lastKeepAliveActionTime;
    private readonly TimeSpan _keepAliveInterval;

    /// <summary>
    /// Signals that the keep alive thread should go away
    /// </summary>
    public void ExitAsync()
    {
        _exitThreadLatch.Trigger(); //Tells the background thread to stop running.
    }


    /// <summary>
    /// Set the background task running
    /// </summary>
    public void Execute()
    {
        var threadStart = new ThreadStart(StartThreadHere);
        _lastKeepAliveActionTime =  new LastUpdateTime();
        var thread = new Thread(threadStart);
        thread.Start();
    }


    /// <summary>
    /// Thread entry point
    /// </summary>
    void StartThreadHere()
    {
        //Run a loop until we are told to sleep
        while(!_exitThreadLatch.Value)
        {
            Thread.Sleep(2000); //Sleep for at least 2 seconds (arbitrary number to make sure we don't needlessly run)

            //If we have not been told to exit AND our the keep-alive task has not run within the desired interaval,
            //then run it
            if((!_exitThreadLatch.Value)  && 
                ((DateTime.Now - _lastKeepAliveActionTime.Timestamp) > _keepAliveInterval))
            {
                PerformKeepAliveTask();

                //Update the last run keep-alive time
                _lastKeepAliveActionTime = new LastUpdateTime();
            }
        }
        //Exit thread
    }
}
