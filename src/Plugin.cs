﻿using System;
using System.Security.Permissions;
using BepInEx;
using SlugBase.Features;
using UnityEngine;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Dreamer
{
    [BepInPlugin(MOD_ID, "The Dreamer", "1.1")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "alduris.dreamer";
        public static readonly SlugcatStats.Name Dreamer = new("Dreamer", false);

        public static readonly PlayerFeature<bool> AffectSelf = FeatureTypes.PlayerBool("dreamer_moveself");
        public static readonly PlayerFeature<float> PushStrength = FeatureTypes.PlayerFloat("dreamer_maxstr");
        public static readonly PlayerFeature<float> PushMult = FeatureTypes.PlayerFloat("dreamer_strmult");
        public static readonly PlayerFeature<float> MaxDist = FeatureTypes.PlayerFloat("dreamer_maxdist");

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

                // Tutorial
                On.Room.Loaded += Room_Loaded;
                On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;

                // Random stuff
                On.RainWorldGame.Update += RainWorldGame_Update;

                Logger.LogInfo("Whee!");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (self.game != null && self.abstractRoom.name == "HI_A22" && self.game.StoryCharacter == Dreamer)
            {
                self.roomSettings.roomSpecificScript = true;
            }
            orig(self);
        }

        private void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            if (room.abstractRoom.name == "HI_A22" && room.game.GetStorySession.saveState.cycleNumber == 0 && room.game.StoryCharacter == Dreamer)
            {
                room.AddObject(new Tutorial(room));
            }
            else
            {
                orig(room);
            }
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.session is StoryGameSession sgs && !sgs.saveState.miscWorldSaveData.pebblesEnergyTaken)
            {
                sgs.saveState.miscWorldSaveData.pebblesEnergyTaken = true;

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

        private void TeleportPlayer(Player self, Vector2 pos)
        {
            self.SuperHardSetPosition(pos);
            foreach (var chunk in self.bodyChunks)
            {
                chunk.CheckHorizontalCollision();
                chunk.CheckVerticalCollision();
                chunk.checkAgainstSlopesVertically();
                chunk.lastContactPoint = chunk.contactPoint;
                chunk.vel = Vector2.zero;
            }
            self.abstractCreature.pos = self.room.GetWorldCoordinate(pos);
            self.feetStuckPos = null;

            self.LoseAllGrasps();

            if (self.slugOnBack?.HasASlug ?? false)
            {
                TeleportPlayer(self.slugOnBack.slugcat, pos);
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (CWTs.TryGetData(self, out var data))
            {
                // Toggle ability
                var input = self.controller?.GetInput() ?? RWInput.PlayerInput(self.playerState.playerNumber);
                if (input.spec && !data.astralKeyPress && (self.stun == 0 || data.astral) && self.AI == null)
                {
                    data.astralKeyPress = true;
                    data.astral = !data.astral;
                    self.PlayHUDSound(data.astral ? SoundID.SS_AI_Give_The_Mark_Boom : SoundID.Snail_Pop);
                    self.LoseAllGrasps();
                    if (!data.astral && data.projection != null && !self.dead && !self.room.GetTile(data.projection.pos).Solid)
                    {
                        TeleportPlayer(self, data.projection.pos);
                    }
                }
                else if (data.astralKeyPress && !input.spec)
                {
                    data.astralKeyPress = false;
                }
                
                if (self.dead)
                {
                    data.astral = false;
                }

                if (data.astral)
                {
                    self.stun = 180;

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
