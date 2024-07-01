using System;
using System.Security.Permissions;
using BepInEx;
using SlugBase.Features;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Dreamer
{
    [BepInPlugin(MOD_ID, "The Dreamer", "1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "alduris.dreamer";
        public static readonly SlugcatStats.Name Dreamer = new("Dreamer", false);

        public static readonly PlayerFeature<bool> AffectSelf = FeatureTypes.PlayerBool("dreamer_moveself");
        public static readonly PlayerFeature<float> PushStrength = FeatureTypes.PlayerFloat("dreamer_maxstr");
        public static readonly PlayerFeature<float> PushMult = FeatureTypes.PlayerFloat("dreamer_strmult");

        // Add hooks
        public void OnEnable()
        {
            try
            {
                // Controls + movement
                On.Player.Update += Player_Update;

                // Stun
                On.Player.Stun += Player_Stun;
                On.Player.JumpOnChunk += Player_JumpOnChunk;

                // Random stuff
                On.RainWorldGame.Update += RainWorldGame_Update;

                Logger.LogInfo("Whee!");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (!(self.session as StoryGameSession).saveState.miscWorldSaveData.pebblesEnergyTaken)
            {
                (self.session as StoryGameSession).saveState.miscWorldSaveData.pebblesEnergyTaken = true;

                // Reload rooms (ripped straight from RM_CORE room script with slight modifications)
                if (self.world != null)
                {
                    for (int i = self.world.activeRooms.Count - 1; i >= 0; i--)
                    {
                        if (self.world.activeRooms[i] != self.cameras[0].room)
                        {
                            if (self.roomRealizer != null)
                            {
                                self.roomRealizer.KillRoom(self.world.activeRooms[i].abstractRoom);
                            }
                            else
                            {
                                self.world.activeRooms[i].abstractRoom.Abstractize();
                            }
                        }
                    }
                }
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (CWTs.TryGetData(self, out var data))
            {
                // Toggle ability
                var input = self.controller?.GetInput() ?? RWInput.PlayerInput(self.playerState.playerNumber);
                if (input.pckp && input.thrw && !data.astralKeyPress && (self.stun == 0 || data.astral) && self.AI == null)
                {
                    data.astralKeyPress = true;
                    data.astral = !data.astral;
                    self.PlayHUDSound(data.astral ? SoundID.SS_AI_Give_The_Mark_Boom : SoundID.Snail_Pop);
                }
                else if (data.astralKeyPress && !input.pckp && !input.thrw)
                {
                    data.astralKeyPress = false;
                }

                if (data.astral)
                {
                    self.stun = 11;

                    if (data.projection == null)
                    {
                        data.projection = new Projection(self);
                        self.room.AddObject(data.projection);
                    }
                    else
                    {
                        data.projection.MovementUpdate(input);
                    }
                }
                else
                {
                    data.projection?.Destroy();
                    data.projection = null;
                }
            }
        }

        private void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig(self, st);
            if (CWTs.TryGetData(self, out var data) && data.astral)
            {
                data.astral = false;
            }
        }

        private void Player_JumpOnChunk(On.Player.orig_JumpOnChunk orig, Player self)
        {
            int stun = self.stun;
            orig(self);
            if (CWTs.TryGetData(self, out var data) && data.astral && self.stun != stun)
            {
                data.astral = false;
            }
        }
    }
}
