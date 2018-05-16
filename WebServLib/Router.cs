using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensions;

namespace WebServLib
{
    public class Router
    {
        public string WebsitePath { get; set; }

        private Dictionary<string, ExtensionInfo> extFolderMap;

        public Router()
        {
            extFolderMap = new Dictionary<string, ExtensionInfo>
            {
                {"ico",  new ExtensionInfo {Loader = ImageLoader, ContentType = "image/ico"}},
                {"png",  new ExtensionInfo {Loader = ImageLoader, ContentType = "image/png"}},
                {"jpg",  new ExtensionInfo {Loader = ImageLoader, ContentType = "image/jpg"}},
                {"gif",  new ExtensionInfo {Loader = ImageLoader, ContentType = "image/gif"}},
                {"bmp",  new ExtensionInfo {Loader = ImageLoader, ContentType = "image/bmp"}},
                {"html", new ExtensionInfo {Loader = PageLoader , ContentType = "text/html"}},
                {"css",  new ExtensionInfo {Loader = FileLoader , ContentType = "text/css"}},
                {"js",   new ExtensionInfo {Loader = FileLoader , ContentType = "text/javascript"}},
                {"",     new ExtensionInfo {Loader = PageLoader , ContentType = "text/html"}}
            };
        }

        private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            var fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fStream);
            var ret = new ResponsePacket
            {
                Data = br.ReadBytes((int) fStream.Length),
                ContentType = extInfo.ContentType
            };

            br.Close();
            fStream.Close();

            return ret;
        }

        public ResponsePacket Route(string verb, string path, Dictionary<string, string> kvParams)
        {
            var ext = path.RightOf('.');
            ResponsePacket ret = null;

            if (extFolderMap.TryGetValue(ext, out var extInfo))
            {
                // Strip off leading '/' & reformat with path separator.
                var fullPath = Path.Combine(WebsitePath, path);
                ret = extInfo.Loader(fullPath, ext, extInfo);
            }

            return ret;
        }

        private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            var text = File.ReadAllText(fullPath);
            var ret = new ResponsePacket
            {
                Data = Encoding.UTF8.GetBytes(text),
                ContentType = extInfo.ContentType,
                Encoding = Encoding.UTF8
            };

            return ret;
        }

        private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret;

            if (fullPath == WebsitePath)
            {
                ret = Route("GET", "", null);
            }
            else
            {
                if (string.IsNullOrEmpty(ext))
                {
                    fullPath = fullPath + ".html";
                }

                fullPath = WebsitePath + "\\Pages" + fullPath.RightOf(WebsitePath);
                ret = FileLoader(fullPath, ext, extInfo);
            }

            return ret;
        }
    }

    public class ResponsePacket
    {
        public string Redirect { get; set; }
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; }
    }

    internal class ExtensionInfo
    {
        public string ContentType { get; set; }
        public Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
    }
}
