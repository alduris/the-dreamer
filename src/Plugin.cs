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
            // Controls
            On.Player.Update += Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;
            IL.Player.checkInput += Player_checkInput;

            // Stun
            On.Player.Stun += Player_Stun;
            On.Player.JumpOnChunk += Player_JumpOnChunk;
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (CWTs.TryGetData(self, out var data))
            {
                var oldInp = self.input[0];
                if (data.astral)
                {
                    self.input[0] = new Player.InputPackage(oldInp.gamePad, oldInp.controllerType, 0, 0, false, false, false, false, false);
                }
                orig(self, eu);
                self.input[0] = oldInp;
            }
            else
            {
                orig(self, eu);
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (CWTs.TryGetData(self, out var data))
            {
                // Toggle ability
                if (self.input[0].pckp && self.input[0].thrw && !data.astralKeyPress && self.stun == 0 && self.AI == null)
                {
                    data.astralKeyPress = true;
                    data.astral = !data.astral;
                }
                else if (data.astralKeyPress && !self.input[0].pckp && !self.input[0].thrw)
                {
                    data.astralKeyPress = false;
                }

                if (data.astral)
                {
                    self.stun = 2;

                    if (data.projection == null)
                    {
                        data.projection = new Projection(self);
                    }
                    else
                    {
                        data.projection.MovementUpdate(self.input[0]);
                    }
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

        private void Player_checkInput(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.After, x => x.MatchCall(typeof(Creature).GetProperty(nameof(Creature.stun)).GetGetMethod()));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((int stun, Player self) =>
            {
                return (CWTs.TryGetData(self, out var data) && data.astral) ? 0 : stun;
            });
        }
    }
}
