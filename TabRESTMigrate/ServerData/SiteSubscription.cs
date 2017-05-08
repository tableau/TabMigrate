using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about a Subscription in a Server's site
/// </summary>
class SiteSubscription : IHasSiteItemId
{
    public readonly string Id;
    public readonly string UserId;
    public readonly string UserName;
    public readonly string ContentId;
    public readonly string ContentType;
    public readonly string ScheduleId;
    public readonly string ScheduleName;
    public readonly string Subject;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="subscriptionNode"></param>
    public SiteSubscription(XmlNode subscriptionNode)
    {
        var sbDevNotes = new StringBuilder();

        if(subscriptionNode.Name.ToLower() != "subscription")
        {
            AppDiagnostics.Assert(false, "Not a subscription");
            throw new Exception("Unexpected content - not subscription");
        }

        this.Id = subscriptionNode.Attributes["id"].Value;
        this.Subject = subscriptionNode.Attributes["subject"].Value;

        //Namespace for XPath queries
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");

        //Get the user attributes
        var userNode = subscriptionNode.SelectSingleNode("iwsOnline:user", nsManager);
        this.UserId = userNode.Attributes["id"].Value;
        this.UserName = userNode.Attributes["name"].Value;

        //Get information about the content being subscribed to (workbook or view)
        var contentNode  = subscriptionNode.SelectSingleNode("iwsOnline:content", nsManager);
        this.ContentId   = contentNode.Attributes["id"].Value;
        this.ContentType = contentNode.Attributes["type"].Value;

        //Get the schedule attibutes
        var scheduleNode = subscriptionNode.SelectSingleNode("iwsOnline:schedule", nsManager);
        this.ScheduleId = scheduleNode.Attributes["id"].Value;
        this.ScheduleName = scheduleNode.Attributes["name"].Value;

        this.DeveloperNotes = sbDevNotes.ToString();
    }

    public override string ToString()
    {
        return "Subscription: " + this.ContentId + "/" + this.UserName;
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
