using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dreamer
{
    internal static class CWTs
    {
        private static readonly ConditionalWeakTable<Player, DreamerData> playerCWT = new();
        public static bool TryGetData(Player player, out DreamerData data)
        {
            if (player.SlugCatClass == Plugin.Dreamer)
            {
                data = playerCWT.GetValue(player, _ => new DreamerData());
                return true;
            }
            data = null;
            return false;
        }
    }

    internal class DreamerData
    {
        public bool astral = false;
        public bool astralKeyPress = false;
        public Projection? projection = null;
    }
}
