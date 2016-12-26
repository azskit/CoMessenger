using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CimPlugin.Plugin.Groups
{
    public abstract class Group
    {
        public string GroupId { get; set; }
        public List<string> UserIds { get; } = new List<string>();
        public string DisplayName { get; set; }

    }
}
