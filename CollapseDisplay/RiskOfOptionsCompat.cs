using BepInEx.Bootstrap;
using RiskOfOptions;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialize()
        {
            const string MOD_GUID = CollapseDisplayPlugin.PluginGUID;
            const string MOD_NAME = "Collapse Display";

            ModSettingsManager.AddOption(new ColorOption(CollapseDisplayPlugin.HUDHealthBarHighlightColor), MOD_GUID, MOD_NAME);
            
            ModSettingsManager.AddOption(new ColorOption(CollapseDisplayPlugin.CombatHealthBarHighlightColor), MOD_GUID, MOD_NAME);

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
