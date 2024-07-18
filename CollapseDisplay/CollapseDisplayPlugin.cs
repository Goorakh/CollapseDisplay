using BepInEx;
using HG;
using RoR2.UI;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace CollapseDisplay
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class CollapseDisplayPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "CollapseDisplay";
        public const string PluginVersion = "1.2.0";

        internal static CollapseDisplayPlugin Instance { get; private set; }

        static Sprite _healthBarHighlight;

        static HealthBarCollapseDisplayOptions _hudHealthBarOptions;
        static HealthBarCollapseDisplayOptions _allyListHealthBarOptions;
        static HealthBarCollapseDisplayOptions _combatHealthBarOptions;
        static HealthBarCollapseDisplayOptions _fallbackHealthBarOptions;

        internal static ReadOnlyArray<HealthBarCollapseDisplayOptions> AllHealthBarDisplayOptions { get; private set; }

        internal static HealthBarCollapseDisplayOptions GetDisplayOptions(HealthBarType barType)
        {
            return barType switch
            {
                HealthBarType.Hud => _hudHealthBarOptions,
                HealthBarType.Combat => _combatHealthBarOptions,
                HealthBarType.AllyList => _allyListHealthBarOptions,
                HealthBarType.Unknown => _fallbackHealthBarOptions,
                _ => throw new NotImplementedException($"{barType} is not implemented"),
            };
        }

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

            _hudHealthBarOptions = new HealthBarCollapseDisplayOptions(Config, "Player HUD", new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 5f,
                sprite = _healthBarHighlight ?? Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion()
            });

            _allyListHealthBarOptions = new HealthBarCollapseDisplayOptions(Config, "Ally List", new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 1f,
                sprite = _healthBarHighlight ?? Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion()
            });

            _combatHealthBarOptions = new HealthBarCollapseDisplayOptions(Config, "Enemy", new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 1f,
                sprite = _healthBarHighlight ?? Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion()
            });

            _fallbackHealthBarOptions = _combatHealthBarOptions;

            AllHealthBarDisplayOptions = new ReadOnlyArray<HealthBarCollapseDisplayOptions>([_hudHealthBarOptions, _allyListHealthBarOptions, _combatHealthBarOptions]);

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.Initialize();
            }

            HealthBarTypeRegistration.Initialize();
            HealthBarHooks.Initialize();
            CollapseDamageProviderHooks.Initialize();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            HealthBarTypeRegistration.Cleanup();
            HealthBarHooks.Cleanup();
            CollapseDamageProviderHooks.Cleanup();

            Instance = SingletonHelper.Unassign(Instance, this);
        }
    }
}
