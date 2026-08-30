using System;
using System.Collections.Generic;
using UnityEngine;

namespace Northwild
{
    public struct ShelterProtection
    {
        public float RainBlock;
        public float WindBlock;

        public ShelterProtection(float rainBlock, float windBlock)
        {
            RainBlock = rainBlock;
            WindBlock = windBlock;
        }
    }

    [Serializable]
    public class ShelterData
    {
        public Vector3 position;
        public float yaw;
    }

    public sealed class Shelter : MonoBehaviour
    {
        private static readonly List<Shelter> active = new List<Shelter>();

        public static Shelter Create(Vector3 position, float yaw)
        {
            GameObject root = new GameObject("Lean-to Shelter");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Shelter shelter = root.AddComponent<Shelter>();
            shelter.BuildVisuals();
            return shelter;
        }

        public static ShelterProtection ProtectionAt(Vector3 position)
        {
            float rain = 0f;
            float wind = 0f;
            foreach (Shelter shelter in active)
            {
                if (shelter == null)
                    continue;

                Vector3 local = shelter.transform.InverseTransformPoint(position);
                bool underRoof = Mathf.Abs(local.x) < 2.25f && local.z > -1.15f && local.z < 2.15f &&
                                 position.y < shelter.transform.position.y + 2.6f;
                if (!underRoof)
                    continue;
                rain = Mathf.Max(rain, 0.88f);
                wind = Mathf.Max(wind, 0.67f);
            }
            return new ShelterProtection(rain, wind);
        }

        public static List<ShelterData> CaptureAll()
        {
            List<ShelterData> result = new List<ShelterData>();
            foreach (Shelter shelter in active)
            {
                if (shelter != null)
                    result.Add(new ShelterData { position = shelter.transform.position, yaw = shelter.transform.eulerAngles.y });
            }
            return result;
        }

        public static void RestoreAll(List<ShelterData> data)
        {
            Shelter[] existing = FindObjectsByType<Shelter>();
            foreach (Shelter shelter in existing)
                Destroy(shelter.gameObject);
            active.Clear();

            if (data == null)
                return;
            foreach (ShelterData saved in data)
                Create(saved.position, saved.yaw);
        }

        private void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        private void BuildVisuals()
        {
            Color wood = new Color(0.29f, 0.16f, 0.07f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject post = NorthwildVisuals.Primitive(
                    PrimitiveType.Cylinder, "Support Post", transform,
                    new Vector3(side * 1.75f, 1.25f, 0.8f),
                    new Vector3(0.13f, 1.25f, 0.13f), wood);
                post.transform.localRotation = Quaternion.Euler(0f, 0f, side * -12f);
                NorthwildVisuals.RemoveCollider(post);
            }

            GameObject ridge = NorthwildVisuals.Primitive(
                PrimitiveType.Cylinder, "Ridge Pole", transform,
                new Vector3(0f, 2.45f, 0.82f), new Vector3(0.14f, 1.95f, 0.14f), wood);
            ridge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            NorthwildVisuals.RemoveCollider(ridge);

            GameObject roof = NorthwildVisuals.Primitive(
                PrimitiveType.Cube, "Layered Bough Roof", transform,
                new Vector3(0f, 1.45f, 0.18f), new Vector3(4.1f, 0.24f, 3.3f),
                new Color(0.12f, 0.27f, 0.1f));
            roof.transform.localRotation = Quaternion.Euler(34f, 0f, 0f);

            GameObject bed = NorthwildVisuals.Primitive(
                PrimitiveType.Cube, "Insulated Bed", transform,
                new Vector3(0f, 0.17f, 0.65f), new Vector3(2.1f, 0.22f, 1.75f),
                new Color(0.21f, 0.32f, 0.13f));
            NorthwildVisuals.RemoveCollider(bed);
        }
    }
}
