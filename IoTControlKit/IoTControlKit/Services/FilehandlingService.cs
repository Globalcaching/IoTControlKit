using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace IoTControlKit.Services
{
    public class FileHandlingService : BaseService
    {
        private static FileHandlingService _uniqueInstance = null;
        private static object _lockObject = new object();

        private FileHandlingService()
        {
        }

        public static FileHandlingService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new FileHandlingService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GetExtension(HttpRequest request, string targetFolder, string targetFileName)
        {
            var extension = string.Empty;

            for (int i = 0; i < request.Form.Files.Count; i++)
            {
                // Pointer to file
                var file = request.Form.Files[i];
                var filename = Path.GetFileName(file.FileName);
                extension = Path.GetExtension(filename);
            }

            return extension;
        }

        public List<string> UploadFiles(HttpRequest request, string targetFolder, string targetFileName)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < request.Form.Files.Count; i++)
            {
                // Pointer to file
                var file = request.Form.Files[i];
                var filename = string.IsNullOrEmpty(targetFileName) ? Path.GetFileName(file.FileName) : targetFileName;
                var filepath = Path.Combine(targetFolder, filename);

                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Delete(filepath);
                }

                using (var fs = System.IO.File.Create(filepath))
                {
                    byte[] buffer = new byte[4096];
                    var inputStream = file.OpenReadStream();
                    var read = inputStream.Read(buffer, 0, buffer.Length);
                    int totalread = 0;
                    while (totalread < file.Length && read > 0)
                    {
                        totalread += read;
                        fs.Write(buffer, 0, read);
                        read = inputStream.Read(buffer, 0, buffer.Length);
                    }
                }
                result.Add(filepath);
            }
            return result;
        }

    }
}