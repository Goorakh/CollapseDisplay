using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace CollapseDisplay
{
    [DisallowMultipleComponent]
    public class CollapseDamageProvider : NetworkBehaviour
    {
        [SyncVar]
        float _estimatedCollapseDamage;
        public float EstimatedCollapseDamage => _estimatedCollapseDamage;

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
            float totalCollapseDamage = -1f;

            if (_body && _healthComponent && _healthComponent.alive)
            {
                DotController dotController = DotController.FindDotController(gameObject);
                if (dotController && dotController.HasDotActive(DotController.DotIndex.Fracture))
                {
                    totalCollapseDamage = 0f;
                    foreach (DotController.DotStack dotStack in dotController.dotStackList)
                    {
                        if (dotStack.dotIndex != DotController.DotIndex.Fracture)
                            continue;

                        float stackDamage = dotStack.damage;

                        if (dotStack.attackerTeam == _body.teamComponent.teamIndex)
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
                                if (_healthComponent.combinedHealth > _healthComponent.fullCombinedHealth * 0.9f)
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
                                    Vector3 vectorToAttacker = attackerBody.corePosition - _body.corePosition;
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

                                if (_body.isBoss)
                                {
                                    int bossDamageBonusItemCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.BossDamageBonus);
                                    if (bossDamageBonusItemCount > 0)
                                    {
                                        stackDamage *= 1f + (0.2f * bossDamageBonusItemCount);
                                    }
                                }
                            }
                        }

                        if (_body.HasBuff(RoR2Content.Buffs.DeathMark))
                        {
                            stackDamage *= 1.5f;
                        }

                        float armor = _body.armor + _healthComponent.adaptiveArmorValue;
                        float armorDamageMultiplier = armor >= 0f ? 1f - (armor / (armor + 100f))
                                                                  : 2f - (100f / (100f - armor));

                        stackDamage = Mathf.Max(1f, stackDamage * armorDamageMultiplier);

                        if (_body.inventory)
                        {
                            int armorPlateItemCount = _body.inventory.GetItemCount(RoR2Content.Items.ArmorPlate);
                            if (armorPlateItemCount > 0)
                            {
                                stackDamage = Mathf.Max(1f, stackDamage - (5f * armorPlateItemCount));
                            }
                        }

                        if (_body.hasOneShotProtection)
                        {
                            float unprotectedHealth = (_healthComponent.fullCombinedHealth + _healthComponent.barrier) * (1f - _body.oneShotProtectionFraction);
                            float maxAllowedDamage = Mathf.Max(0f, unprotectedHealth - _healthComponent.serverDamageTakenThisUpdate);

                            stackDamage = Mathf.Min(stackDamage, maxAllowedDamage);
                        }

                        if (_body.HasBuff(RoR2Content.Buffs.LunarShell) && stackDamage > _healthComponent.fullHealth * 0.1f)
                        {
                            stackDamage = _healthComponent.fullHealth * 0.1f;
                        }

                        if (_body.inventory)
                        {
                            int minHealthPercentageItemCount = _body.inventory.GetItemCount(RoR2Content.Items.MinHealthPercentage);
                            if (minHealthPercentageItemCount > 0)
                            {
                                float minHealth = _healthComponent.fullCombinedHealth * (minHealthPercentageItemCount / 100f);
                                stackDamage = Mathf.Max(0f, Mathf.Min(stackDamage, _healthComponent.combinedHealth - minHealth));
                            }
                        }

                        if (stackDamage > 0f)
                        {
                            totalCollapseDamage += stackDamage;
                        }
                    }

                    totalCollapseDamage -= _healthComponent.barrier;
                }
            }

            _estimatedCollapseDamage = totalCollapseDamage;
        }
    }
}
