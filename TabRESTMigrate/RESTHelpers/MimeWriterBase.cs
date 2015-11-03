using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Base class for working with MIME data
/// </summary>
abstract class MimeWriterBase
{
    /// <summary>
    /// What is the unique boundary marker between MIME parts
    /// </summary>
    public abstract string MimeBoundaryMarker
    { get; }

    /// <summary>
    ///Generate the MIME package that we are going to send
    /// </summary>
    /// <returns></returns>
    public abstract byte[] GenerateMimeEncodedChunk();


    /// <summary>
    /// Writes the boundary line between MIME sections
    /// </summary>
    /// <param name="mStream"></param>
    /// <param name="includeClose"></param>
    protected void WriteBoundaryLine(MemoryStream mStream, bool includeClose = false)
    {
        if(includeClose)
        {
            WriteAsciiLine(mStream, "--" + MimeBoundaryMarker + "--");
            return;
        }
        WriteAsciiLine(mStream, "--" + MimeBoundaryMarker);
    }

    /// <summary>
    /// Writes ASCII to a MIME stream
    /// </summary>
    /// <param name="mStream"></param>
    /// <param name="text"></param>
    protected static void WriteAsciiString(MemoryStream mStream, string text)
    {
        var byteEncoding = ASCIIEncoding.ASCII.GetBytes(text);
        mStream.Write(byteEncoding, 0, byteEncoding.Length);
    }

    /// <summary>
    /// Write a line of text to a MIME stream
    /// </summary>
    /// <param name="mStream"></param>
    /// <param name="text"></param>
    protected static void WriteAsciiLine(MemoryStream mStream, string text = "")
    {
        WriteAsciiString(mStream, text + "\r\n");
    }

    /// <summary>
    /// Write a UTF8 encoded line ot a mime stream
    /// </summary>
    /// <param name="mStream"></param>
    /// <param name="text"></param>
    protected static void WriteUtf8Line(MemoryStream mStream, string text = "")
    {
        WriteUtf8String(mStream, text + "\r\n");
    }

    /// <summary>
    /// Write UTF8 encoded text to a mime stream
    /// </summary>
    /// <param name="mStream"></param>
    /// <param name="text"></param>
    protected static void WriteUtf8String(MemoryStream mStream, string text)
    {
        var byteEncoding = UTF8Encoding.UTF8.GetBytes(text);
        mStream.Write(byteEncoding, 0, byteEncoding.Length);
    }
}
