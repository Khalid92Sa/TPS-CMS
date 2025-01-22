using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Web.Utils;

public class AttachmentHelper
{
    public static async Task<FileStream> handleUpload(IFormFile file, string saveLocation)
    {
        string filePath = Path.Combine(saveLocation, file.FileName);
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        FileStream attachmentStream = new FileStream(filePath, FileMode.Open);
        return attachmentStream;

    }
    public static byte[] ReadStream(Stream stream)
    {
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static void removeFile(string fileName, string saveLocation)
    {
        string filePath = Path.Combine(saveLocation, fileName);
        File.Delete(filePath);
    }
}