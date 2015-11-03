using System;

/// <summary>
/// Information we need to know to generate correct URLs that point to content on a Tableau Server's site
/// </summary>
public interface ITableauServerSiteInfo
{
    string ServerName { get; }
    string ServerNameWithProtocol { get; }
    ServerProtocol Protocol { get; }
    string SiteId { get; }
}
