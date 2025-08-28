using CollapseDisplay.Utilities;
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

                            DamageInfo stackDamageInfo = new DamageInfo
                            {
                                attacker = dotStack.attackerObject,
                                damageType = dotStack.damageType | DamageType.DoT,
                                damage = dotStack.damage
                            };

                            float stackDamage = HealthComponentUtils.EstimateTakeDamage(_healthComponent, stackDamageInfo);
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
            }

            _collapseDamage = collapseDamage;
        }
    }
}
