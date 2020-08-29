using System;
using System.Collections.Generic;
using System.Text;

namespace AutoMessage
{
    public class MessageTypeNode
    {
        public string Name;
        public string OriginalTypeName;
        public string TypeName;
        public List<MessageTypeNode> Children;
    }
}
