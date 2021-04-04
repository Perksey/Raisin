using System;

namespace Raisin.PluginSystem
{
    public class SavedPlugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }
        public Version RaisinVersion { get; set; }
    }
}