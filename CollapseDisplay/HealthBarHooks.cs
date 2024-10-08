﻿using CollapseDisplay.Config;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CollapseDisplay
{
    static class HealthBarHooks
    {
        readonly struct AdditionalBarInfos
        {
            public static readonly FieldInfo[] BarInfoFields = typeof(AdditionalBarInfos).GetFields(BindingFlags.Public | BindingFlags.Instance)
                                                                                         .Where(f => f.FieldType == typeof(HealthBar.BarInfo))
                                                                                         .ToArray();

            public readonly HealthBar.BarInfo CollapseDamageBarInfo;
            public readonly HealthBar.BarInfo WarpedEchoDamageBarInfo;

            public readonly int EnabledBarCount;

            public AdditionalBarInfos(HealthBar.BarInfo collapseDamageBarInfo, HealthBar.BarInfo warpedEchoDamageBarInfo)
            {
                int enabledBarCount = 0;
                CollapseDamageBarInfo = collapseDamageBarInfo;
                if (CollapseDamageBarInfo.enabled)
                    enabledBarCount++;

                WarpedEchoDamageBarInfo = warpedEchoDamageBarInfo;
                if (WarpedEchoDamageBarInfo.enabled)
                    enabledBarCount++;

                EnabledBarCount = enabledBarCount;
            }
        }

        public static void Initialize()
        {
            IL.RoR2.UI.HealthBar.ApplyBars += HealthBar_ApplyBars;
        }

        public static void Cleanup()
        {
            IL.RoR2.UI.HealthBar.ApplyBars -= HealthBar_ApplyBars;
        }

        static void HealthBar_ApplyBars(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            MethodReference handleBarMethod = null;
            VariableDefinition localsVar;

            static bool nameStartsWith(MethodReference methodReference, string value)
            {
                // This is dumb
                if (methodReference == null || methodReference.Name == null)
                    return false;

                return methodReference.Name.StartsWith(value);
            }

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(out handleBarMethod) && nameStartsWith(handleBarMethod, "<ApplyBars>g__HandleBar|")))
            {
                int localsVarIndex = -1;
                if (c.TryGotoPrev(x => x.MatchLdloca(out localsVarIndex)))
                {
                    localsVar = il.Method.Body.Variables[localsVarIndex];
                }
                else
                {
                    Log.Error("Failed to find locals variable");
                    return;
                }
            }
            else
            {
                Log.Error("Failed to find HandleBar method");
                return;
            }

            c.Index = 0;

            VariableDefinition damageDisplayBarInfos = new VariableDefinition(il.Import(typeof(AdditionalBarInfos)));
            il.Method.Body.Variables.Add(damageDisplayBarInfos);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(collectBarInfos);

            c.Emit(OpCodes.Stloc, damageDisplayBarInfos);

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection.GetActiveCount))))
            {
                c.Emit(OpCodes.Ldloca, damageDisplayBarInfos);
                c.EmitDelegate(addDamageDisplayBarsToCount);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static int addDamageDisplayBarsToCount(int activeCount, in AdditionalBarInfos damageDisplayBarInfos)
                {
                    return activeCount + damageDisplayBarInfos.EnabledBarCount;
                }
            }
            else
            {
                Log.Error("Failed to find bar count patch location");
                return;
            }

            if (!c.TryGotoNext(MoveType.AfterLabel,
                               x => x.MatchRet()))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            foreach (FieldInfo barInfoField in AdditionalBarInfos.BarInfoFields)
            {
                c.Emit(OpCodes.Ldarg_0);

                c.Emit(OpCodes.Ldloca, damageDisplayBarInfos);
                c.Emit(OpCodes.Ldflda, barInfoField);

                c.Emit(OpCodes.Ldloca, localsVar);

                c.Emit(OpCodes.Call, handleBarMethod);
            }
        }

        static AdditionalBarInfos collectBarInfos(HealthBar healthBar)
        {
            HealthBarType healthBarType = HealthBarType.Unknown;
            if (healthBar.TryGetComponent(out HealthBarTypeProvider healthBarTypeProvider))
            {
                healthBarType = healthBarTypeProvider.Type;
            }

            DelayedDamageDisplayOptions collapseDisplayOptions = CollapseDisplayPlugin.CollapseDisplayOptions;
            DelayedDamageBarStyle collapseDamageBarStyle = collapseDisplayOptions.GetDamageBarStyle(healthBarType);

            HealthBarStyle.BarStyle collapseIndicatorStyle = collapseDamageBarStyle.BarStyle;
            HealthBar.BarInfo collapseBarInfo = new HealthBar.BarInfo
            {
                enabled = false,
                color = collapseIndicatorStyle.baseColor,
                sprite = collapseIndicatorStyle.sprite,
                imageType = collapseIndicatorStyle.imageType,
                sizeDelta = collapseIndicatorStyle.sizeDelta,
                normalizedXMin = 0f,
                normalizedXMax = 0
            };

            DelayedDamageDisplayOptions warpedEchoDisplayOptions = CollapseDisplayPlugin.WarpedEchoDisplayOptions;
            DelayedDamageBarStyle warpedEchoDamageBarStyle = warpedEchoDisplayOptions.GetDamageBarStyle(healthBarType);

            HealthBarStyle.BarStyle warpedEchoIndicatorStyle = warpedEchoDamageBarStyle.BarStyle;
            HealthBar.BarInfo warpedEchoBarInfo = new HealthBar.BarInfo
            {
                enabled = false,
                color = warpedEchoIndicatorStyle.baseColor,
                sprite = warpedEchoIndicatorStyle.sprite,
                imageType = warpedEchoIndicatorStyle.imageType,
                sizeDelta = warpedEchoIndicatorStyle.sizeDelta,
                normalizedXMin = 0f,
                normalizedXMax = 0f
            };

            HealthComponent healthComponent = healthBar.source;
            if (healthComponent && healthComponent.TryGetComponent(out DelayedDamageProvider delayedDamageProvider))
            {
                HealthComponent.HealthBarValues healthBarValues = healthComponent.GetHealthBarValues();

                float currentHealth = healthComponent.combinedHealth;
                float fullCombinedHealth = healthComponent.fullCombinedHealth;

                float nonCurseFraction = 1f - healthBarValues.curseFraction;

                float totalBarDamageAmount = 0f;

                void addBar(ref HealthBar.BarInfo barInfo, float damageAmount)
                {
                    barInfo.enabled = true;

                    float xMin = (currentHealth - totalBarDamageAmount - damageAmount) / fullCombinedHealth * nonCurseFraction;
                    barInfo.normalizedXMin = Mathf.Clamp01(xMin);

                    float xMax = (currentHealth - totalBarDamageAmount) / fullCombinedHealth * nonCurseFraction;
                    barInfo.normalizedXMax = Mathf.Clamp01(xMax);

                    totalBarDamageAmount += damageAmount;
                }

                if (warpedEchoDamageBarStyle.EnabledConfig.Value)
                {
                    DelayedDamageInfo warpedEchoDamageInfo = delayedDamageProvider.WarpedEchoDamage;
                    if (warpedEchoDamageInfo.DamageTimestamp.timeUntil > 0f)
                    {
                        float warpedEchoDisplayedDamage = warpedEchoDamageInfo.Damage - healthComponent.barrier;
                        if (warpedEchoDisplayedDamage > 0f)
                        {
                            addBar(ref warpedEchoBarInfo, warpedEchoDisplayedDamage);
                        }
                    }
                }

                if (collapseDamageBarStyle.EnabledConfig.Value)
                {
                    DelayedDamageInfo collapseDamageInfo = delayedDamageProvider.CollapseDamage;
                    if (collapseDamageInfo.DamageTimestamp.timeUntil > 0f)
                    {
                        float collapseDisplayedDamage = collapseDamageInfo.Damage - healthComponent.barrier;
                        if (collapseDisplayedDamage > 0f)
                        {
                            addBar(ref collapseBarInfo, collapseDisplayedDamage);
                        }
                    }
                }
            }

            return new AdditionalBarInfos(collapseBarInfo, warpedEchoBarInfo);
        }
    }
}
