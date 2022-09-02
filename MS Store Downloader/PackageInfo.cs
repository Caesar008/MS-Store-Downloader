using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MS_Store_Downloader
{
    internal class PackageInfo : IComparable<PackageInfo>
    {
        internal string Name;
        internal string Extension;
        internal string Uri;
        internal string RevisionNumber;
        internal string UpdateID;
        internal string ID;
        internal double Size;

        public PackageInfo(string name, string extension, string uri, string revisionNumber, string updateID, string id, double size)
        {
            Name = name;
            Extension = extension;
            Uri = uri;
            RevisionNumber = revisionNumber;
            UpdateID = updateID;
            ID = id;
            this.Size = size;
        }

        public int CompareTo(PackageInfo other)
        {
            return this.Name.CompareTo(other.Name);
        }
    }
}
