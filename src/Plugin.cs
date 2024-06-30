using System;
using BepInEx;
using Dreamer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace Dreamer
{
    [BepInPlugin(MOD_ID, "The Dreamer", "1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "alduris.dreamer";
        public static readonly SlugcatStats.Name Dreamer = new("Dreamer", false);

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

                Logger.LogInfo("Whee!");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
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
                    Debug.Log("HI");
                }
                else if (data.astralKeyPress && !input.pckp && !input.thrw)
                {
                    data.astralKeyPress = false;
                    Debug.Log("BYE");
                }

                if (data.astral)
                {
                    self.stun = 2;
                    (self.graphicsModule as PlayerGraphics).blink = 2;

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
