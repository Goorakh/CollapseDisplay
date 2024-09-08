using BepInEx.Configuration;
using RoR2.UI;
using System;
using UnityEngine;

namespace CollapseDisplay.Config
{
    public class DelayedDamageBarStyle
    {
        public readonly ConfigEntry<bool> EnabledConfig;

        public readonly HealthBarType BarType;

        readonly DelayedDamageDisplayOptions _displayOptions;

        readonly float _baseSizeDelta;

        HealthBarStyle.BarStyle _barStyle;
        public HealthBarStyle.BarStyle BarStyle => _barStyle;

        public DelayedDamageBarStyle(ConfigFile file, string sectionName, HealthBarType type, HealthBarStyle.BarStyle baseStyle, DelayedDamageDisplayOptions displayOptions)
        {
            BarType = type;

            string identifier = type switch
            {
                HealthBarType.Hud => "Player HUD",
                HealthBarType.AllyList => "Ally List",
                HealthBarType.Combat => "Enemy",
                _ => throw new NotImplementedException($"Bar type {type} is not implemented")
            };

            EnabledConfig = file.Bind(sectionName, $"Show on {identifier}", true, new ConfigDescription($"If this damage indicator should display on {identifier} healthbars"));

            _barStyle = baseStyle;
            _baseSizeDelta = _barStyle.sizeDelta;

            _displayOptions = displayOptions;

            _displayOptions.IndicatorColor.SettingChanged += IndicatorColor_SettingChanged;
            updateBarColor();

            _displayOptions.IndicatorScale.SettingChanged += IndicatorScale_SettingChanged;
            updateBarSizeDelta();
        }

        void IndicatorColor_SettingChanged(object sender, System.EventArgs e)
        {
            updateBarColor();
        }

        void updateBarColor()
        {
            _barStyle.baseColor = _displayOptions.IndicatorColor.Value;
        }

        void IndicatorScale_SettingChanged(object sender, System.EventArgs e)
        {
            updateBarSizeDelta();
        }

        void updateBarSizeDelta()
        {
            _barStyle.sizeDelta = Mathf.Max(0f, _baseSizeDelta * _displayOptions.IndicatorScale.Value);
        }
    }
}
