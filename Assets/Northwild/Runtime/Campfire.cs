using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    public enum FireAction
    {
        Tinder,
        Kindling,
        Fuel,
        Ignite,
        Boil
    }

    [Serializable]
    public class CampfireData
    {
        public Vector3 position;
        public int tinderUnits;
        public int kindlingUnits;
        public float fuelMinutes;
        public float moisture;
        public bool lit;
        public float boilingMinutes;
    }

    public sealed class Campfire : MonoBehaviour
    {
        private static readonly List<Campfire> active = new List<Campfire>();
        private int tinderUnits;
        private int kindlingUnits;
        private float fuelMinutes;
        private float moisture = 0.08f;
        private bool lit;
        private float boilingMinutes;
        private PlayerInventory boilingOwner;
        private GameObject fireEffects;
        private GameObject flame;
        private Light fireLight;

        public bool IsLit { get { return lit; } }
        public float Moisture { get { return moisture; } }
        public float FuelMinutes { get { return fuelMinutes; } }
        public bool IsBoiling { get { return boilingMinutes > 0f; } }

        public string Status
        {
            get
            {
                string state = lit ? "Burning" : "Unlit";
                string boiling = IsBoiling ? " | water boiling: " + Mathf.CeilToInt(boilingMinutes) + " min" : string.Empty;
                return state + " | tinder " + tinderUnits + " | kindling " + kindlingUnits +
                       " | fuel " + Mathf.CeilToInt(fuelMinutes) + " min | moisture " +
                       Mathf.RoundToInt(moisture * 100f) + "%" + boiling;
            }
        }

        public static Campfire Create(Vector3 position)
        {
            GameObject root = new GameObject("Stone Fire Ring");
            root.transform.position = position;
            Campfire fire = root.AddComponent<Campfire>();
            fire.BuildVisuals();
            return fire;
        }

        public static Campfire CreateLitForTesting(Vector3 position)
        {
            Campfire fire = Create(position);
            fire.kindlingUnits = 2;
            fire.fuelMinutes = 360f;
            fire.moisture = 0.05f;
            fire.lit = true;
            fire.SetFlame(true);
            return fire;
        }

        public static Campfire ClosestTo(Vector3 position, float range)
        {
            Campfire closest = null;
            float closestSqr = range * range;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Campfire candidate = active[i];
                if (candidate == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                float sqr = (candidate.transform.position - position).sqrMagnitude;
                if (sqr <= closestSqr)
                {
                    closestSqr = sqr;
                    closest = candidate;
                }
            }
            return closest;
        }

        public static float HeatAt(Vector3 position)
        {
            float heat = 0f;
            foreach (Campfire fire in active)
            {
                if (fire == null || !fire.lit)
                    continue;
                float distance = Vector3.Distance(position, fire.transform.position);
                if (distance < 5.2f)
                    heat += Mathf.Lerp(14f, 0f, distance / 5.2f);
            }
            return heat;
        }

        public static List<CampfireData> CaptureAll()
        {
            List<CampfireData> result = new List<CampfireData>();
            foreach (Campfire fire in active)
            {
                if (fire != null)
                    result.Add(fire.Capture());
            }
            return result;
        }

        public static void RestoreAll(List<CampfireData> data)
        {
            Campfire[] existing = FindObjectsByType<Campfire>();
            foreach (Campfire fire in existing)
                Destroy(fire.gameObject);
            active.Clear();

            if (data == null)
                return;
            foreach (CampfireData saved in data)
            {
                Campfire fire = Create(saved.position);
                fire.Restore(saved);
            }
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

        public void Perform(FireAction action, PlayerInventory inventory)
        {
            switch (action)
            {
                case FireAction.Tinder: AddTinder(inventory); break;
                case FireAction.Kindling: AddKindling(inventory); break;
                case FireAction.Fuel: AddFuel(inventory); break;
                case FireAction.Ignite: Ignite(); break;
                case FireAction.Boil: StartBoiling(inventory); break;
            }
        }

        private void AddTinder(PlayerInventory inventory)
        {
            ItemId used;
            if (inventory.Remove(ItemId.BirchBark, 1))
                used = ItemId.BirchBark;
            else if (inventory.Remove(ItemId.DryGrass, 1))
                used = ItemId.DryGrass;
            else
            {
                NorthwildGame.Instance.Notify("You need dry birch bark or grass for tinder.");
                return;
            }

            tinderUnits++;
            moisture = Mathf.Max(0f, moisture - (used == ItemId.BirchBark ? 0.08f : 0.04f));
            NorthwildGame.Instance.Notify("Tinder placed in the fire ring.");
        }

        private void AddKindling(PlayerInventory inventory)
        {
            if (!inventory.Remove(ItemId.Twig, 1))
            {
                NorthwildGame.Instance.Notify("You need thin, dry twigs for kindling.");
                return;
            }
            kindlingUnits++;
            NorthwildGame.Instance.Notify("One bundle of twig kindling added. Two bundles are needed.");
        }

        private void AddFuel(PlayerInventory inventory)
        {
            if (inventory.Remove(ItemId.Log, 1))
            {
                fuelMinutes += 245f;
                NorthwildGame.Instance.Notify("A split log is added as long-burning fuel.");
                return;
            }
            if (inventory.Remove(ItemId.Stick, 1))
            {
                fuelMinutes += 72f;
                NorthwildGame.Instance.Notify("A stick is added as fuel.");
                return;
            }
            NorthwildGame.Instance.Notify("You need a stick or split log for fuel.");
        }

        private void Ignite()
        {
            if (lit)
            {
                NorthwildGame.Instance.Notify("The fire is already burning.");
                return;
            }
            if (tinderUnits < 1 || kindlingUnits < 2 || fuelMinutes < 1f)
            {
                NorthwildGame.Instance.Notify("Prepare tinder, at least 2 kindling bundles and fuel before ignition.");
                return;
            }
            if (moisture > 0.72f)
            {
                tinderUnits = Mathf.Max(0, tinderUnits - 1);
                NorthwildGame.Instance.Notify("The wet tinder smoulders and fails. Find dry material or shelter the fire.");
                return;
            }

            float chance = Mathf.Clamp01(0.97f - moisture * 0.65f);
            if (UnityEngine.Random.value > chance)
            {
                tinderUnits = Mathf.Max(0, tinderUnits - 1);
                NorthwildGame.Instance.Notify("The ignition attempt fails and consumes the tinder.");
                return;
            }

            lit = true;
            tinderUnits = Mathf.Max(0, tinderUnits - 1);
            kindlingUnits = Mathf.Max(0, kindlingUnits - 2);
            SetFlame(true);
            NorthwildGame.Instance.Notify("The kindling catches. Feed and protect the fire.");
        }

        private void StartBoiling(PlayerInventory inventory)
        {
            if (!lit)
            {
                NorthwildGame.Instance.Notify("Water can only be boiled over a lit fire.");
                return;
            }
            if (IsBoiling)
            {
                NorthwildGame.Instance.Notify("One container is already boiling.");
                return;
            }
            if (!inventory.Remove(ItemId.RawWater, 1))
            {
                NorthwildGame.Instance.Notify("You have no untreated water to boil.");
                return;
            }

            boilingOwner = inventory;
            boilingMinutes = 14f;
            NorthwildGame.Instance.Notify("Water placed over the fire. Keep it burning for 14 game minutes.");
        }

        private void Update()
        {
            if (NorthwildGame.Instance == null || NorthwildGame.Instance.Climate == null)
                return;

            WorldClimate climate = NorthwildGame.Instance.Climate;
            float gameMinutes = climate.GameMinutesThisFrame;
            ShelterProtection protection = Shelter.ProtectionAt(transform.position);

            if (climate.Precipitation > 0.01f)
                moisture += climate.Precipitation * (1f - protection.RainBlock) * 0.012f * gameMinutes;

            if (lit)
            {
                moisture -= 0.018f * gameMinutes;
                float burnRate = 1f + climate.WindMetresPerSecond * (1f - protection.WindBlock) * 0.035f;
                fuelMinutes -= gameMinutes * burnRate;

                if (boilingMinutes > 0f)
                {
                    boilingMinutes -= gameMinutes;
                    if (boilingMinutes <= 0f && boilingOwner != null)
                    {
                        boilingMinutes = 0f;
                        boilingOwner.Add(ItemId.SafeWater, 1, false);
                        NorthwildGame.Instance.Notify("The water has maintained a boil and is now safe to drink.");
                    }
                }

                if (fuelMinutes <= 0f || moisture >= 0.96f)
                {
                    fuelMinutes = Mathf.Max(0f, fuelMinutes);
                    lit = false;
                    SetFlame(false);
                    NorthwildGame.Instance.Notify(moisture >= 0.96f ? "Rain extinguishes the saturated fire." : "The fire runs out of fuel.");
                }
            }

            moisture = Mathf.Clamp01(moisture);
            if (fireEffects != null && lit)
            {
                if (fireLight != null)
                    fireLight.intensity = 720f + Mathf.Sin(Time.time * 11f) * 65f + UnityEngine.Random.value * 90f;
            }
        }

        private void BuildVisuals()
        {
            for (int i = 0; i < 11; i++)
            {
                float angle = i / 11f * Mathf.PI * 2f;
                float irregularity = 0.9f + Mathf.Sin(i * 2.17f) * 0.08f;
                Color stoneColour = Color.Lerp(
                    new Color(0.22f, 0.23f, 0.23f),
                    new Color(0.34f, 0.33f, 0.3f),
                    Mathf.Repeat(i * 0.37f, 1f));
                GameObject stone = NorthwildVisuals.Primitive(
                    PrimitiveType.Sphere, "Ring Stone", transform,
                    new Vector3(Mathf.Cos(angle) * 0.66f, 0.09f, Mathf.Sin(angle) * 0.66f),
                    new Vector3(0.3f * irregularity, 0.17f, 0.24f / irregularity), stoneColour);
                stone.transform.localRotation = Quaternion.Euler(i * 13f, i * 31f, i * 7f);
                NorthwildVisuals.RemoveCollider(stone);
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f;
                float radius = 0.16f + (i % 3) * 0.075f;
                GameObject coal = NorthwildVisuals.Primitive(
                    PrimitiveType.Sphere, "Glowing Coal", transform,
                    new Vector3(Mathf.Cos(angle) * radius, 0.1f + (i % 2) * 0.035f, Mathf.Sin(angle) * radius),
                    new Vector3(0.16f, 0.075f, 0.13f), new Color(0.12f, 0.035f, 0.018f));
                coal.GetComponent<Renderer>().sharedMaterial = NorthwildVisuals.EmissiveMaterial(
                    new Color(0.1f, 0.025f, 0.012f), new Color(1f, 0.12f, 0.015f), 1.8f);
                NorthwildVisuals.RemoveCollider(coal);
            }

            for (int layer = 0; layer < 2; layer++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 position = layer == 0
                        ? new Vector3(side * 0.18f, 0.22f, 0f)
                        : new Vector3(0f, 0.38f, side * 0.18f);
                    float yaw = layer == 0 ? 42f : -48f;
                    GameObject log = NorthwildVisuals.Primitive(
                        PrimitiveType.Cylinder, "Charred Firewood", transform, position,
                        new Vector3(0.11f, 0.53f, 0.11f), new Color(0.16f, 0.075f, 0.028f));
                    log.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
                    NorthwildVisuals.RemoveCollider(log);
                }
            }

            fireEffects = NorthwildParticleFactory.CreateCampfireEffects(transform);
            flame = new GameObject("Flickering Fire Light");
            flame.transform.SetParent(fireEffects.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            fireLight = flame.AddComponent<Light>();
            flame.AddComponent<HDAdditionalLightData>();
            fireLight.type = LightType.Point;
            fireLight.lightUnit = LightUnit.Lumen;
            fireLight.color = new Color(1f, 0.35f, 0.075f);
            fireLight.range = 7.5f;
            fireLight.shadows = LightShadows.Soft;
            fireLight.shadowStrength = 0.42f;
            fireLight.intensity = 760f;
            SetFlame(false);
        }

        private void SetFlame(bool visible)
        {
            if (fireEffects != null)
                fireEffects.SetActive(visible);
        }

        private CampfireData Capture()
        {
            return new CampfireData
            {
                position = transform.position,
                tinderUnits = tinderUnits,
                kindlingUnits = kindlingUnits,
                fuelMinutes = fuelMinutes,
                moisture = moisture,
                lit = lit,
                boilingMinutes = boilingMinutes
            };
        }

        private void Restore(CampfireData data)
        {
            tinderUnits = data.tinderUnits;
            kindlingUnits = data.kindlingUnits;
            fuelMinutes = data.fuelMinutes;
            moisture = data.moisture;
            lit = data.lit;
            boilingMinutes = data.boilingMinutes;
            SetFlame(lit);
        }
    }
}
