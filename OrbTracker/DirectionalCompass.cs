using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OrbTracker
{
    internal class DirectionalCompass : MonoBehaviour
    {
        private GameObject compassInternal;
        private GameObject entity;

        private Func<bool> Condition;

        private float radius;

        public List<GameObject> trackedObjects;

        public static GameObject Create(string name, GameObject entity, Sprite sprite, float radius, float scale, Func<bool> condition)
        {
            // This object is a container for the script. Can be set active/inactive externally to control script
            GameObject compass = new(name, typeof(SpriteRenderer));
            DontDestroyOnLoad(compass);

            compass.transform.parent = entity.transform;

            DirectionalCompass dc = compass.AddComponent<DirectionalCompass>();

            // This object is the actual compass sprite. Set active/inactive by the script itself
            dc.compassInternal = new(name + " Internal", typeof(SpriteRenderer));
            DontDestroyOnLoad(dc.compassInternal);
            dc.compassInternal.layer = 18;

            SpriteRenderer sr = dc.compassInternal.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;

            dc.compassInternal.transform.parent = compass.transform;
            dc.compassInternal.transform.localScale = Vector3.one * scale;

            dc.entity = entity;
            dc.radius = radius;
            dc.Condition = condition;

            return compass;
        }

        public void Destroy()
        {
            Destroy(compassInternal);
            Destroy(gameObject);
        }

        public void Update()
        {
            if (entity != null && Condition() && TryGetClosestObject(out GameObject o))
            {
                Vector3 dir = o.transform.position - entity.transform.position;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Reverse the reflection caused by facing right
                dir.x *= entity.transform.localScale.x;

                transform.localPosition = Vector3.ClampMagnitude(dir, radius);
                transform.eulerAngles = new(0, 0, angle - 90);

                compassInternal.SetActive(true);
            }
            else
            {
                compassInternal.SetActive(false);
            }
        }

        private bool TryGetClosestObject(out GameObject o)
        {
            if (trackedObjects == null || !trackedObjects.Any() || entity == null)
            {
                o = null;

                return false;
            }

            o =  trackedObjects.Aggregate((i1, i2) => SqrDistanceFromEntity(i1) < SqrDistanceFromEntity(i2) ? i1 : i2);
            
            return o != null;
        }

        private float SqrDistanceFromEntity(GameObject o)
        {
            if (o == null || entity == null) return 9999f;

            return (o.transform.position - entity.transform.position).sqrMagnitude;
        }
    }
}
