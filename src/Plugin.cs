using System;
using BepInEx;
using Dreamer;
using UnityEngine;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "The Dreamer", "1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "alduris.dreamer";
        public static readonly SlugcatStats.Name Dreamer = new("Dreamer", false);

        // Add hooks
        public void OnEnable()
        {
            On.Player.Update += Player_Update;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (CWTs.TryGetData(self, out var data))
            {
                //
            }
        }
    }
}
