using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
class MimeWriterXml : MimeWriterBase
{
    private readonly string _mimeBoundary;
    private readonly string _xmlPayload;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="xmlPayload">The XML text we want to package in MIME</param>
    public MimeWriterXml(string xmlPayload)
    {
        _xmlPayload = xmlPayload;

        //Should be safe but we should do extra work to double check that this GUID is not encoded in the byte array (probability is very low)
        _mimeBoundary = Guid.NewGuid().ToString(); 
    }

    /// <summary>
    /// Marker between MIME segments
    /// </summary>
    public override string MimeBoundaryMarker
    {
        get { return _mimeBoundary; }
    }

    /// <summary>
    /// Builds and in memory stream containing the binary data in a multi-part MIME that is ready for upload
    /// </summary>
    /// <returns></returns>
    public override byte [] GenerateMimeEncodedChunk()
    {
        var inMemoryStream = new MemoryStream();
        using (inMemoryStream)
        {
            WriteBoundaryLine(inMemoryStream);
//            WriteAsciiLine(inMemoryStream, "Content-Disposition: name=\"request_payload\"");
            WriteAsciiLine(inMemoryStream, "Content-Disposition: form-data; name=\"request_payload\""); //Server v9.2 is stricter about request syntax
            WriteAsciiLine(inMemoryStream, "Content-Type: text/xml");
            WriteAsciiLine(inMemoryStream);
            WriteUtf8Line(inMemoryStream, _xmlPayload); //Write out the XML payload as Utf8

            WriteBoundaryLine(inMemoryStream, true); //Write the closing marker for the MIME
            //Go to the beginning
            inMemoryStream.Seek(0, SeekOrigin.Begin);
            return inMemoryStream.ToArray();
        }
    }

}
