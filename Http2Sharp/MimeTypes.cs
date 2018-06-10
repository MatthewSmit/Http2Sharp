using System.Collections.Generic;

namespace Http2Sharp
{
    public static class MimeTypes
    {
        private static readonly IDictionary<string, string> extensionMimeMapping = new Dictionary<string, string>
        {
            {".ico", IMAGE_ICON},
        };

        public const string IMAGE_ICON = "image/x-icon";

        public const string OCTET_STREAM = "application/octet-stream";

        public static string FindFromExtension(string extension)
        {
            if (extensionMimeMapping.TryGetValue(extension, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
