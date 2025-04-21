using System.Runtime.CompilerServices;

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
        public Projection projection = null;
    }
}
