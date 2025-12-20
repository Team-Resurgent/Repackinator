using Repackinator.Core.Helpers;
using Repackinator.Core.Models;

namespace Repackinator.UI.Models
{
    public class ScrubOption(ScrubOptionType type)
    {
        public ScrubOptionType Type { get; set; } = type;

        public string Name
        {
            get
            {
                return Utility.EnumValueToString(Type);
            }
        }
    }
}
