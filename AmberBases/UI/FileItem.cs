using System;

namespace AmberBases.UI
{
    internal class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public override string ToString() => Name;
    }
}
