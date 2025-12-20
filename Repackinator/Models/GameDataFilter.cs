using Repackinator.Core.Helpers;
using Repackinator.Core.Models;

namespace Repackinator.Models
{
    public class GameDataFilter(GameDataFilterType type)
    {
        public GameDataFilterType Type { get; set; } = type;

        public string Name
        {
            get
            {
                return Utility.EnumValueToString(Type);
            }
        }
    }
}
