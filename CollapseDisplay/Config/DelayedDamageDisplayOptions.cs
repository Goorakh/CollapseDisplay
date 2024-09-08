using BepInEx.Configuration;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CollapseDisplay.Config
{
    public class DelayedDamageDisplayOptions
    {
        public readonly ConfigEntry<Color> IndicatorColor;

        public readonly ConfigEntry<float> IndicatorScale;

        public readonly DelayedDamageBarStyle HUDHealthBarStyle;

        public readonly DelayedDamageBarStyle AllyListHealthBarStyle;

        public readonly DelayedDamageBarStyle CombatHealthBarStyle;

        public DelayedDamageBarStyle FallbackHealthBarStyle => CombatHealthBarStyle ?? AllyListHealthBarStyle ?? HUDHealthBarStyle;

        public DelayedDamageDisplayOptions(Sprite highlightSprite, ConfigFile file, string sectionName, Color defaultColor, float defaultIndicatorScale)
        {
            IndicatorColor = file.Bind(sectionName, "Indicator Color", defaultColor, new ConfigDescription("The color of this damage indicator"));
            IndicatorScale = file.Bind(sectionName, "Indicator Scale", defaultIndicatorScale, new ConfigDescription("The scale of this damage indicator"));

            HUDHealthBarStyle = new DelayedDamageBarStyle(file, sectionName, HealthBarType.Hud, new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 5f,
                sprite = highlightSprite
            }, this);

            AllyListHealthBarStyle = new DelayedDamageBarStyle(file, sectionName, HealthBarType.AllyList, new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 1f,
                sprite = highlightSprite
            }, this);

            CombatHealthBarStyle = new DelayedDamageBarStyle(file, sectionName, HealthBarType.Combat, new HealthBarStyle.BarStyle
            {
                enabled = true,
                imageType = Image.Type.Sliced,
                sizeDelta = 1f,
                sprite = highlightSprite
            }, this);
        }

        public DelayedDamageBarStyle GetDamageBarStyle(HealthBarType barType)
        {
            return barType switch
            {
                HealthBarType.Hud => HUDHealthBarStyle,
                HealthBarType.Combat => CombatHealthBarStyle,
                HealthBarType.AllyList => AllyListHealthBarStyle,
                HealthBarType.Unknown => FallbackHealthBarStyle,
                _ => throw new NotImplementedException($"{barType} is not implemented"),
            };
        }
    }
}
