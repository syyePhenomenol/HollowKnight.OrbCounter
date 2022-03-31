using HutongGames.PlayMaker;
using Modding;
using System.Linq;
using System.Reflection;
using Vasi;

namespace HollowKnight.OrbCounter
{
    public class OrbCounter : Mod
    {
        public static OrbCounter Instance;

        public override string GetVersion() => "1.0.0";

        private static FieldInfo spawnedOrbs;

        private static FieldInfo pickedUp;

        private static bool orbPickedUp = false;

        public override void Initialize()
        {
            Instance = this;

            spawnedOrbs = typeof(DreamPlant).GetField("spawnedOrbs", BindingFlags.Instance | BindingFlags.NonPublic);
            pickedUp = typeof(DreamPlantOrb).GetField("pickedUp", BindingFlags.Instance | BindingFlags.NonPublic);

            On.DreamPlantOrb.OnTriggerEnter2D += DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        }

        private void DreamPlantOrb_OnTriggerEnter2D(On.DreamPlantOrb.orig_OnTriggerEnter2D orig, DreamPlantOrb self, UnityEngine.Collider2D collision)
        {
            if (!(bool) pickedUp.GetValue(self) && collision.tag == "Player")
            {
                orbPickedUp = true;
            }

            orig(self, collision);
        }

        private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.gameObject.name == "Dream Nail" && self.FsmName == "Control")
            {
                self.GetState("Update").InsertAction(2, new AddOrbCounter(self));
            }
        }

        private class AddOrbCounter : FsmStateAction
        {
            private readonly PlayMakerFSM _self;

            public AddOrbCounter(PlayMakerFSM self)
            {
                _self = self;
            }

            public override void OnEnter()
            {
                FsmString text = _self.FsmVariables.StringVariables.FirstOrDefault(x => x.Name == "Orbs String");

                DreamPlantOrb[] orbs = UnityEngine.Object.FindObjectsOfType<DreamPlantOrb>();

                DreamPlant plant = UnityEngine.Object.FindObjectOfType<DreamPlant>();

                if (orbPickedUp && orbs.Count() > 0 && plant != null)
                {
                    orbPickedUp = false;

                    int remainingOrbs = (int) spawnedOrbs.GetValue(plant) - 1;

                    if (remainingOrbs >= 0)
                    {
                        text.Value = text + " (" + (orbs.Count() - remainingOrbs) + "/" + orbs.Count() + ")";
                    }
                }

                Finish();
            }
        }
    }
}