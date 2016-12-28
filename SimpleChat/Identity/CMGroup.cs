using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleChat.Identity
{
    [Serializable]
    public class CMGroup
    {
        public string GroupId { get; private set; }
        public List<string> UserIds { get; } = new List<string>();
        public string DisplayName { get; private set; }

        public CMGroup(string displayName, string groupId, IEnumerable<string> userIds)
        {
            DisplayName = displayName;
            GroupId = groupId;
            UserIds.AddRange(userIds);
        }
    }
}
