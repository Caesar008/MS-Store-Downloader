using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MS_Store_Downloader
{
    internal class PackageInfo
    {
        internal string Name;
        internal string Extension;
        internal string Uri;
        internal string RevisionNumber;
        internal string UpdateID;
        internal string ID;

        public PackageInfo(string name, string extension, string uri, string revisionNumber, string updateID, string id)
        {
            Name = name;
            Extension = extension;
            Uri = uri;
            RevisionNumber = revisionNumber;
            UpdateID = updateID;
            ID = id;
        }
    }
}
