using Repackinator.Core.Helpers;
using Repackinator.Core.Models;

namespace Repackinator.Models
{
    public class GroupingOption(GroupingOptionType type)
    {
        public GroupingOptionType Type { get; set; } = type;

        public string Name
        {
            get
            {
                return Utility.EnumValueToString(Type);
            }
        }
    }
}
