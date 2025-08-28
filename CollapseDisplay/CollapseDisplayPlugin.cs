using BepInEx;
using CollapseDisplay.Config;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CollapseDisplay
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class CollapseDisplayPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "CollapseDisplay";
        public const string PluginVersion = "1.3.6";

        internal static CollapseDisplayPlugin Instance { get; private set; }

        public static DelayedDamageDisplayOptions CollapseDisplayOptions { get; private set; }

        public static DelayedDamageDisplayOptions EssenceOfHeresyDisplayOptions { get; private set; }

        static Sprite _healthBarHighlight;

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            Texture2D healthBarHighlightTexture = new Texture2D(1, 1);
            if (healthBarHighlightTexture.LoadImage(Properties.Resources.HealthBarHighlight_Opaque))
            {
                _healthBarHighlight = Sprite.Create(healthBarHighlightTexture, new Rect(0f, 0f, healthBarHighlightTexture.width, healthBarHighlightTexture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, new Vector4(4, 3, 3, 4));
                _healthBarHighlight.name = "HealthBarHighlightOpaque";
            }
            else
            {
                Log.Error("Failed to load health bar overlay texture");
                _healthBarHighlight = null;
            }

            Sprite healthBarHighlight = _healthBarHighlight ? _healthBarHighlight : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion();

            CollapseDisplayOptions = new DelayedDamageDisplayOptions(healthBarHighlight,
                                                                     Config,
                                                                     "Collapse",
                                                                     new Color(0.9882353f, 0.14509805f, 0.25882354f, 1f),
                                                                     1f);

            EssenceOfHeresyDisplayOptions = new DelayedDamageDisplayOptions(healthBarHighlight,
                                                                            Config,
                                                                            "Heretic Ruin",
                                                                            new Color32(0x29, 0x8A, 0xF2, 0xFF),
                                                                            1f);

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.Initialize();
            }

            HealthBarTypeRegistration.Initialize();
            HealthBarHooks.Initialize();
            DelayedDamageProviderHooks.Initialize();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
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
