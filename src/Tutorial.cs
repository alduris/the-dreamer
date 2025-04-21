using System.Runtime.CompilerServices;

namespace Dreamer
{
    internal class Tutorial : UpdatableAndDeletable
    {
        private static readonly ConditionalWeakTable<RainWorldGame, object> shown = new();

        public Tutorial(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!shown.TryGetValue(room.game, out _) && room.game.session is StoryGameSession && room.game.Players.Count > 0 && room.game.Players[0].realizedCreature != null && room.game.Players[0].realizedCreature.room == room)
            {
                shown.Add(room.game, new());
                room.game.cameras[0].hud.textPrompt.AddMessage(room.game.rainWorld.inGameTranslator.Translate("Press SPECIAL to project. Press again to teleport to your projection."), 200, 300, true, true);
                if (room.game.cameras[0].hud.textPrompt.subregionTracker != null)
                {
                    room.game.cameras[0].hud.textPrompt.subregionTracker.lastShownRegion = 1;
                }
                Destroy();
            }
        }
    }
}
