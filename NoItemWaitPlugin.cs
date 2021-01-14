using System;
using System.Linq;
using RoR2;
using BepInEx;
using R2API.Utils;
using UnityEngine;
using MonoMod.Cil;

namespace NoItemWait
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class NoItemWaitPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Windows10CE.NoItemWait";
        public const string ModName = "NoItemWait";
        public const string ModVer = "1.0.0";

        internal static float autoPickupDistance = -1;
        internal static bool modEnabled = true;

        public void Awake()
        {
            modEnabled = Config.Bind<bool>("NoItemWait", nameof(modEnabled), true, "Whether or not the mod should be enabled.").Value;
            autoPickupDistance = Config.Bind<float>("NoItemWait", nameof(autoPickupDistance), 15f, "How far away you can be from an item before it picks up automatically. Set to -1 to use the base game value.").Value;

            if (!modEnabled)
                return;
            
            IL.RoR2.PickupDropletController.CreatePickupDroplet += CreatePickupDropletHook;
            On.RoR2.GenericPickupController.CreatePickup += GenericPickupCreateHook;

#if DEBUG
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif
        }

        internal static void CreatePickupDropletHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(2),
                x => x.MatchCallOrCallvirt(typeof(Rigidbody).GetProperty("velocity").GetSetMethod())
            );

            c.Remove();
            c.EmitDelegate<Func<Vector3>>(() => new Vector3());
        }

        internal static GenericPickupController GenericPickupCreateHook(On.RoR2.GenericPickupController.orig_CreatePickup orig, ref GenericPickupController.CreatePickupInfo info)
        {
            var self = orig(ref info);

            self.waitDuration = 0;
            var collider = self.gameObject.GetComponentsInChildren<SphereCollider>().First(x => x.isTrigger);
            collider.radius = NoItemWaitPlugin.autoPickupDistance < 0 ? collider.radius : autoPickupDistance;

            return self;
        }
    }
}
