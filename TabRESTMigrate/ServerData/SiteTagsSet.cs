using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Information about Tags associated with content in a site
/// </summary>
class SiteTagsSet : ITagSetInfo
{
    IReadOnlyList<SiteTag> _tags;
    public IEnumerable<SiteTag> Tags
    {
        get
        {
            return _tags;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userNode"></param>
    public SiteTagsSet(XmlNode tagsNode)
    {
        if (tagsNode.Name.ToLower() != "tags")
        {
            AppDiagnostics.Assert(false, "Not tags");
            throw new Exception("Unexpected content - not tags");
        }

        //Namespace for XPath queries
        var nsManager = XmlHelper.CreateTableauXmlNamespaceManager("iwsOnline");

        //Build a set of tags
        var tags = new List<SiteTag>();
        //Get the project tags
        var tagsSet = tagsNode.SelectNodes("iwsOnline:tag", nsManager);
        foreach(var tagNode in tagsSet)
        {
            var newTag = new SiteTag((XmlNode) tagNode);
            tags.Add(newTag);

        }
        _tags = tags.AsReadOnly();
    }

    /// <summary>
    /// String representation
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return "tags: " + this.TagSetText;
    }


    /// <summary>
    /// Text of the tag set
    /// </summary>
    public string TagSetText
    {
        get
        {
            if (_tags == null) return "";

            var sb = new StringBuilder();
            int numItems = 0;
            foreach (var tag in _tags)
            {
                if (numItems > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(tag.Label);
                numItems++;
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// True of the specified tag can be found in the set
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public bool IsTaggedWith(string tag)
    {
        var tagSet = _tags;
        //No tags?
        if(tagSet == null)
        {
            return false;
        }

        //Look for hte tag
        foreach(var thisTag in tagSet)
        {
            if(thisTag.Label == tag)
            {
                return true;
            }
        }

        return false;
    }
}
