using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace CollapseDisplay
{
    [DisallowMultipleComponent]
    public class DelayedDamageProvider : NetworkBehaviour
    {
        [SyncVar]
        DelayedDamageInfo _collapseDamage;

        public DelayedDamageInfo CollapseDamage => _collapseDamage;

        [SyncVar]
        DelayedDamageInfo _warpedEchoDamage;

        public DelayedDamageInfo WarpedEchoDamage => _warpedEchoDamage;

        CharacterBody _body;
        HealthComponent _healthComponent;

        void Awake()
        {
            _body = GetComponent<CharacterBody>();
            _healthComponent = GetComponent<HealthComponent>();
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }
        }

        [Server]
        void fixedUpdateServer()
        {
            DelayedDamageInfo collapseDamage = DelayedDamageInfo.None;
            DelayedDamageInfo warpedEchoDamage = DelayedDamageInfo.None;

            if (_body)
            {
                if (_healthComponent && _healthComponent.alive)
                {
                    DotController dotController = DotController.FindDotController(gameObject);
                    if (dotController && dotController.HasDotActive(DotController.DotIndex.Fracture))
                    {
                        float minDotTimer = float.PositiveInfinity;

                        collapseDamage.Damage = 0f;
                        foreach (DotController.DotStack dotStack in dotController.dotStackList)
                        {
                            if (dotStack.dotIndex != DotController.DotIndex.Fracture)
                                continue;

                            minDotTimer = Mathf.Min(minDotTimer, dotStack.timer);

                            float stackDamage = dotStack.damage;

                            DamageInfo stackDamageInfo = new DamageInfo
                            {
                                attacker = dotStack.attackerObject,
                                damageType = dotStack.damageType | DamageType.DoT,
                                damage = stackDamage
                            };

                            modifyIncomingDamage(ref stackDamage, stackDamageInfo);

                            if (stackDamage > 0f)
                            {
                                collapseDamage.Damage += stackDamage;
                            }
                        }

                        if (float.IsFinite(minDotTimer) && minDotTimer > 0f)
                        {
                            collapseDamage.DamageTimestamp = Run.FixedTimeStamp.now + minDotTimer;
                        }
                    }
                }

                if (_body.incomingDamageList.Count > 0)
                {
                    float minDamageDelay = float.PositiveInfinity;

                    warpedEchoDamage.Damage = 0f;
                    foreach (CharacterBody.DelayedDamageInfo delayedDamage in _body.incomingDamageList)
                    {
                        minDamageDelay = Mathf.Min(minDamageDelay, delayedDamage.timeUntilDamage);

                        DamageInfo halfDamageInfo = delayedDamage.halfDamage;

                        float damage = halfDamageInfo.damage;

                        modifyIncomingDamage(ref damage, halfDamageInfo);

                        if (damage > 0f)
                        {
                            warpedEchoDamage.Damage += damage;
                        }
                    }

                    if (float.IsFinite(minDamageDelay) && minDamageDelay > 0f)
                    {
                        warpedEchoDamage.DamageTimestamp = Run.FixedTimeStamp.now + minDamageDelay;
                    }
                }
            }

            _collapseDamage = collapseDamage;
            _warpedEchoDamage = warpedEchoDamage;
        }

        void modifyIncomingDamage(ref float damage, DamageInfo damageInfo)
        {
            bool isCrit = damageInfo.crit;

            bool bypassArmor = (damageInfo.damageType & DamageType.BypassArmor) != 0;

            CharacterMaster attackerMaster = null;
            CharacterBody attackerBody = null;
            TeamIndex attackerTeamIndex = TeamIndex.None;
            Vector3 damagePositionToAttackerVector = Vector3.zero;
            float victimCombinedHealth = _healthComponent.combinedHealth;
            if (damageInfo.attacker)
            {
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    attackerTeamIndex = attackerBody.teamComponent.teamIndex;
                    damagePositionToAttackerVector = attackerBody.corePosition - damageInfo.position;
                }
            }

            if (attackerTeamIndex == _body.teamComponent.teamIndex)
            {
                TeamDef attackerTeamDef = TeamCatalog.GetTeamDef(attackerTeamIndex);
                if (attackerTeamDef != null)
                {
                    damage *= attackerTeamDef.friendlyFireScaling;
                }
            }

            if (attackerBody)
            {
                if (attackerBody.canPerformBackstab && (damageInfo.damageType & DamageType.DoT) == 0 && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-damagePositionToAttackerVector, _body)))
                {
                    isCrit = true;
                }

                attackerMaster = attackerBody.master;

                if (attackerMaster && attackerMaster.inventory)
                {
                    if (victimCombinedHealth >= _healthComponent.fullCombinedHealth * 0.9f)
                    {
                        int crowbarItemCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.Crowbar);
                        if (crowbarItemCount > 0)
                        {
                            damage *= 1f + (0.75f * crowbarItemCount);
                        }
                    }

                    int nearbyDamageBonusItemCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.NearbyDamageBonus);
                    if (nearbyDamageBonusItemCount > 0 && damagePositionToAttackerVector.sqrMagnitude <= 13f * 13f)
                    {
                        damage *= 1f + (nearbyDamageBonusItemCount * 0.2f);
                    }

                    int fragileDamageBonusItemCount = attackerMaster.inventory.GetItemCount(DLC1Content.Items.FragileDamageBonus);
                    if (fragileDamageBonusItemCount > 0)
                    {
                        damage *= 1f + (fragileDamageBonusItemCount * 0.2f);
                    }

                    if (attackerBody.HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff))
                    {
                        int lowerHealthHigherDamageItemCount = attackerMaster.inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage);
                        damage *= 1f + (lowerHealthHigherDamageItemCount * 0.2f);
                    }

                    if (damageInfo.procCoefficient > 0f)
                    {
                        if (_body.HasBuff(RoR2Content.Buffs.MercExpose) && attackerBody.bodyIndex == BodyCatalog.FindBodyIndex("MercBody"))
                        {
                            float exposeDamage = attackerBody.damage * 3.5f;
                            damage += exposeDamage;
                        }
                    }

                    if (_body.isBoss)
                    {
                        int bossDamageBonusItemCount = attackerMaster.inventory.GetItemCount(RoR2Content.Items.BossDamageBonus);
                        if (bossDamageBonusItemCount > 0)
                        {
                            damage *= 1f + (0.2f * bossDamageBonusItemCount);
                        }
                    }
                }

                if (isCrit)
                {
                    damage *= attackerBody.critMultiplier;
                }
            }

            if ((damageInfo.damageType & DamageType.WeakPointHit) != 0)
            {
                damage *= 1.5f;
            }

            if (_body.HasBuff(RoR2Content.Buffs.DeathMark))
            {
                damage *= 1.5f;
            }

            if (!bypassArmor)
            {
                float armor = _body.armor;
                armor += _healthComponent.adaptiveArmorValue;

                if ((_body.bodyFlags & CharacterBody.BodyFlags.ResistantToAOE) != 0 && (damageInfo.damageType & DamageType.AOE) != 0)
                {
                    armor += 300f;
                }

                float armorDamageMultiplier = armor >= 0f ? 1f - (armor / (armor + 100f))
                                                          : 2f - (100f / (100f - armor));

                damage = Mathf.Max(1f, damage * armorDamageMultiplier);

                if (_body.inventory)
                {
                    int armorPlateItemCount = _body.inventory.GetItemCount(RoR2Content.Items.ArmorPlate);
                    if (armorPlateItemCount > 0)
                    {
                        damage = Mathf.Max(1f, damage - (5f * armorPlateItemCount));
                    }
                }
            }

            if (_body.hasOneShotProtection)
            {
                float unprotectedHealth = (_healthComponent.fullCombinedHealth + _healthComponent.barrier) * (1f - _body.oneShotProtectionFraction);
                float maxAllowedDamage = Mathf.Max(0f, unprotectedHealth - _healthComponent.serverDamageTakenThisUpdate);

                damage = Mathf.Min(damage, maxAllowedDamage);
            }

            if ((damageInfo.damageType & DamageType.BonusToLowHealth) != 0)
            {
                float lowHealthDamageMultiplier = Mathf.Lerp(3f, 1f, _healthComponent.combinedHealthFraction);
                damage *= lowHealthDamageMultiplier;
            }

            if (_body.HasBuff(RoR2Content.Buffs.LunarShell) && damage > _healthComponent.fullHealth * 0.1f)
            {
                damage = _healthComponent.fullHealth * 0.1f;
            }

            if (_body.inventory)
            {
                int minHealthPercentageItemCount = _body.inventory.GetItemCount(RoR2Content.Items.MinHealthPercentage);
                if (minHealthPercentageItemCount > 0)
                {
                    float minHealth = _healthComponent.fullCombinedHealth * (minHealthPercentageItemCount / 100f);
                    damage = Mathf.Max(0f, Mathf.Min(damage, _healthComponent.combinedHealth - minHealth));
                }
            }

            if (!damageInfo.delayedDamageSecondHalf && _body.HasBuff(DLC2Content.Buffs.DelayedDamageBuff))
            {
                damage *= 0.5f;
            }
        }
    }
}
