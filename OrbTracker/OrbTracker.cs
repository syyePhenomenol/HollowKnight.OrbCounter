using HutongGames.PlayMaker;
using Modding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;

namespace OrbTracker
{
    public class OrbTracker : Mod, IMenuMod, ITogglableMod
    { 
        public override string GetVersion() => "1.1.0";

        internal static OrbTracker Instance;

        public bool ToggleButtonInsideMenu => true;

        private static readonly FieldInfo spawnedOrbs = typeof(DreamPlant).GetField("spawnedOrbs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo pickedUp = typeof(DreamPlantOrb).GetField("pickedUp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo isActive = typeof(DreamPlantOrb).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool orbPickedUp = false;

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            IMenuMod.MenuEntry e = toggleButtonEntry.Value;

            return new() { new(e.Name, e.Values, "", e.Saver, e.Loader) };
        }

        public override void Initialize()
        {
            Instance = this;

            // If mod is enabled during the game for the first time
            if (GameManager.instance.IsGameplayScene())
            {
                PlayMakerFSM counterFSM = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Dream Nail").FirstOrDefault(obj => obj.LocateMyFSM("Control") != null).LocateMyFSM("Control");
                
                if (counterFSM != null && counterFSM.GetState("Update").Actions.Count() == 3)
                {
                    counterFSM.GetState("Update").InsertAction(2, new AddOrbCounter(counterFSM));
                }

                CreateDirectionalCompass();

                if (Object.FindObjectsOfType<DreamPlantOrb>().Any(o => (bool)isActive.GetValue(o)))
                {
                    ActivateDirectionalCompass();
                }
            }

            //On.DreamPlant.OnTriggerEnter2D += DreamPlant_OnTriggerEnter2D;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            On.DreamPlantOrb.SetActive += DreamPlantOrb_SetActive;
            On.DreamPlantOrb.OnTriggerEnter2D += DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        }

        public void Unload()
        {
            HeroController.instance?.GetComponent<DirectionalCompass>()?.Destroy();

            //On.DreamPlant.OnTriggerEnter2D -= DreamPlant_OnTriggerEnter2D;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
            On.DreamPlantOrb.SetActive -= DreamPlantOrb_SetActive;
            On.DreamPlantOrb.OnTriggerEnter2D -= DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable -= PlayMakerFSM_OnEnable;
        }

        private void DreamPlantOrb_SetActive(On.DreamPlantOrb.orig_SetActive orig, DreamPlantOrb self, bool value)
        {
            orig(self, value);

            if (value)
            {
                ActivateDirectionalCompass();
            }
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            CreateDirectionalCompass();
        }

        private void CreateDirectionalCompass()
        {
            GameObject knight = HeroController.instance?.gameObject;

            if (knight != null)
            {
                HeroController.instance?.GetComponent<DirectionalCompass>()?.Destroy();

                DirectionalCompass compassC = knight.AddComponent<DirectionalCompass>();
                compassC.trackedObjects = new(Object.FindObjectsOfType<DreamPlantOrb>().Where(o => !(bool)pickedUp.GetValue(o) && (bool)isActive.GetValue(o)).Select(o => o.gameObject));
            }
        }

        private void RefreshTrackedOrbs()
        {
            DirectionalCompass compassC = HeroController.instance?.GetComponent<DirectionalCompass>();

            if (compassC != null)
            {
                compassC.trackedObjects = new(Object.FindObjectsOfType<DreamPlantOrb>().Where(o => !(bool)pickedUp.GetValue(o) && (bool)isActive.GetValue(o)).Select(o => o.gameObject));
            }
        }

        private void ActivateDirectionalCompass()
        {
            DirectionalCompass compassC = HeroController.instance?.GetComponent<DirectionalCompass>();

            if (compassC != null)
            {
                compassC.active = true;
            }
        }

        private void DreamPlantOrb_OnTriggerEnter2D(On.DreamPlantOrb.orig_OnTriggerEnter2D orig, DreamPlantOrb self, Collider2D collision)
        {
            if (!(bool) pickedUp.GetValue(self) && collision.tag == "Player")
            {
                orbPickedUp = true;
            }

            orig(self, collision);

            if (collision.tag == "Player")
            {
                RefreshTrackedOrbs();
            }
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