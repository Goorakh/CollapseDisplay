using BepInEx.Bootstrap;
using CollapseDisplay.Config;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CollapseDisplay
{
    static class RiskOfOptionsCompat
    {
        public static bool Enabled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Initialize()
        {
            const string MOD_GUID = CollapseDisplayPlugin.PluginGUID;
            const string MOD_NAME = "Collapse Display";

            static void addDisplayOptions(DelayedDamageDisplayOptions displayOptions)
            {
                ModSettingsManager.AddOption(new ColorOption(displayOptions.IndicatorColor), MOD_GUID, MOD_NAME);

                ModSettingsManager.AddOption(new FloatFieldOption(displayOptions.IndicatorScale, new FloatFieldConfig
                {
                    Min = 0f
                }), MOD_GUID, MOD_NAME);

                static void addDamageBarStyle(DelayedDamageBarStyle damageBarStyle)
                {
                    ModSettingsManager.AddOption(new CheckBoxOption(damageBarStyle.EnabledConfig), MOD_GUID, MOD_NAME);
                }

                addDamageBarStyle(displayOptions.HUDHealthBarStyle);
                addDamageBarStyle(displayOptions.AllyListHealthBarStyle);
                addDamageBarStyle(displayOptions.CombatHealthBarStyle);
            }

            addDisplayOptions(CollapseDisplayPlugin.CollapseDisplayOptions);

            ModSettingsManager.SetModDescription("Options for Collapse Display", MOD_GUID, MOD_NAME);

            FileInfo iconFile = null;

            DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(CollapseDisplayPlugin.Instance.Info.Location));
            do
            {
                FileInfo[] files = dir.GetFiles("icon.png", SearchOption.TopDirectoryOnly);
                if (files != null && files.Length > 0)
                {
                    iconFile = files[0];
                    break;
                }

                dir = dir.Parent;
            } while (dir != null && !string.Equals(dir.Name, "plugins", StringComparison.OrdinalIgnoreCase));

            if (iconFile != null)
            {
                Texture2D iconTexture = new Texture2D(256, 256);
                if (iconTexture.LoadImage(File.ReadAllBytes(iconFile.FullName)))
                {
                    Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                    iconSprite.name = "CollapseDisplayIcon";

                    ModSettingsManager.SetModIcon(iconSprite, MOD_GUID, MOD_NAME);
                }
            }
        }
    }
}
