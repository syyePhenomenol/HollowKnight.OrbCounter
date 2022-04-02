using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OrbTracker
{
    internal class DirectionalCompass : MonoBehaviour
    {
        public GameObject compass;

        public bool active = false;

        public float radius = 1.5f;

        public List<GameObject> trackedObjects;

        public void Start()
        {
            compass = new("Directional Compass", typeof(SpriteRenderer));

            compass.layer = 18;

            DontDestroyOnLoad(compass);

            SpriteRenderer sr = compass.GetComponent<SpriteRenderer>();

            sr.sprite = SpriteManager.Instance.GetSprite("arrow");

            compass.transform.parent = transform;
            compass.transform.localScale = Vector3.one * 2.0f;
        }

        public void Update()
        {
            try
            {
                if (active && TryGetClosestObject(out GameObject o))
                {
                    Vector3 dir = o.transform.position - transform.position;

                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    if (HeroController.instance.GetCState("facingRight"))
                    {
                        dir.x = -dir.x;
                    }

                    compass.transform.localPosition = Vector3.ClampMagnitude(dir, radius);

                    compass.transform.eulerAngles = new(0, 0, angle - 90);

                    compass.SetActive(true);
                }
                else
                {
                    compass.SetActive(false);
                }
            }
            catch (Exception e)
            {
                OrbTracker.Instance.LogError(e);
            }
        }

        private bool TryGetClosestObject(out GameObject o)
        {
            if (trackedObjects == null || !trackedObjects.Any())
            {
                o = null;

                return false;
            }

            o =  trackedObjects.Aggregate((i1, i2) => SqrDistanceFromKnight(i1) < SqrDistanceFromKnight(i2) ? i1 : i2);
            
            return o != null;
        }

        private float SqrDistanceFromKnight(GameObject o)
        {
            if (o == null) return 9999f;

            return (o.transform.position - transform.position).sqrMagnitude;
        }

        public void Destroy()
        {
            Destroy(compass);
            Destroy(this);
        }
    }
}
