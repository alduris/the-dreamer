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

        private Vector2 vel = Vector2.zero;
        private int cloudCount;
        private int starCount;
        private Quaternion[] cloudSin;
        private float[] cloudAlphas;
        private float[] cloudRots;
        private Vector2[] starOffsets;
        private Vector3[] starSin;

        private int lastLife = 0;
        private int life = 0;

        private Color lightColor;
        private Color darkColor;

        public Projection(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            room = player.room;
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
                starOffsets[i] = Custom.RNV();
                starSin[i] = new Vector3(Random.value, Random.value, Random.value);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (playerRef.TryGetTarget(out var player) && CWTs.TryGetData(player, out var data) && data.astral)
            {
                lastPos = pos;
                pos += vel;

                foreach (var list in room.physicalObjects)
                {
                    foreach (var obj in list)
                    {
                        if (obj == player) continue; // don't affect ourselves
                        // if (obj is Player && !Custom.rainWorld.options.friendlyFire) continue; // don't affect other jolly players without friendly fire

                        foreach (var chunk in obj.bodyChunks)
                        {
                            var mag = Mathf.Min(1f, 1f / Vector2.Distance(pos, chunk.pos));
                            if (mag > 0.01f)
                            {
                                chunk.vel += (pos - chunk.pos) * mag / chunk.mass;
                                chunk.pos += (pos - chunk.pos) * mag / chunk.mass;
                            }
                        }
                    }
                }

                if (Random.value < 1f/60f)
                {
                    room.AddObject(new TinyGlyph(pos, lightColor, Random.Range(5, 10)));
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
            if (input.AnyDirectionalInput)
            {
                var dir = input.analogueDir;
                var dist = Vector2.Distance(dir, vel);
                vel = Vector2.Lerp(dir, vel, dist < 0.01f ? 0f : Mathf.Max(0.8f, dist));
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
                sLeaser.sprites[i] = new FSprite("Circle20", true);
            }
            for (int i = cloudCount; i < cloudCount + starCount; i++)
            {
                sLeaser.sprites[i] = new FSprite("tinyStar", true);
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
                sLeaser.sprites[i].color = (i < cloudCount) ? Color.Lerp(darkColor, lightColor, Mathf.Pow(Random.value * 0.2f, 1.5f)) : lightColor;
                sLeaser.sprites[i].alpha = cloudAlphas[i];
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            var posFac = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;
            var lifeFac = Mathf.Lerp(lastLife, life, timeStacker);

            for (int i = 0; i < cloudCount; i++)
            {
                float t = lifeFac * cloudSin[i].z + cloudSin[i].w;
                var v = new Vector2(Mathf.Sin(t + cloudSin[i].x), Mathf.Cos(t + cloudSin[i].y));
                sLeaser.sprites[i].SetPosition(posFac + Custom.rotateVectorDeg(v, cloudRots[i]) * 12f);
            }

            for (int i = 0; i < starCount; i++)
            {
                var t = Mathf.Sin(lifeFac * 0.4f * starSin[i].x + starSin[i].y) * starSin[i].z;
                var s = Custom.rotateVectorDeg(Vector2.up * t, starOffsets[i].GetAngle()) * 5f;
                var v = starOffsets[i] + s;
                sLeaser.sprites[i + cloudCount].SetPosition(posFac + v);
            }

            sLeaser.sprites[sLeaser.sprites.Length - 1].SetPosition(posFac + vel * 5f);
        }

        public class TinyGlyph : CosmeticSprite
        {
            private Color color;
            private int maxLife;
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
                sLeaser.sprites = [new FSprite("halyGlyph" + Random.Range(0, 7), true)];
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
