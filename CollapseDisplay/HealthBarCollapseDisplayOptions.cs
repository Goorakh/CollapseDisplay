using BepInEx.Configuration;
using RoR2.UI;
using System;
using UnityEngine;

namespace CollapseDisplay
{
    public class HealthBarCollapseDisplayOptions
    {
        static readonly Color _defaultColor = new Color(0.9882353f, 0.14509805f, 0.25882354f, 1f);

        public readonly ConfigEntry<bool> EnabledConfig;

        public readonly ConfigEntry<Color> IndicatorColorConfig;

        public readonly ConfigEntry<float> IndicatorSizeDeltaMultiplier;

        readonly float _baseIndicatorScale;

        HealthBarStyle.BarStyle _indicatorStyle;
        public HealthBarStyle.BarStyle IndicatorStyle => _indicatorStyle;

        public HealthBarCollapseDisplayOptions(ConfigFile file, string configSectionName, HealthBarStyle.BarStyle indicatorStyle)
        {
            EnabledConfig = file.Bind(new ConfigDefinition(configSectionName, "Enabled"), true, new ConfigDescription("If this collapse indicator should be visible"));

            IndicatorColorConfig = file.Bind(new ConfigDefinition(configSectionName, "Indicator Color"), _defaultColor, new ConfigDescription("The color of the collapse indicator"));

            IndicatorSizeDeltaMultiplier = file.Bind(new ConfigDefinition(configSectionName, "Indicator Scale"), 1f, new ConfigDescription("The scale of the collapse indicator", new AcceptableValueMin<float>(0f)));

            _baseIndicatorScale = indicatorStyle.sizeDelta;

            _indicatorStyle = indicatorStyle;

            IndicatorColorConfig.SettingChanged += ColorConfig_SettingChanged;
            refreshColor();

            IndicatorSizeDeltaMultiplier.SettingChanged += ScaleMultiplier_SettingChanged;
            refreshSizeDelta();
        }

        void ColorConfig_SettingChanged(object sender, EventArgs e)
        {
            refreshColor();
        }

        void ScaleMultiplier_SettingChanged(object sender, EventArgs e)
        {
            refreshSizeDelta();
        }

        void refreshColor()
        {
            _indicatorStyle.baseColor = IndicatorColorConfig.Value;
        }

        void refreshSizeDelta()
        {
            _indicatorStyle.sizeDelta = Mathf.Max(0f, _baseIndicatorScale * IndicatorSizeDeltaMultiplier.Value);
        }
    }
}
