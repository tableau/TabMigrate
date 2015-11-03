using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Manages a file upload chunk that we can sent to Tableau Server as part of a chunked upload of a file
/// </summary>
class MimeWriterFileUploadChunk : MimeWriterBase
{
    private readonly byte[] _uploadData;
    private readonly int _numberBytes;
    private readonly string _mimeBoundary;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uploadData"></param>
    /// <param name="numBytes"></param>
    public MimeWriterFileUploadChunk(byte[] uploadData, int numBytes)
    {
        _uploadData = uploadData;
        _numberBytes = numBytes;

        //We should do extra work to double check that this GUID is not encoded in the byte array (probability is very low)
       _mimeBoundary = Guid.NewGuid().ToString();
    }

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
            WriteAsciiLine(inMemoryStream, "Content-Disposition: form-data; name=\"request_payload\"");  //Server v9.2 is stricter about request syntax
            WriteAsciiLine(inMemoryStream, "Content-Type: text/xml");
            WriteAsciiLine(inMemoryStream);
            WriteAsciiLine(inMemoryStream); //[2015-10-17] Extra line to meet expectations for 2 empty lines after content

            WriteBoundaryLine(inMemoryStream);
//            WriteAsciiLine(inMemoryStream, "Content-Disposition: name=\"tableau_file\"; filename=\"FILE-NAME\"");
            WriteAsciiLine(inMemoryStream, "Content-Disposition: form-data; name=\"tableau_file\"; filename=\"FILE-NAME\"");  //Server v9.2 is stricter about request syntax
            WriteAsciiLine(inMemoryStream, "Content-Type: application/octet-stream");
            WriteAsciiLine(inMemoryStream);

            //Write the raw binary data
            inMemoryStream.Write(_uploadData, 0, _numberBytes);
            WriteAsciiLine(inMemoryStream);
            WriteBoundaryLine(inMemoryStream, true);

            //Go to the beginning
            inMemoryStream.Seek(0, SeekOrigin.Begin);
            return inMemoryStream.ToArray();
        }
    }

}
