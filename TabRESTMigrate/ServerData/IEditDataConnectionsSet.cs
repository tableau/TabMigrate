using System;
using System.Collections.Generic;
using System.Text;
/// <summary>
/// Object Allows the setting of the Data Connections
/// </summary>
interface IEditDataConnectionsSet
{
    void SetDataConnections(IEnumerable<SiteConnection> connections);
}