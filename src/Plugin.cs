using System;
using BepInEx;
using UnityEngine;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "The Dreamer", "1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "alduris.dreamer";

        // Add hooks
        public void OnEnable()
        {
        }
    }
}
