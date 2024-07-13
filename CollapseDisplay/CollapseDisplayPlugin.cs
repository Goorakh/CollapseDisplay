using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
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
        public const string PluginVersion = "1.0.1";

        public static ConfigEntry<Color> CombatHealthBarHighlightColor { get; private set; }

        public static ConfigEntry<Color> HUDHealthBarHighlightColor { get; private set; }

        static Sprite _healthBarHighlight;

        static readonly HealthBarStyle _combatHealthBarStyle = Addressables.LoadAssetAsync<HealthBarStyle>("RoR2/Base/Common/CombatHealthBar.asset").WaitForCompletion();
        static readonly HealthBarStyle _hudHealthBarStyle = Addressables.LoadAssetAsync<HealthBarStyle>("RoR2/Base/Common/HUDHealthBar.asset").WaitForCompletion();

        static readonly Color _defaultCollapseIndicatorColor = new Color(0.9882353f, 0.14509805f, 0.25882354f, 1f);

        static HealthBarStyle.BarStyle _collapseHUDHealthBarStyle = new HealthBarStyle.BarStyle
        {
            enabled = true,
            baseColor = _defaultCollapseIndicatorColor,
            imageType = Image.Type.Sliced,
            sizeDelta = 5f,
            sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion()
        };

        static HealthBarStyle.BarStyle _collapseCombatHealthBarStyle = new HealthBarStyle.BarStyle
        {
            enabled = true,
            baseColor = _defaultCollapseIndicatorColor,
            imageType = Image.Type.Sliced,
            sizeDelta = 1f,
            sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texUIHighlightExecute.png").WaitForCompletion()
        };

        internal static CollapseDisplayPlugin Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            HUDHealthBarHighlightColor = Config.Bind("General", "Player Indicator Color", _defaultCollapseIndicatorColor, "The collapse indicator color on player health bars");

            CombatHealthBarHighlightColor = Config.Bind("General", "Enemy Indicator Color", _defaultCollapseIndicatorColor, "The collapse indicator color on enemy health bars");

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.Initialize();
            }

            IL.RoR2.UI.HealthBar.ApplyBars += HealthBar_ApplyBars;

            if (!_healthBarHighlight)
            {
                Texture2D healthBarHighlightTexture = new Texture2D(1, 1);
                if (healthBarHighlightTexture.LoadImage(Properties.Resources.HealthBarHighlight_Opaque))
                {
                    _healthBarHighlight = Sprite.Create(healthBarHighlightTexture, new Rect(0f, 0f, healthBarHighlightTexture.width, healthBarHighlightTexture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, new Vector4(4, 3, 3, 4));
                    _healthBarHighlight.name = "HealthBarHighlightOpaque";
                }
                else
                {
                    Log.Error("Failed to load health bar overlay texture");
                }
            }

            if (_healthBarHighlight)
            {
                _collapseHUDHealthBarStyle.sprite = _healthBarHighlight;
                _collapseCombatHealthBarStyle.sprite = _healthBarHighlight;
            }

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            IL.RoR2.UI.HealthBar.ApplyBars -= HealthBar_ApplyBars;

            Instance = SingletonHelper.Unassign(Instance, this);
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
                HealthBarStyle.BarStyle style;
                Color barColor;

                if (healthBar.style == _hudHealthBarStyle)
                {
                    style = _collapseHUDHealthBarStyle;
                    barColor = HUDHealthBarHighlightColor.Value;
                }
                else //if (healthBar.style == _combatHealthBarStyle)
                {
                    style = _collapseCombatHealthBarStyle;
                    barColor = CombatHealthBarHighlightColor.Value;
                }

                HealthBar.BarInfo collapseBarInfo = new HealthBar.BarInfo
                {
                    enabled = false,
                    color = barColor,
                    sprite = style.sprite,
                    imageType = style.imageType,
                    sizeDelta = style.sizeDelta,
                    normalizedXMin = 0f,
                    normalizedXMax = 1f
                };

                HealthComponent healthComponent = healthBar.source;
                if (healthComponent)
                {
                    CharacterBody body = healthComponent.body;
                    if (body)
                    {
                        DotController dotController = DotController.FindDotController(body.gameObject);
                        if (dotController && dotController.HasDotActive(DotController.DotIndex.Fracture))
                        {
                            float collapseDamage = 0;
                            foreach (DotController.DotStack dotStack in dotController.dotStackList)
                            {
                                if (dotStack.dotIndex != DotController.DotIndex.Fracture)
                                    continue;

                                float stackDamage = dotStack.damage;

                                if (dotStack.attackerTeam == body.teamComponent.teamIndex)
                                {
                                    TeamDef attackerTeam = TeamCatalog.GetTeamDef(dotStack.attackerTeam);
                                    if (attackerTeam != null)
                                    {
                                        stackDamage *= attackerTeam.friendlyFireScaling;
                                    }
                                }

                                if (dotStack.attackerObject && dotStack.attackerObject.TryGetComponent(out CharacterBody attackerBody))
                                {
                                    if (attackerBody.inventory)
                                    {
                                        if (healthComponent.combinedHealth > healthComponent.fullCombinedHealth * 0.9f)
                                        {
                                            int crowbarItemCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.Crowbar);
                                            if (crowbarItemCount > 0)
                                            {
                                                stackDamage *= 1f + (0.75f * crowbarItemCount);
                                            }
                                        }

                                        int nearbyDamageBonusItemCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.NearbyDamageBonus);
                                        if (nearbyDamageBonusItemCount > 0)
                                        {
                                            Vector3 vectorToAttacker = attackerBody.corePosition - body.corePosition;
                                            if (vectorToAttacker.sqrMagnitude <= 13f * 13f)
                                            {
                                                stackDamage *= 1f + (nearbyDamageBonusItemCount * 0.2f);
                                            }
                                        }

                                        int fragileDamageBonusItemCount = attackerBody.inventory.GetItemCount(DLC1Content.Items.FragileDamageBonus);
                                        if (fragileDamageBonusItemCount > 0)
                                        {
                                            stackDamage *= 1f + (fragileDamageBonusItemCount * 0.2f);
                                        }

                                        if (body.isBoss)
                                        {
                                            int bossDamageBonusItemCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.BossDamageBonus);
                                            if (bossDamageBonusItemCount > 0)
                                            {
                                                stackDamage *= 1f + (0.2f * bossDamageBonusItemCount);
                                            }
                                        }
                                    }
                                }

                                if (body.HasBuff(RoR2Content.Buffs.DeathMark))
                                {
                                    stackDamage *= 1.5f;
                                }

                                float armor = body.armor + healthComponent.adaptiveArmorValue;
                                float armorDamageMultiplier = armor >= 0f ? 1f - (armor / (armor + 100f))
                                                                          : 2f - (100f / (100f - armor));

                                stackDamage = Mathf.Max(1f, stackDamage * armorDamageMultiplier);

                                if (body.inventory)
                                {
                                    int armorPlateItemCount = body.inventory.GetItemCount(RoR2Content.Items.ArmorPlate);
                                    if (armorPlateItemCount > 0)
                                    {
                                        stackDamage = Mathf.Max(1f, stackDamage - (5f * armorPlateItemCount));
                                    }
                                }

                                if (body.hasOneShotProtection)
                                {
                                    float unprotectedHealth = (healthComponent.fullCombinedHealth + healthComponent.barrier) * (1f - body.oneShotProtectionFraction);
                                    float maxAllowedDamage = Mathf.Max(0f, unprotectedHealth - healthComponent.serverDamageTakenThisUpdate);

                                    stackDamage = Mathf.Min(stackDamage, maxAllowedDamage);
                                }

                                if (body.HasBuff(RoR2Content.Buffs.LunarShell) && stackDamage > healthComponent.fullHealth * 0.1f)
                                {
                                    stackDamage = healthComponent.fullHealth * 0.1f;
                                }

                                if (body.inventory)
                                {
                                    int minHealthPercentageItemCount = body.inventory.GetItemCount(RoR2Content.Items.MinHealthPercentage);
                                    if (minHealthPercentageItemCount > 0)
                                    {
                                        float minHealth = healthComponent.fullCombinedHealth * (minHealthPercentageItemCount / 100f);
                                        stackDamage = Mathf.Max(0f, Mathf.Min(stackDamage, healthComponent.combinedHealth - minHealth));
                                    }
                                }

                                if (stackDamage > 0f)
                                {
                                    collapseDamage += stackDamage;
                                }
                            }

                            collapseDamage -= healthComponent.barrier;

                            if (collapseDamage > 0f)
                            {
                                collapseBarInfo.enabled = true;

                                float fullCombinedHealth = healthComponent.fullCombinedHealth;
                                float combinedHealth = healthComponent.health + healthComponent.shield;

                                collapseBarInfo.normalizedXMax = Mathf.Clamp01(combinedHealth / fullCombinedHealth);
                                collapseBarInfo.normalizedXMin = Mathf.Clamp01((combinedHealth - collapseDamage) / fullCombinedHealth);
                            }
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
