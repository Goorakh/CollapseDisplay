using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CollapseDisplay
{
    static class HealthBarTypeRegistration
    {
        static readonly HealthBarStyle _combatHealthBarStyle = Addressables.LoadAssetAsync<HealthBarStyle>("RoR2/Base/Common/CombatHealthBar.asset").WaitForCompletion();

        static readonly List<HealthBarTypeProvider> _addedPrefabProviders = [];

        public static void Initialize()
        {
            On.RoR2.UI.HealthBar.Awake += HealthBar_Awake;

            static void registerHealthBarPrefab(string assetPath, HealthBarType type)
            {
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(assetPath).WaitForCompletion();
                if (!prefab)
                {
                    Log.Error($"Invalid asset path '{assetPath}'");
                    return;
                }

                int healthBarsRegistered = 0;
                foreach (HealthBar healthBar in prefab.GetComponentsInChildren<HealthBar>(true))
                {
                    if (!healthBar.GetComponent<HealthBarTypeProvider>())
                    {
                        HealthBarTypeProvider typeProvider = healthBar.gameObject.AddComponent<HealthBarTypeProvider>();
                        typeProvider.Type = type;

                        _addedPrefabProviders.Add(typeProvider);

                        healthBarsRegistered++;
                    }
                }

                if (healthBarsRegistered == 0)
                {
                    Log.Warning($"No health bars found on asset '{assetPath}'");
                }
                else
                {
#if DEBUG
                    Log.Debug($"Registered {healthBarsRegistered} health bar(s) as {type} on asset '{assetPath}'");
#endif
                }
            }

            registerHealthBarPrefab("RoR2/Base/UI/HUDSimple.prefab", HealthBarType.Hud);
            registerHealthBarPrefab("RoR2/Base/UI/CombatHealthbar.prefab", HealthBarType.Combat);
            registerHealthBarPrefab("RoR2/Base/UI/AllyCard.prefab", HealthBarType.AllyList);
        }

        public static void Cleanup()
        {
            On.RoR2.UI.HealthBar.Awake -= HealthBar_Awake;

            foreach (HealthBarTypeProvider typeProvider in _addedPrefabProviders)
            {
                GameObject.Destroy(typeProvider);
            }

            _addedPrefabProviders.Clear();
        }

        static void HealthBar_Awake(On.RoR2.UI.HealthBar.orig_Awake orig, HealthBar self)
        {
            if (!self.GetComponent<HealthBarTypeProvider>())
            {
                HealthBarType barType = HealthBarType.Unknown;
                if (self.style == _combatHealthBarStyle)
                {
                    barType = HealthBarType.Combat;
                }

                Log.Info($"Health bar {self} is not registered, assuming bar type of {barType}");

                HealthBarTypeProvider typeProvider = self.gameObject.AddComponent<HealthBarTypeProvider>();
                typeProvider.Type = barType;
            }

            orig(self);
        }
    }
}
