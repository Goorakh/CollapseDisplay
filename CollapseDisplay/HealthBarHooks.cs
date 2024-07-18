using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace CollapseDisplay
{
    static class HealthBarHooks
    {
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

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(out handleBarMethod) && handleBarMethod.Name.StartsWith("<ApplyBars>g__HandleBar|")))
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

            VariableDefinition collapseDisplayBarInfo = new VariableDefinition(il.Import(typeof(HealthBar.BarInfo)));
            il.Method.Body.Variables.Add(collapseDisplayBarInfo);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((HealthBar healthBar) =>
            {
                HealthBarType healthBarType = HealthBarType.Unknown;
                if (healthBar.TryGetComponent(out HealthBarTypeProvider healthBarTypeProvider))
                {
                    healthBarType = healthBarTypeProvider.Type;
                }

                HealthBarCollapseDisplayOptions displayOptions = CollapseDisplayPlugin.GetDisplayOptions(healthBarType);
                HealthBarStyle.BarStyle style = displayOptions.IndicatorStyle;

                HealthBar.BarInfo collapseBarInfo = new HealthBar.BarInfo
                {
                    enabled = false,
                    color = style.baseColor,
                    sprite = style.sprite,
                    imageType = style.imageType,
                    sizeDelta = style.sizeDelta,
                    normalizedXMin = 0f,
                    normalizedXMax = 0f
                };

                if (displayOptions.EnabledConfig.Value)
                {
                    HealthComponent healthComponent = healthBar.source;
                    if (healthComponent && healthComponent.TryGetComponent(out CollapseDamageProvider collapseDamageProvider))
                    {
                        float collapseDamage = collapseDamageProvider.EstimatedCollapseDamage;
                        if (collapseDamage > 0f)
                        {
                            collapseBarInfo.enabled = true;

                            float fullCombinedHealth = healthComponent.fullCombinedHealth;
                            float combinedHealth = healthComponent.health + healthComponent.shield;

                            HealthBar.BarInfo instantHealthBarInfo = healthBar.barInfoCollection.instantHealthbarInfo;
                            HealthBar.BarInfo curseBarInfo = healthBar.barInfoCollection.curseBarInfo;

                            float healthBarMin = instantHealthBarInfo.normalizedXMin;
                            float healthBarMax = curseBarInfo.enabled ? curseBarInfo.normalizedXMin : 1f;

                            collapseBarInfo.normalizedXMin = Util.Remap(Mathf.Clamp01((combinedHealth - collapseDamage) / fullCombinedHealth), 0f, 1f, healthBarMin, healthBarMax);

                            collapseBarInfo.normalizedXMax = Util.Remap(Mathf.Clamp01(combinedHealth / fullCombinedHealth), 0f, 1f, healthBarMin, healthBarMax);
                        }
                    }
                }

                return collapseBarInfo;
            });

            c.Emit(OpCodes.Stloc, collapseDisplayBarInfo);

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection.GetActiveCount))))
            {
                c.Emit(OpCodes.Ldloca, collapseDisplayBarInfo);
                c.EmitDelegate((int activeCount, in HealthBar.BarInfo collapseDisplayBarInfo) =>
                {
                    if (collapseDisplayBarInfo.enabled)
                    {
                        activeCount++;
                    }

                    return activeCount;
                });
            }
            else
            {
                Log.Error("Failed to find bar count patch location");
                return;
            }

            if (!c.TryGotoNext(MoveType.Before,
                               x => x.MatchRet()))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);

            c.Emit(OpCodes.Ldloca, collapseDisplayBarInfo);

            c.Emit(OpCodes.Ldloca, localsVar);

            c.Emit(OpCodes.Call, handleBarMethod);
        }
    }
}
