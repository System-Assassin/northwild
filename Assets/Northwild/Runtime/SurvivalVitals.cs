using System;
using UnityEngine;

namespace Northwild
{
    [Serializable]
    public class VitalsData
    {
        public float health;
        public float coreTemperature;
        public float hydration;
        public float calories;
        public float fatigue;
        public float wetness;
    }

    public sealed class SurvivalVitals : MonoBehaviour
    {
        private BushcraftPlayerController movement;
        private WorldClimate climate;
        private bool deathReported;

        public float Health { get; private set; } = 100f;
        public float CoreTemperatureC { get; private set; } = 37f;
        public float Hydration { get; private set; } = 86f;
        public float Calories { get; private set; } = 2400f;
        public float Fatigue { get; private set; } = 88f;
        public float Wetness { get; private set; } = 7f;
        public float EffectiveTemperatureC { get; private set; }
        public bool IsDead { get { return Health <= 0f; } }

        public void Configure(BushcraftPlayerController playerMovement, WorldClimate worldClimate)
        {
            movement = playerMovement;
            climate = worldClimate;
        }

        private void Update()
        {
            if (climate == null || IsDead)
            {
                ReportDeathIfNeeded();
                return;
            }

            float minutes = climate.GameMinutesThisFrame;
            if (minutes <= 0f)
                return;

            ShelterProtection shelter = Shelter.ProtectionAt(transform.position);
            float fireHeat = Campfire.HeatAt(transform.position);
            bool sprinting = movement != null && movement.IsSprinting;
            bool moving = movement != null && movement.IsMoving;

            if (climate.Precipitation > 0.01f)
                Wetness += climate.Precipitation * (1f - shelter.RainBlock) * 1.05f * minutes;

            float drying = 0.025f + fireHeat * 0.055f;
            Wetness -= drying * minutes;
            Wetness = Mathf.Clamp(Wetness, 0f, 100f);

            float exertionWarmth = sprinting ? 4.5f : moving ? 1.6f : 0f;
            float windChill = climate.WindMetresPerSecond * 0.52f * (1f - shelter.WindBlock);
            float wetPenalty = Wetness * 0.085f;
            const float basicClothingInsulation = 8.2f;
            EffectiveTemperatureC = climate.AmbientTemperatureC + basicClothingInsulation + fireHeat +
                                    exertionWarmth - windChill - wetPenalty;

            if (EffectiveTemperatureC < 5f)
                CoreTemperatureC -= (5f - EffectiveTemperatureC) * 0.00145f * minutes;
            else if (EffectiveTemperatureC > 24f)
                CoreTemperatureC += (EffectiveTemperatureC - 24f) * 0.00085f * minutes;
            else
                CoreTemperatureC = Mathf.MoveTowards(CoreTemperatureC, 36.9f, 0.0025f * minutes);

            float hydrationUse = 0.038f + (sprinting ? 0.045f : moving ? 0.012f : 0f);
            Hydration -= hydrationUse * minutes;
            Calories -= (1.05f + (sprinting ? 1.35f : moving ? 0.42f : 0f)) * minutes;
            Fatigue -= (0.032f + (sprinting ? 0.048f : moving ? 0.012f : 0f)) * minutes;

            if (CoreTemperatureC < 35f)
                Health -= (35f - CoreTemperatureC) * 0.22f * minutes;
            if (CoreTemperatureC > 39.5f)
                Health -= (CoreTemperatureC - 39.5f) * 0.18f * minutes;
            if (Hydration <= 0f)
                Health -= 0.38f * minutes;
            if (Calories <= 0f)
                Health -= 0.08f * minutes;
            if (Fatigue <= 0f)
                Health -= 0.11f * minutes;

            CoreTemperatureC = Mathf.Clamp(CoreTemperatureC, 29f, 41.5f);
            Hydration = Mathf.Clamp(Hydration, 0f, 100f);
            Calories = Mathf.Clamp(Calories, 0f, 3200f);
            Fatigue = Mathf.Clamp(Fatigue, 0f, 100f);
            Health = Mathf.Clamp(Health, 0f, 100f);
            ReportDeathIfNeeded();
        }

        public void Drink(float hydration)
        {
            Hydration = Mathf.Clamp(Hydration + hydration, 0f, 100f);
        }

        public void Eat(float calories, float hydration)
        {
            Calories = Mathf.Clamp(Calories + calories, 0f, 3200f);
            Hydration = Mathf.Clamp(Hydration + hydration, 0f, 100f);
        }

        public void ApplyColdWaterExposure(float amount)
        {
            Wetness = Mathf.Clamp(Wetness + amount, 0f, 100f);
            CoreTemperatureC = Mathf.Max(29f, CoreTemperatureC - amount * 0.0025f);
        }

        public VitalsData Capture()
        {
            return new VitalsData
            {
                health = Health,
                coreTemperature = CoreTemperatureC,
                hydration = Hydration,
                calories = Calories,
                fatigue = Fatigue,
                wetness = Wetness
            };
        }

        public void Restore(VitalsData data)
        {
            if (data == null)
                return;
            Health = data.health;
            CoreTemperatureC = data.coreTemperature;
            Hydration = data.hydration;
            Calories = data.calories;
            Fatigue = data.fatigue;
            Wetness = data.wetness;
            deathReported = false;
        }

        private void ReportDeathIfNeeded()
        {
            if (!IsDead || deathReported)
                return;
            deathReported = true;
            if (NorthwildGame.Instance != null)
                NorthwildGame.Instance.Notify("You have died from exposure. Load the last save with F9.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
