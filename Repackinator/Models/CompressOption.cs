using Repackinator.Core.Helpers;
using Repackinator.Core.Models;

namespace Repackinator.UI.Models
{
    public class CompressOption(CompressOptionType type)
    {
        public CompressOptionType Type { get; set; } = type;

        public string Name
        {
            get
            {
                return Utility.EnumValueToString(Type);
            }
        }
    }
}
