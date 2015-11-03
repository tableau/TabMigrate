using System;
using System.Collections.Generic;
using System.Text;
/// <summary>
/// Object can answer questions about tags
/// </summary>
interface ITagSetInfo
{
    /// <summary>
    /// True of if the content has the specified tag
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    bool IsTaggedWith(string tagText);

    /// <summary>
    /// Text string containing all the tags
    /// </summary>
    /// <returns></returns>
    string TagSetText
    {
        get;
    }
}