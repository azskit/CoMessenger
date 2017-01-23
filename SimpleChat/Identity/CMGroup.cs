using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleChat.Identity
{
    [Serializable]
    internal class CMGroup
    {
        internal string GroupId { get; private set; }
        internal List<string> UserIds { get; } = new List<string>();
        internal string DisplayName { get; private set; }

        internal CMGroup(string displayName, string groupId, IEnumerable<string> userIds)
        {
            DisplayName = displayName;
            GroupId = groupId;
            UserIds.AddRange(userIds);
        }
    }
}
