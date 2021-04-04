using System;

namespace Raisin.PluginSystem
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class RaisinPluginAttribute : Attribute
    {
        public RaisinPluginAttribute(params string[] userFacingNamespaces)
        {
            UserFacingNamespaces = userFacingNamespaces;
        }

        public string[] UserFacingNamespaces { get; }
    }
}