using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Schedule in a Server's site
/// </summary>
partial class SiteSchedule : IHasSiteItemId
{
    public readonly string Id;
    public readonly string ScheduleName;
    public readonly string ScheduleState;
    public readonly string PriorityText;
    public readonly string ScheduleType;
    public readonly string ScheduleFrequency;
    public readonly string NextRunUTCText;
    public readonly string EndScheduleIfHourlyUTC = null;

    /// <summary>
    /// TRUE if this schedule is of type extract refresh
    /// </summary>
    public bool IsExtractRefreshSchedule
    {
        get
        {
            if(string.Compare(this.ScheduleType, "extract", true) == 0)
            {
                return true;
            }
            return false;
        }
    }

    /// [2017-05-25] Currently all Schedules are Server scoped (but available to query by site admins). 
    ///              In the future this class may need to be amdended to indicate which schedules are server vs. site
    ///              scoped, if site scoped schedules are added.
    public const string ScheduleScope = "Server";

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="scheduleNode"></param>
    public SiteSchedule(XmlNode scheduleNode)
    {
        var sbDevNotes = new StringBuilder();

        if(scheduleNode.Name.ToLower() != "schedule")
        {
            AppDiagnostics.Assert(false, "Not a schedule");
            throw new Exception("Unexpected content - not schedule");
        }

        this.Id = scheduleNode.Attributes["id"].Value;
        this.ScheduleName = scheduleNode.Attributes["name"].Value;
        this.ScheduleState = scheduleNode.Attributes["state"].Value;
        this.ScheduleType = scheduleNode.Attributes["type"].Value;
        this.ScheduleFrequency = scheduleNode.Attributes["frequency"].Value;
        this.NextRunUTCText = scheduleNode.Attributes["nextRunAt"].Value;

        //This attribute is only present in hourly scheduled jobs
        const string attributeEndHourlyAt = "endScheduleAt";
        if(scheduleNode.Attributes[attributeEndHourlyAt] != null)
        { 
            this.EndScheduleIfHourlyUTC = scheduleNode.Attributes[attributeEndHourlyAt].Value;
        }
        this.PriorityText = scheduleNode.Attributes["priority"].Value;


        this.DeveloperNotes = sbDevNotes.ToString();
    }

    public override string ToString()
    {
        return "Schedule: " + this.Id + "/" + this.ScheduleName;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
