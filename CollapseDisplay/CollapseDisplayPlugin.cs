using BepInEx;
using CollapseDisplay.Config;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CollapseDisplay
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class CollapseDisplayPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "CollapseDisplay";
        public const string PluginVersion = "1.5.2";

        internal static CollapseDisplayPlugin Instance { get; private set; }

        public static DelayedDamageDisplayOptions CollapseDisplayOptions { get; private set; }

        public static DelayedDamageDisplayOptions EssenceOfHeresyDisplayOptions { get; private set; }

        public static DelayedDamageDisplayOptions NetworkedSufferingDisplayOptions { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            Sprite healthBarHighlightSprite;

            Texture2D healthBarHighlightTexture = new Texture2D(1, 1);
            if (healthBarHighlightTexture.LoadImage(Properties.Resources.HealthBarHighlight_Opaque))
            {
                healthBarHighlightTexture.name = "texHealthBarHighlightOpaque";

                healthBarHighlightSprite = Sprite.Create(healthBarHighlightTexture, new Rect(0f, 0f, healthBarHighlightTexture.width, healthBarHighlightTexture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, new Vector4(4, 3, 3, 4));
                healthBarHighlightSprite.name = healthBarHighlightTexture.name;
            }
            else
            {
                Log.Error("Failed to load health bar overlay texture");
                Destroy(healthBarHighlightTexture);
                healthBarHighlightSprite = Addressables.LoadAssetAsync<Sprite>(RoR2_Base_Common.texUIHighlightExecute_png_texUIHighlightExecute_).WaitForCompletion();
            }

            CollapseDisplayOptions = new DelayedDamageDisplayOptions(healthBarHighlightSprite,
                                                                     Config,
                                                                     "Collapse",
                                                                     new Color(0.9882353f, 0.14509805f, 0.25882354f, 1f),
                                                                     1f);

            EssenceOfHeresyDisplayOptions = new DelayedDamageDisplayOptions(healthBarHighlightSprite,
                                                                            Config,
                                                                            "Heretic Ruin",
                                                                            new Color32(0x29, 0x8A, 0xF2, byte.MaxValue),
                                                                            1f);

            NetworkedSufferingDisplayOptions = new DelayedDamageDisplayOptions(healthBarHighlightSprite,
                                                                               Config,
                                                                               "Networked Suffering",
                                                                               new Color32(0x46, 0xFC, 0x83, byte.MaxValue),
                                                                               1f);

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.Initialize();
            }

            HealthBarTypeRegistration.Initialize();
            HealthBarHooks.Initialize();
            DelayedDamageProviderHooks.Initialize();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        }

        void OnDestroy()
        {
            HealthBarTypeRegistration.Cleanup();
            HealthBarHooks.Cleanup();
            DelayedDamageProviderHooks.Cleanup();

            Instance = SingletonHelper.Unassign(Instance, this);
        }
    }
}
