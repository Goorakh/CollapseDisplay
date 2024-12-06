using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace CollapseDisplay
{
    static class DelayedDamageProviderHooks
    {
        static readonly List<DelayedDamageProvider> _delayedDamageProviderPrefabComponents = [];

        public static void Initialize()
        {
            destroyAllPrefabComponents();
            BodyCatalog.availability.CallWhenAvailable(() =>
            {
                if (_delayedDamageProviderPrefabComponents.Capacity < BodyCatalog.bodyCount)
                    _delayedDamageProviderPrefabComponents.Capacity = BodyCatalog.bodyCount;

                foreach (GameObject bodyPrefab in BodyCatalog.allBodyPrefabs)
                {
                    if (bodyPrefab.GetComponent<HealthComponent>())
                    {
                        DelayedDamageProvider delayedDamageProvider = bodyPrefab.AddComponent<DelayedDamageProvider>();
                        _delayedDamageProviderPrefabComponents.Add(delayedDamageProvider);
                    }
                }
            });
        }

        public static void Cleanup()
        {
            destroyAllPrefabComponents();
        }

        static void destroyAllPrefabComponents()
        {
            foreach (DelayedDamageProvider delayedDamageProvider in _delayedDamageProviderPrefabComponents)
            {
                GameObject.Destroy(delayedDamageProvider);
            }

            _delayedDamageProviderPrefabComponents.Clear();
        }
    }
}
