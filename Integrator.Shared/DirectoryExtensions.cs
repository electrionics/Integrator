using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Shared
{
    public static class DirectoryExtensions
    {
        public static List<DirectoryInfo> GetLeafDirectories(this DirectoryInfo root)
        {
            return Directory.EnumerateDirectories(root.FullName, "*.*", SearchOption.AllDirectories)
                .Where(f => !Directory.EnumerateDirectories(f, "*.*", SearchOption.TopDirectoryOnly).Any())
                .Select(p => new DirectoryInfo(p))
                .ToList();
        }
    }
}