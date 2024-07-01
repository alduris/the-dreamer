using System;
using System.Collections.Generic;
using RWCustom;
using SlugBase.DataTypes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dreamer
{
    internal class Projection : CosmeticSprite
    {
        private WeakReference<Player> playerRef;

        private readonly int cloudCount;
        private readonly int starCount;
        private readonly Quaternion[] cloudSin;
        private readonly float[] cloudAlphas;
        private readonly float[] cloudRots;
        private readonly Vector2[] starOffsets;
        private readonly Vector3[] starSin;

        private int lastLife = 0;
        private int life = 0;

        private Color lightColor;
        private Color darkColor;

        public Projection(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            room = player.room;
            pos = player.mainBodyChunk.pos;
            lastPos = pos;
            vel = Custom.RNV().normalized * 1.5f;

            lightColor = PlayerColor.GetCustomColor(player.graphicsModule as PlayerGraphics, "Body");
            darkColor = PlayerColor.GetCustomColor(player.graphicsModule as PlayerGraphics, "Eyes");

            cloudCount = Random.Range(10, 20);
            starCount = Random.Range(6, 14);

            cloudSin = new Quaternion[cloudCount];
            cloudAlphas = new float[cloudCount];
            cloudRots = new float[cloudCount];
            for (int i = 0; i < cloudCount; i++)
            {
                cloudSin[i] = Random.rotation;
                cloudAlphas[i] = Random.Range(0.6f, 0.85f);
                cloudRots[i] = Random.Range(0f, 360f);
            }

            starOffsets = new Vector2[starCount];
            starSin = new Vector3[starCount];
            for (int i = 0; i < starCount; i++)
            {
                starOffsets[i] = Random.insideUnitCircle;
                starSin[i] = new Vector3(Random.value, Random.value, Random.value);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (playerRef.TryGetTarget(out var player) && CWTs.TryGetData(player, out var data) && data.astral)
            {
                var maxstr = Plugin.PushStrength.TryGet(player, out var f1) ? f1 : 0.5f;
                var strmult = Plugin.PushMult.TryGet(player, out var f2) ? f2 : 2f;
                lastPos = pos;
                pos += vel;

                foreach (var list in room.physicalObjects)
                {
                    foreach (var obj in list)
                    {
                        if (obj == player && (!Plugin.AffectSelf.TryGet(player, out var b1) || !b1)) continue; // don't affect ourselves
                        // if (obj is Player && !Custom.rainWorld.options.friendlyFire) continue; // don't affect other jolly players without friendly fire

                        foreach (var chunk in obj.bodyChunks)
                        {
                            var mag = Mathf.Min(maxstr, 1f / Vector2.Distance(pos, chunk.pos)) * strmult;
                            if (Mathf.Abs(mag) > 0.001f)
                            {
                                chunk.vel -= (pos - chunk.pos).normalized * mag / Mathf.Sqrt(chunk.mass);
                                chunk.pos += (pos - chunk.pos).normalized * mag / Mathf.Sqrt(chunk.mass);
                            }
                        }
                    }
                }

                if (Random.value < 0.25f)
                {
                    room.AddObject(new TinyGlyph(pos + Random.insideUnitCircle * 14f, lightColor, Random.Range(5, 10)));
                }

                lastLife = life;
                life++;
            }
            else
            {
                Destroy();
            }
        }

        public void MovementUpdate(Player.InputPackage input)
        {
            var dir = input.IntVec.ToVector2() * 2f;
            if (dir.magnitude > 0.05f)
            {
                var dist = Vector2.Distance(dir, vel);
                vel = Vector2.Lerp(dir, vel, dist < 0.01f ? 0f : Mathf.Min(0.4f, dist));
            }
            else
            {
                vel *= 0.8f;
                if (vel.magnitude < 0.01f)
                {
                    vel = Vector2.zero;
                }
            }
        }

        public override void Destroy()
        {
            room?.AddObject(new ShockWave(pos, 160f, 0.07f, 9, false));
            base.Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[cloudCount + starCount + 1];

            for (int i = 0; i < cloudCount; i++)
            {
                sLeaser.sprites[i] = new FSprite("Circle20", true) { shader = rCam.game.rainWorld.Shaders["FlatLight"] };
            }
            for (int i = cloudCount; i < cloudCount + starCount; i++)
            {
                sLeaser.sprites[i] = new FSprite("tinyStar", true) { alpha = Random.Range(0.5f, 0.8f) };
            }
            sLeaser.sprites[sLeaser.sprites.Length - 1] = new FSprite("FaceA0", true);

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Water");

            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = (i < cloudCount) ? CreateCloudColor() : lightColor;
                if (i < cloudCount)
                {
                    sLeaser.sprites[i].alpha = cloudAlphas[i];
                }
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            var posFac = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;
            var lifeFac = Mathf.Lerp(lastLife, life, timeStacker);

            for (int i = 0; i < cloudCount; i++)
            {
                float t = lifeFac * 0.3f * cloudSin[i].z + cloudSin[i].w;
                var v = new Vector2(Mathf.Cos(t + cloudSin[i].x), Mathf.Cos(t + cloudSin[i].y));
                sLeaser.sprites[i].SetPosition(posFac + Custom.rotateVectorDeg(v, cloudRots[i]) * 8f);
            }

            for (int i = 0; i < starCount; i++)
            {
                var t = Mathf.Sin(lifeFac * 0.04f * starSin[i].x + starSin[i].y) * starSin[i].z;
                var s = Custom.rotateVectorDeg(Vector2.up * t, starOffsets[i].GetAngle() + 90f) * 3f;
                var v = starOffsets[i] * 14f + s;
                sLeaser.sprites[i + cloudCount].SetPosition(posFac + v);
            }

            sLeaser.sprites[sLeaser.sprites.Length - 1].SetPosition(posFac + vel * 2.5f);
        }

        private Color CreateCloudColor()
        {
            Color otherColor;
            if (Random.value < 0.5f)
            {
                otherColor = lightColor;
            }
            else
            {
                otherColor = Color.Lerp(darkColor, Color.black, 0.4f);
            }
            return Color.Lerp(darkColor, otherColor, Mathf.Pow(Random.value * 0.2f, 1.5f));
        }

        public class TinyGlyph : CosmeticSprite
        {
            private Color color;
            private readonly int maxLife;
            private int life;
            private int lastLife;

            public TinyGlyph(Vector2 pos, Color color, int life) : this(pos, Vector2.zero, color, life) { }

            public TinyGlyph(Vector2 pos, Vector2 vel, Color color, int life)
            {
                this.pos = pos;
                this.vel = vel;
                this.life = life;
                this.lastLife = life;
                this.maxLife = life;
                this.color = color;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (life == 0)
                {
                    Destroy();
                }
                else
                {
                    lastLife = life;
                    life--;

                    lastPos = pos;
                    pos += vel;
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = [new FSprite("haloGlyph" + Random.Range(0, 7), true)];
                AddToContainer(sLeaser, rCam, null);
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                sLeaser.sprites[0].RemoveFromContainer();
                (newContatiner ?? rCam.ReturnFContainer("Water")).AddChild(sLeaser.sprites[0]);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                sLeaser.sprites[0].color = new Color(color.r, color.g, color.b, Mathf.Lerp(lastLife, life, timeStacker) / maxLife);
            }
        }
    }
}
