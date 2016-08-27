using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

internal partial class DatasourcePublishSettings
{
    /// <summary>
    /// The name of the owner of the content (NULL if unknown)
    /// </summary>
    public readonly string OwnerName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ownerName"></param>
    public DatasourcePublishSettings(string ownerName)
    {
        this.OwnerName = ownerName;
    }
}
