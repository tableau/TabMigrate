using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
/// <summary>
/// Writes out XML for a credential
/// </summary>
static class CredentialXmlHelper
{
    /// <summary>
    /// Writes out the credential element.  Used in Workbook and Data Source publication
    /// </summary>
    /// <param name="xmlWriter"></param>
    /// <param name="credential"></param>
    public static void WriteCredential(XmlWriter xmlWriter, CredentialManager.Credential credential)
    {
        CredentialManager.Connection connection = credential.Connection;

        WriteCredential(xmlWriter, connection.ServerAddress, connection.ServerPort, credential.Name, credential.Password, credential.IsEmbedded);
    }
    /// <summary>
    /// Writes out the credential element.  Used in Workbook and Data Source publication
    /// </summary>
    /// <param name="xmlWriter"></param>
    /// <param name="connectionUserName"></param>
    /// <param name="password"></param>
    /// <param name="isEmbedded"></param>
    public static void WriteCredential(XmlWriter xmlWriter, string serverAddress, string serverPort, string connectionUserName, string password, bool isEmbedded)
    {
        //  <connectionCredentials name="connection-username" password="connection-password"
        //  embed="embed-flag" />
        xmlWriter.WriteStartElement("connection");
        xmlWriter.WriteAttributeString("serverAddress", serverAddress);
        if (!"".Equals(serverPort))
        {
            xmlWriter.WriteAttributeString("serverPort", serverPort);
        }
        xmlWriter.WriteStartElement("connectionCredentials");
        xmlWriter.WriteAttributeString("name", connectionUserName);
        xmlWriter.WriteAttributeString("password", password);
        XmlHelper.WriteBooleanAttribute(xmlWriter, "embed", isEmbedded);
        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndElement();
    }
}
