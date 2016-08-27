using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

/// <summary>
/// Base class on top of which requests to Tableau Server are based
/// </summary>
abstract class TableauServerRequestBase
{
    /// <summary>
    /// Sends the body text up to the server, using the PUT method
    /// </summary>
    /// <param name="request"></param>
    /// <param name="bodyText"></param>
    protected static void SendPutContents(WebRequest request, string bodyText)
    {
        SendHttpRequestContents_Inner(request, bodyText, "PUT");
    }

    /// <summary>
    /// Sends the body text up to the server
    /// </summary>
    /// <param name="request"></param>
    /// <param name="bodyText"></param>
    protected static void SendPostContents(WebRequest request, string bodyText)
    {
        SendHttpRequestContents_Inner(request, bodyText, "POST");
    }

    /// <summary>
    /// Sends the body text up to the server
    /// </summary>
    /// <param name="request"></param>
    /// <param name="bodyText"></param>
    private static void SendHttpRequestContents_Inner(WebRequest request, string bodyText, string httpMethod = "POST")
    {
        request.Method = httpMethod;
        // Set the ContentType property of the WebRequest.
        request.ContentType = "application/xml;charset=utf-8"; //[2016-06-17] We want to be very explicit on the content type; to give servers very clear instructions on how to parse
        // Set the ContentLength property of the WebRequest.
        byte[] byteArray = Encoding.UTF8.GetBytes(bodyText);
        request.ContentLength = byteArray.Length;

        // Get the request stream.
        var dataStream = request.GetRequestStream();
        // Write the data to the request stream.
        dataStream.Write(byteArray, 0, byteArray.Length);
        // Close the Stream object.

        dataStream.Close();
    }


    /// <summary>
    /// Debugging function: Allows us to test that all of our content was replaced
    /// </summary>
    /// <param name="text"></param>
    protected void AssertTemplateFullyReplaced(string text)
    {
        if (text.Contains("{{iws"))
        {
            System.Diagnostics.Debug.Assert(false, "Text still contains template fragments");
        }
    }

    /// <summary>
    /// Gets the web response as a XML document
    /// </summary>
    protected static System.Xml.XmlDocument GetWebResponseAsXml(WebResponse response)
    {
        string streamText = "";
        var responseStream = response.GetResponseStream();
        using (responseStream)
        {
            var streamReader = new StreamReader(responseStream);
            using (streamReader)
            {
                streamText = streamReader.ReadToEnd();
                streamReader.Close();
            }
            responseStream.Close();
        }

        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.LoadXml(streamText);
        return xmlDoc;
    }

    /// <summary>
    /// Gets the web response as a XML document
    /// </summary>
    protected static string GetWebResponseAsText(WebResponse response)
    {
        string streamText = "";
        var responseStream = response.GetResponseStream();
        using (responseStream)
        {
            var streamReader = new StreamReader(responseStream);
            using (streamReader)
            {
                streamText = streamReader.ReadToEnd();
                streamReader.Close();
            }
            responseStream.Close();
        }

        return streamText;
    }

}
