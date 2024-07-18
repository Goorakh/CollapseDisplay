using RoR2;

namespace CollapseDisplay
{
    static class CollapseDamageProviderHooks
    {
        public static void Initialize()
        {
            On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
        }

        public static void Cleanup()
        {
            On.RoR2.HealthComponent.Awake -= HealthComponent_Awake;
        }

        static void HealthComponent_Awake(On.RoR2.HealthComponent.orig_Awake orig, HealthComponent self)
        {
            orig(self);

            self.gameObject.AddComponent<CollapseDamageProvider>();
        }
    }
}
