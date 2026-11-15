using System.Collections.Generic;

namespace AmberScreen.Save
{
    public class LoadResult
    {
        public bool ok;
        public bool versionMismatch;
        public string savedVersion = "";
        public readonly List<string> skippedKeys = new List<string>();
        public string message = "";
    }
}
