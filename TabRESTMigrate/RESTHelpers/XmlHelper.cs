using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/// <summary>
/// Useful XML methods
/// </summary>
internal static class XmlHelper
{
    public const string XmlAttribute_Value = "value";

    /// <summary>
    /// Return the XML Settings
    /// </summary>
    /// <returns></returns>
    public static XmlWriterSettings XmlSettingsForWebRequests
    {
        get
        {
            XmlWriterSettings xWriterSettings = new XmlWriterSettings();
            xWriterSettings.Encoding = new System.Text.UTF8Encoding(false);  //Use UTF-8; Never use a byte order marker, 
            xWriterSettings.OmitXmlDeclaration = true; //No XML header
            return xWriterSettings;
        }
    }

    /// <summary>
    /// Gets the attribute or returns a default value
    /// </summary>
    /// <param name="xNode"></param>
    /// <param name="attributeName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static string RequiredParseXmlAttribute(XmlNode xNode, string attributeName)
    {
        var attr = xNode.Attributes[attributeName];
        if ((attr == null) || (string.IsNullOrWhiteSpace(attr.Value)))
        {
            throw new Exception("No xml attribute found for " + attributeName);
        }

        return attr.Value;
    }

    /// <summary>
    /// Gets the attribute or returns a default value
    /// </summary>
    /// <param name="xNode"></param>
    /// <param name="attributeName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static string SafeParseXmlAttribute(XmlNode xNode, string attributeName, string defaultValue)
    {
        var attr = xNode.Attributes[attributeName];
        if ((attr == null) || (string.IsNullOrWhiteSpace(attr.Value)))
        {
            return defaultValue;
        }

        return attr.Value;
    }

    public static bool SafeParseXmlAttribute_Bool(XmlNode xNode, string attributeName, bool defaultValue)
    {
        var attr = xNode.Attributes[attributeName];
        if ((attr == null) || (string.IsNullOrWhiteSpace(attr.Value)))
        {
            return defaultValue;
        }

        return System.Convert.ToBoolean(attr.Value);
    }

    /// <summary>
    /// Creates a namespace manager needed for XML XPath queries
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static XmlNamespaceManager CreateTableauXmlNamespaceManager(string prefix)
    {
        var msTable = new NameTable();
        var ns = new XmlNamespaceManager(msTable);
        //[2016-05-08] This app has been changed to use the 2.3 version of the Tableau Server REST API.  
        //             Between 2.0 and 2.3, a change was made to the namespace returned by the APIs.  
        //             Supporting this change requires change in the XML namespace. Without this, XPATH
        //             queries on the XML will not return results.
        //Example (v2.0 REST APIs): <?xml version=\"1.0\" encoding=\"UTF-8\"?> < tsResponse xmlns =\"http://tableausoftware.com/api\" 
        //Example (v2.3 REST APIs): <?xml version=\"1.0\" encoding=\"UTF-8\"?> < tsResponse xmlns =\"http://tableau.com/api\" 
        //ns.AddNamespace(prefix, "http://tableausoftware.com/api");
        ns.AddNamespace(prefix, "http://tableau.com/api");

        return ns;
    }

    /// <summary>
    /// Recursive search for first matching node, returning attribute
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="nodeName"></param>
    /// <returns></returns>
    public static string GetAttributeValue(XmlNode xmlNode, string nodeName, string attributeName, bool searchSiblings, out bool found)
    {
        found = false;
        if (xmlNode == null)
        {
            return "";
        }

        if (xmlNode.Name == nodeName)
        {
            var attribute = xmlNode.Attributes[attributeName];
            //No attribute
            if (attribute == null)
            {
                return "";
            }
            found = true;
            return attribute.Value;
        }

        //Do any of the current nodes children have the value?
        string childHasValue = GetAttributeValue(xmlNode.FirstChild, nodeName, attributeName, true, out found);
        if (found)
        {
            return childHasValue;
        }

        if (searchSiblings)
        {
            var nextSibling = xmlNode.NextSibling;
            while (nextSibling != null)
            {
                var siblingAttribute = GetAttributeValue(nextSibling, nodeName, attributeName, false, out found);
                if (found)
                {
                    return siblingAttribute;
                }

                nextSibling = nextSibling.NextSibling;
            }
        }

        return "";
    }

    /// <summary>
    /// Writes out a true/false attribute value
    /// </summary>
    /// <param name="xmlWriter"></param>
    /// <param name="attributeName"></param>
    /// <param name="value"></param>
    public static void WriteBooleanAttribute(XmlWriter xmlWriter, string attributeName, bool value)
    {
        string valueText = BoolToXmlText(value);
        xmlWriter.WriteAttributeString(attributeName, valueText);
    }

    /// <summary>
    /// Gives us a culture invariant true/false text for XML
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string BoolToXmlText(bool value)
    {
        if (value)
        {
            return "true";
        }
        else
        {
            return "false";
        }
    }

    /// <summary>
    /// Reads a true/false value
    /// </summary>
    /// <param name="xNode"></param>
    /// <param name="attributeName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    internal static bool ReadBooleanAttribute(XmlNode xNode, string attributeName, bool defaultValue)
    {
        var attribute = xNode.Attributes[attributeName];
        if(attribute == null)
        {
            return defaultValue;
        }

        string attributeValue = attribute.Value;
        attributeValue = attributeValue.Trim().ToLower();
        if(attributeValue == "true")
        {
            return true;
        }

        if (attributeValue == "false")
        {
            return false;
        }

        throw new Exception("Invalid boolean value " + attributeValue);
    }

    /// <summary>
    /// Reads a true/false value
    /// </summary>
    /// <param name="xNode"></param>
    /// <param name="attributeName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    internal static string ReadTextAttribute(XmlNode xNode, string attributeName, string defaultValue = "")
    {
        var attribute = xNode.Attributes[attributeName];
        if (attribute == null)
        {
            return defaultValue;
        }

        return attribute.Value;
    }


    /// <summary>
    /// Write a simple name/value pair as an XML element
    /// </summary>
    /// <param name="xmlWriter"></param>
    /// <param name="elementName"></param>
    /// <param name="value"></param>
    internal static void WriteValueElement(XmlWriter xmlWriter, string elementName, bool value)
    {
        xmlWriter.WriteStartElement(elementName);
        XmlHelper.WriteBooleanAttribute(xmlWriter, XmlAttribute_Value, value);
        xmlWriter.WriteEndElement();
    }
    /// <summary>
    /// Write a simple name/value pair as an XML element
    /// </summary>
    /// <param name="xmlWriter"></param>
    /// <param name="elementName"></param>
    /// <param name="value"></param>
    internal static void WriteValueElement(XmlWriter xmlWriter, string elementName, string value)
    {
        xmlWriter.WriteStartElement(elementName);
        xmlWriter.WriteAttributeString(XmlAttribute_Value, value);
        xmlWriter.WriteEndElement();
    }

}
