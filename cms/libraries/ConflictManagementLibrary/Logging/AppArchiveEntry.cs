using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Logging
{
    public class AppArchiveEntry
    {
        public static AppArchiveEntry CreateInstance(string theEntryName, string theFolderPath)
        {
            return new AppArchiveEntry(theEntryName, theFolderPath);
        }

        public string TheEntryName { get; }
        public string TheFolderPath { get; }

        private AppArchiveEntry(string theEntryName, string theFolderPath)
        {
            TheEntryName = theEntryName;
            TheFolderPath = theFolderPath;
        }
    }
}
