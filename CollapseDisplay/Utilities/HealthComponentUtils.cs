using RoR2;
using System;
using UnityEngine;

namespace CollapseDisplay.Utilities
{
    public static class HealthComponentUtils
    {
        public static float EstimateTakeDamage(HealthComponent victim, DamageInfo damageInfo)
        {
            if (damageInfo is null)
                throw new ArgumentNullException(nameof(damageInfo));

            if (!victim || !victim.body)
                return damageInfo.damage;

            CharacterMaster attackerMaster = null;
            CharacterBody attackerBody = null;
            TeamIndex attackerTeamIndex = TeamIndex.None;
            Vector3 hitPositionToAttackerCorePosition = Vector3.zero;
            float combinedHealthBeforeDamage = victim.combinedHealth;
            if (damageInfo.attacker)
            {
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    attackerTeamIndex = attackerBody.teamComponent.teamIndex;
                    hitPositionToAttackerCorePosition = attackerBody.corePosition - damageInfo.position;
                }
            }

            bool bypassArmor = (damageInfo.damageType & DamageType.BypassArmor) != 0;
            bool bypassBlock = (damageInfo.damageType & DamageType.BypassBlock) != 0;

            if ((damageInfo.damageType & DamageTypeExtended.DamagePercentOfMaxHealth) == DamageTypeExtended.DamagePercentOfMaxHealth)
            {
                damageInfo.damage = victim.fullHealth * 0.1f;
            }

            KnockbackFinUtil.ModifyDamageInfo(ref damageInfo, attackerBody, victim.body);

            if (victim.body.HasBuff(DLC2Content.Buffs.lunarruin))
            {
                float lunarRuinDamageCoefficient = victim.body.GetBuffCount(DLC2Content.Buffs.lunarruin) * 0.1f;
                float lunarRuinDamage = damageInfo.damage * lunarRuinDamageCoefficient;
                damageInfo.damage += lunarRuinDamage;
            }

            if (attackerTeamIndex == victim.body.teamComponent.teamIndex)
            {
                TeamDef attackerTeamDef = TeamCatalog.GetTeamDef(attackerTeamIndex);
                if (attackerTeamDef != null)
                {
                    damageInfo.damage *= attackerTeamDef.friendlyFireScaling;
                }
            }

            if (damageInfo.damage > 0f)
            {
                if (attackerBody)
                {
                    if (attackerBody.canPerformBackstab && (damageInfo.damageType & DamageType.DoT) != DamageType.DoT && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-hitPositionToAttackerCorePosition, victim.body)))
                    {
                        damageInfo.crit = true;
                        damageInfo.procChainMask.AddProc(ProcType.Backstab);
                    }

                    attackerMaster = attackerBody.master;
                    if (attackerMaster && attackerMaster.inventory)
                    {
                        if (combinedHealthBeforeDamage >= victim.fullCombinedHealth * 0.9f)
                        {
                            int crowbarCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.Crowbar);
                            if (crowbarCount > 0)
                            {
                                damageInfo.damage *= 1f + (0.75f * crowbarCount);
                            }
                        }

                        int nearbyDamageBonusCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.NearbyDamageBonus);
                        if (nearbyDamageBonusCount > 0 && hitPositionToAttackerCorePosition.sqrMagnitude <= 13f * 13f)
                        {
                            damageInfo.damageColorIndex = DamageColorIndex.Nearby;
                            damageInfo.damage *= 1f + (nearbyDamageBonusCount * 0.2f);
                        }

                        int fragileDamageBonusCount = attackerMaster.inventory.GetItemCount(DLC1Content.Items.FragileDamageBonus);
                        if (fragileDamageBonusCount > 0)
                        {
                            damageInfo.damage *= 1f + (fragileDamageBonusCount * 0.2f);
                        }

                        if (damageInfo.procCoefficient > 0f)
                        {
                            if (victim.body.HasBuff(RoR2Content.Buffs.MercExpose) && attackerBody && attackerBody.bodyIndex == BodyCatalog.FindBodyIndex("MercBody"))
                            {
                                float exposeDamage = attackerBody.damage * 3.5f;
                                damageInfo.damage += exposeDamage;
                            }
                        }

                        if (victim.body.isBoss)
                        {
                            int bossDamageBonusCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.BossDamageBonus);
                            if (bossDamageBonusCount > 0)
                            {
                                damageInfo.damage *= 1f + (0.2f * bossDamageBonusCount);
                                damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
                            }
                        }
                    }

                    if (damageInfo.crit)
                    {
                        damageInfo.damage *= attackerBody.critMultiplier;
                    }
                }

                if ((damageInfo.damageType & DamageType.WeakPointHit) != 0)
                {
                    damageInfo.damage *= 1.5f;
                    damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
                }

                if (victim.body.HasBuff(RoR2Content.Buffs.DeathMark))
                {
                    damageInfo.damage *= 1.5f;
                    damageInfo.damageColorIndex = DamageColorIndex.DeathMark;
                }

                if (!bypassArmor)
                {
                    if (!damageInfo.delayedDamageSecondHalf)
                    {
                        float armor = victim.body.armor;
                        armor += victim.adaptiveArmorValue;

                        bool isAOE = (damageInfo.damageType & DamageType.AOE) != 0;
                        if ((victim.body.bodyFlags & CharacterBody.BodyFlags.ResistantToAOE) != 0 && isAOE)
                        {
                            armor += 300f;
                        }

                        float armorDamageMultiplier = armor >= 0f ? (1f - (armor / (armor + 100f)))
                                                                  : (2f - (100f / (100f - armor)));

                        damageInfo.damage = Mathf.Max(1f, damageInfo.damage * armorDamageMultiplier);
                    }

                    if (victim.itemCounts.armorPlate > 0)
                    {
                        damageInfo.damage = Mathf.Max(1f, damageInfo.damage - (5f * victim.itemCounts.armorPlate));
                    }
                }

                if (!bypassBlock && damageInfo.damage > 0f && !damageInfo.delayedDamageSecondHalf && !damageInfo.rejected && !damageInfo.firstHitOfDelayedDamageSecondHalf && victim.body.HasBuff(DLC2Content.Buffs.DelayedDamageBuff))
                {
                    damageInfo.damage *= 0.8f;
                }

                if (victim.body.hasOneShotProtection && (damageInfo.damageType & DamageType.BypassOneShotProtection) != DamageType.BypassOneShotProtection)
                {
                    float num7 = (victim.fullCombinedHealth + victim.barrier) * (1f - victim.body.oneShotProtectionFraction);
                    float b = Mathf.Max(0f, num7 - victim.serverDamageTakenThisUpdate);
                    damageInfo.damage = Mathf.Min(damageInfo.damage, b);
                }

                if ((damageInfo.damageType & DamageType.BonusToLowHealth) != 0)
                {
                    float num9 = Mathf.Lerp(3f, 1f, victim.combinedHealthFraction);
                    damageInfo.damage *= num9;
                }

                if (victim.body.HasBuff(RoR2Content.Buffs.LunarShell) && damageInfo.damage > victim.fullHealth * 0.1f)
                {
                    damageInfo.damage = victim.fullHealth * 0.1f;
                }

                if (victim.itemCounts.minHealthPercentage > 0)
                {
                    float num10 = victim.fullCombinedHealth * (victim.itemCounts.minHealthPercentage / 100f);
                    damageInfo.damage = Mathf.Max(0f, Mathf.Min(damageInfo.damage, victim.combinedHealth - num10));
                }
            }

            float barrierDamage = Mathf.Min(damageInfo.damage, victim.barrier);
            float remainingBarrier = victim.barrier - barrierDamage;
            damageInfo.damage -= barrierDamage;

            float shieldDamage = Mathf.Min(damageInfo.damage, victim.shield);
            float remainingShield = victim.shield - shieldDamage;
            damageInfo.damage -= shieldDamage;

            if (victim.itemCounts.unstableTransmitter > 0 && victim.IsHealthBelowThreshold(victim.health - damageInfo.damage, 0.25f) && victim.body.HasBuff(DLC2Content.Buffs.TeleportOnLowHealth) && victim.health - damageInfo.damage > 0f && victim.GetComponent<TeleportOnLowHealthBehavior>())
            {
                int minHealth = victim.GetHealthAtThreshold(0.25f) + 1;
                float transmitterDamage = victim.health - minHealth;
                if (transmitterDamage > 0f)
                {
                    damageInfo.damage = transmitterDamage;
                }
            }

            bool isVoidDeath = (damageInfo.damageType & DamageType.VoidDeath) != 0 && (victim.body.bodyFlags & CharacterBody.BodyFlags.ImmuneToVoidDeath) == 0;

            float remainingHealth = victim.health;

            if (damageInfo.damage > 0f)
            {
                float healthAfterDamage = remainingHealth - damageInfo.damage;
                if (healthAfterDamage < 1f && (damageInfo.damageType & DamageType.NonLethal) != 0 && remainingHealth >= 1f)
                {
                    healthAfterDamage = 1f;
                }

                remainingHealth = healthAfterDamage;
            }

            float executeFraction = float.NegativeInfinity;
            bool immuneToExecutes = (victim.body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) != 0;
            if (!isVoidDeath && !immuneToExecutes)
            {
                if (victim.isInFrozenState && executeFraction < 0.3f)
                {
                    executeFraction = 0.3f;
                }

                if (attackerBody)
                {
                    if (victim.body.isElite)
                    {
                        float executeEliteHealthFraction = attackerBody.executeEliteHealthFraction;
                        if (executeFraction < executeEliteHealthFraction)
                        {
                            executeFraction = executeEliteHealthFraction;
                        }
                    }
                }
            }

            float remainingCombinedHealth = remainingHealth + remainingShield + remainingBarrier;
            float remainingCombinedHealthFraction = remainingCombinedHealth / victim.fullCombinedHealth;

            if (isVoidDeath || (executeFraction > 0f && remainingCombinedHealthFraction <= executeFraction))
            {
                isVoidDeath = true;
                damageInfo.damage = victim.health;
            }

            damageInfo.damage += barrierDamage + shieldDamage;

            return damageInfo.damage;
        }
    }
}
