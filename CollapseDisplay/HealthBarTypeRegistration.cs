using RoR2;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CollapseDisplay
{
    static class HealthBarTypeRegistration
    {
        static readonly HealthBarStyle _combatHealthBarStyle = Addressables.LoadAssetAsync<HealthBarStyle>(RoR2_Base_Common.CombatHealthBar_asset).WaitForCompletion();

        static readonly HealthBarStyle _hudHealthBarStyle = Addressables.LoadAssetAsync<HealthBarStyle>(RoR2_Base_Common.HUDHealthBar_asset).WaitForCompletion();

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
                    Log.Warning($"No health bars found on asset '{prefab.name}'");
                }
                else
                {
                    Log.Debug($"Registered {healthBarsRegistered} health bar(s) as {type} on asset '{prefab.name}'");
                }
            }

            registerHealthBarPrefab(RoR2_Base_UI.HUDSimple_prefab, HealthBarType.Hud);
            registerHealthBarPrefab(RoR2_Base_UI.HUDSimple_Big_prefab, HealthBarType.Hud);
            registerHealthBarPrefab(RoR2_Base_UI.CombatHealthbar_prefab, HealthBarType.Combat);
            registerHealthBarPrefab(RoR2_Base_UI.AllyCard_prefab, HealthBarType.AllyList);
            registerHealthBarPrefab(RoR2_DLC3_Drifter.Bag_UI_prefab, HealthBarType.BaggedObject);
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
                if (self._style == _combatHealthBarStyle)
                {
                    barType = HealthBarType.Combat;
                }
                else if (self._style == _hudHealthBarStyle)
                {
                    barType = HealthBarType.Hud;
                }

                Log.Debug($"Health bar {Util.GetGameObjectHierarchyName(self.gameObject)} is not registered, assuming bar type of {barType}");

                HealthBarTypeProvider typeProvider = self.gameObject.AddComponent<HealthBarTypeProvider>();
                typeProvider.Type = barType;
            }

            orig(self);
        }
    }
}
