using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuikIso
{
    public static class ResourceLoader
    {
        private static string GetResourceFullName(string resourceFileName, ref Assembly? assembly)
        {
            if (assembly == null)
            {
                assembly = typeof(ResourceLoader).GetTypeInfo().Assembly;
            }
            string[] resourceNames = assembly.GetManifestResourceNames();
            string[] resourcePaths = resourceNames.Where(x => x.EndsWith(resourceFileName, StringComparison.CurrentCultureIgnoreCase)).ToArray();
            if (resourcePaths.Length > 1)
            {
                throw new Exception($"Multiple resources ending with {resourceFileName} found: {Environment.NewLine}{string.Join(Environment.NewLine, resourcePaths)}");
            }
            if (resourcePaths.Length == 0)
            {
                throw new Exception($"Resource ending with {resourceFileName} not found.");
            }
            return resourcePaths.First();
        }

        public static Stream? GetEmbeddedResourceStream(string resourceFileName, Assembly? assembly = null)
        {
            string resourcePath = GetResourceFullName(resourceFileName, ref assembly);            
            return assembly?.GetManifestResourceStream(resourcePath);            
        }

        public static string GetEmbeddedResourceString(string resourceFileName, Assembly? assembly = null)
        {
            Stream? stream = GetEmbeddedResourceStream(resourceFileName, assembly);
            if (stream == null)
            {
                return string.Empty;
            }
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        public static byte[] GetEmbeddedResourceBytes(string resourceFileName, Assembly? assembly = null)
        {
            Stream? stream = GetEmbeddedResourceStream(resourceFileName, assembly);
            if (stream == null)
            {
                return Array.Empty<byte>(); 
            }
            using var streamReader = new MemoryStream();
            stream.CopyTo(streamReader);
            return streamReader.ToArray();
        }
    }
}