using HutongGames.PlayMaker;
using Modding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vasi;

namespace HollowKnight.OrbCounter
{
    public class OrbCounter : Mod, IMenuMod, ITogglableMod
    { 
        public override string GetVersion() => "1.0.1";

        public bool ToggleButtonInsideMenu => true;

        private static readonly FieldInfo spawnedOrbs = typeof(DreamPlant).GetField("spawnedOrbs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo pickedUp = typeof(DreamPlantOrb).GetField("pickedUp", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool orbPickedUp = false;

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            IMenuMod.MenuEntry e = toggleButtonEntry.Value;

            return new() { new(e.Name, e.Values, "", e.Saver, e.Loader) };
        }

        public override void Initialize()
        {
            // If mod is enabled during the game for the first time
            if (GameManager.instance.IsGameplayScene())
            {
                PlayMakerFSM counterFSM = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Dream Nail").FirstOrDefault(obj => obj.LocateMyFSM("Control") != null).LocateMyFSM("Control");
                
                if (counterFSM != null && counterFSM.GetState("Update").Actions.Count() == 3)
                {
                    counterFSM.GetState("Update").InsertAction(2, new AddOrbCounter(counterFSM));
                }
            }

            On.DreamPlantOrb.OnTriggerEnter2D += DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        }

        public void Unload()
        {
            On.DreamPlantOrb.OnTriggerEnter2D -= DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable -= PlayMakerFSM_OnEnable;
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

                DreamPlantOrb[] orbs = Object.FindObjectsOfType<DreamPlantOrb>();

                DreamPlant plant = Object.FindObjectOfType<DreamPlant>();

                if (orbPickedUp && orbs.Count() > 0 && plant != null)
                {
                    int remainingOrbs = (int) spawnedOrbs.GetValue(plant) - 1;

                    if (remainingOrbs >= 0)
                    {
                        text.Value = text + " (" + (orbs.Count() - remainingOrbs) + "/" + orbs.Count() + ")";
                    }
                }

                orbPickedUp = false;

                Finish();
            }
        }
    }
}