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
    public class OrbTracker : Mod, IMenuMod, IGlobalSettings<GlobalSettings>
    { 
        public override string GetVersion() => "1.1.2";

        internal static OrbTracker Instance;
        public static GlobalSettings GS { get; set; } = new GlobalSettings();
        public void OnLoadGlobal(GlobalSettings gs) => GS = gs;
        public GlobalSettings OnSaveGlobal() => GS;
        public bool ToggleButtonInsideMenu => false;

        private static readonly FieldInfo spawnedOrbs = typeof(DreamPlant).GetField("spawnedOrbs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo pickedUp = typeof(DreamPlantOrb).GetField("pickedUp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo isActive = typeof(DreamPlantOrb).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool orbPickedUp = false;

        private GameObject compass;
        private DirectionalCompass CompassC => compass?.GetComponent<DirectionalCompass>();
        private GameObject Knight => HeroController.instance?.gameObject;

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new()
            {
                new()
                {
                    Name = "Toggle Counter",
                    Description = "",
                    Values = new string[] { "Off", "On" },
                    Saver = opt => GS.EnableCounter = !GS.EnableCounter,
                    Loader = () => GS.EnableCounter ? 1 : 0
                },

                new()
                {
                    Name = "Toggle Compass",
                    Description = "",
                    Values = new string[] { "Off", "On" },
                    Saver = opt => GS.EnableCompass = !GS.EnableCompass,
                    Loader = () => GS.EnableCompass ? 1 : 0
                }
            };
        }

        public override void Initialize()
        {
            Instance = this;

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            On.DreamPlantOrb.SetActive += DreamPlantOrb_SetActive;
            On.DreamPlantOrb.OnTriggerEnter2D += DreamPlantOrb_OnTriggerEnter2D;
            On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        }

        // Destroy/create a new compass. In my case I don't want the compass to be initially visible
        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            if (Knight == null) return;

            compass?.GetComponent<DirectionalCompass>()?.Destroy();

            compass = DirectionalCompass.Create
            (
                "Orb Compass", // name
                Knight, // parent entity
                SpriteManager.Instance.GetSprite("arrow"), // sprite
                new Vector4(210, 74, 111, 180) / 255f, // color
                1.5f, // radius
                2.0f, // scale
                IsCompassEnabled, // bool condition
                true, // lerp
                0.5f // lerp duration
            );;

            compass?.SetActive(false);
        }

        // Activate/deactivate/update the compass based on something that occurs in-scene
        private void DreamPlantOrb_SetActive(On.DreamPlantOrb.orig_SetActive orig, DreamPlantOrb self, bool value)
        {
            orig(self, value);

            if (value)
            {
                compass?.SetActive(true);

                UpdateCompass();
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
                UpdateCompass();
            }
        }

        // A bool condition you can pass to DirectionalCompass. Here it is a global setting
        public bool IsCompassEnabled()
        {
            return GS.EnableCompass;
        }

        // You will need to manually pass the list of tracked objects
        public void UpdateCompass()
        {
            List<GameObject> trackedObjects = new(Object.FindObjectsOfType<DreamPlantOrb>().Where(o => !(bool)pickedUp.GetValue(o) && (bool)isActive.GetValue(o)).Select(o => o.gameObject));

            if (CompassC != null && trackedObjects.Any())
            {
                CompassC.trackedObjects = trackedObjects;
            }
            else
            {
                compass?.SetActive(false);
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

                if (GS.EnableCounter && orbPickedUp && orbs.Count() > 0 && plant != null)
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

    public class GlobalSettings
    {
        public bool EnableCounter = true;
        public bool EnableCompass = true;
    }
}