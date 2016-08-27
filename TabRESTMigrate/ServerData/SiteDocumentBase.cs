using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Base class for information common to Workbooks and Data Sources, so we don't have lots of redundant code
/// </summary>
abstract class SiteDocumentBase : IHasProjectId, ITagSetInfo, IHasSiteItemId
{
    public readonly string Id;
    public readonly string Name;
    //Note: [2015-10-28] Datasources presently don't return this information
    //public readonly string ContentUrl;
    public readonly string ProjectId;
    public readonly string ProjectName;
    public readonly string OwnerId;
    public readonly SiteTagsSet TagsSet;

    /// <summary>
    /// Any developer/diagnostic notes we want to indicate
    /// </summary>
    public readonly string DeveloperNotes;

    protected SiteDocumentBase(XmlNode xmlNode)
    {
        this.Name = xmlNode.Attributes["name"].Value;
        this.Id = xmlNode.Attributes["id"].Value;

//Note: [2015-10-28] Datasources presently don't return this information
//        this.ContentUrl = xmlNode.Attributes["contentUrl"].Value;

        //Namespace for XPath queries
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");

        //Get the project attributes
        var projectNode = xmlNode.SelectSingleNode("iwsOnline:project", nsManager);
        this.ProjectId = projectNode.Attributes["id"].Value;

        //Some responses do not have the project name
        var attrProjectName = projectNode.Attributes["name"];
        if(attrProjectName != null)
        {
            this.ProjectName = attrProjectName.Value;
        }  

        //Get the owner attributes
        var ownerNode = xmlNode.SelectSingleNode("iwsOnline:owner", nsManager);
        this.OwnerId = ownerNode.Attributes["id"].Value;

        //See if there are tags
        var tagsNode = xmlNode.SelectSingleNode("iwsOnline:tags", nsManager);
        if (tagsNode != null)
        {
            this.TagsSet = new SiteTagsSet(tagsNode);
        }
    }

    /// <summary>
    /// Space delimited list of tags
    /// </summary>
    public string TagSetText
    {
        get
        {
            var tagSet = this.TagsSet;
            if (tagSet == null) return "";
            return tagSet.TagSetText;
        }
    }

    string IHasProjectId.ProjectId
    {
        get { return this.ProjectId; }
    }

    public bool IsTaggedWith(string tagText)
    {
        var tagSet = this.TagsSet;
        if(tagSet == null)
        {
            return false;
        }
        return TagsSet.IsTaggedWith(tagText);
    }

    string IHasSiteItemId.Id
    {
        get { return this.Id; }
    }
}
