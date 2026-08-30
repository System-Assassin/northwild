using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    public enum WeatherType
    {
        Clear,
        Overcast,
        Rain,
        Snow,
        StrongWind
    }

    public sealed class WorldClimate : MonoBehaviour
    {
        [SerializeField] private float gameMinutesPerRealSecond = 1f;
        private Light sun;
        private NorthwildHDRPEnvironment hdrpEnvironment;
        private float weatherHoursRemaining;
        private float windTarget = 4f;

        public int Day { get; private set; } = 1;
        public float TimeOfDay { get; private set; } = 14.25f;
        public WeatherType Weather { get; private set; } = WeatherType.Overcast;
        public float AmbientTemperatureC { get; private set; }
        public float WindMetresPerSecond { get; private set; } = 4f;
        public Vector3 WindDirection { get; private set; } = new Vector3(0.82f, 0f, 0.57f).normalized;
        public float Precipitation { get; private set; }
        public float GameMinutesThisFrame { get; private set; }
        public float GameMinutesPerRealSecond { get { return gameMinutesPerRealSecond; } }

        public string TimeLabel
        {
            get
            {
                int hour = Mathf.FloorToInt(TimeOfDay);
                int minute = Mathf.FloorToInt((TimeOfDay - hour) * 60f);
                return "Day " + Day + "  " + hour.ToString("00") + ":" + minute.ToString("00");
            }
        }

        public void Initialise()
        {
            GameObject sunObject = new GameObject("Low Nordic Sun");
            sunObject.transform.SetParent(transform);
            sun = sunObject.AddComponent<Light>();
            sunObject.AddComponent<HDAdditionalLightData>();
            sun.type = LightType.Directional;
            sun.lightUnit = LightUnit.Lux;
            sun.shadows = LightShadows.Soft;
            // Let the physically based sky create natural sunrise and sunset colour.
            // Tinting the light as well would colour the atmosphere twice.
            sun.color = Color.white;
            sun.useColorTemperature = false;
            RenderSettings.sun = sun;
            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.fogColor = new Color(0.5f, 0.57f, 0.6f);
            RenderSettings.ambientLight = new Color(0.36f, 0.41f, 0.43f);
            weatherHoursRemaining = 3.5f;
            RecalculateClimate();
            UpdateLighting();
            hdrpEnvironment = gameObject.AddComponent<NorthwildHDRPEnvironment>();
            hdrpEnvironment.Configure(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
                CycleWeatherForTesting();

            GameMinutesThisFrame = Time.deltaTime * gameMinutesPerRealSecond;
            TimeOfDay += GameMinutesThisFrame / 60f;
            if (TimeOfDay >= 24f)
            {
                TimeOfDay -= 24f;
                Day++;
            }

            weatherHoursRemaining -= GameMinutesThisFrame / 60f;
            if (weatherHoursRemaining <= 0f)
                ChooseNextWeather();

            WindMetresPerSecond = Mathf.MoveTowards(WindMetresPerSecond, windTarget, Time.deltaTime * 0.3f);
            RecalculateClimate();
            UpdateLighting();
        }

        private void ChooseNextWeather()
        {
            float roll = UnityEngine.Random.value;
            float dailyTemperature = BaseDailyTemperature();
            if (roll < 0.2f)
                Weather = WeatherType.Clear;
            else if (roll < 0.48f)
                Weather = WeatherType.Overcast;
            else if (roll < 0.73f)
                Weather = dailyTemperature <= 1.5f ? WeatherType.Snow : WeatherType.Rain;
            else if (roll < 0.9f)
                Weather = WeatherType.StrongWind;
            else
                Weather = dailyTemperature <= 2f ? WeatherType.Snow : WeatherType.Rain;

            windTarget = Weather == WeatherType.StrongWind
                ? UnityEngine.Random.Range(10f, 17f)
                : UnityEngine.Random.Range(1.5f, 8f);
            float windAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            WindDirection = new Vector3(Mathf.Cos(windAngle), 0f, Mathf.Sin(windAngle));
            weatherHoursRemaining = UnityEngine.Random.Range(2.5f, 6.5f);

            if (NorthwildGame.Instance != null)
                NorthwildGame.Instance.Notify("Weather shifting: " + Weather.ToString().ToLowerInvariant() + ".");
        }

        private void CycleWeatherForTesting()
        {
            int next = ((int)Weather + 1) % Enum.GetValues(typeof(WeatherType)).Length;
            Weather = (WeatherType)next;
            windTarget = Weather == WeatherType.StrongWind ? 14f : 4f;
            weatherHoursRemaining = 4f;
            RecalculateClimate();
            if (NorthwildGame.Instance != null)
                NorthwildGame.Instance.Notify("Prototype weather test: " + Weather.ToString().ToLowerInvariant() + ".");
        }

        private float BaseDailyTemperature()
        {
            float daylightCurve = Mathf.Sin((TimeOfDay - 7f) / 24f * Mathf.PI * 2f);
            return 1.5f + daylightCurve * 5.5f;
        }

        private void RecalculateClimate()
        {
            float weatherOffset = 0f;
            Precipitation = 0f;
            switch (Weather)
            {
                case WeatherType.Clear:
                    weatherOffset = 1f;
                    break;
                case WeatherType.Overcast:
                    weatherOffset = -0.5f;
                    break;
                case WeatherType.Rain:
                    weatherOffset = -2f;
                    Precipitation = 0.72f;
                    break;
                case WeatherType.Snow:
                    weatherOffset = -4.2f;
                    Precipitation = 0.5f;
                    break;
                case WeatherType.StrongWind:
                    weatherOffset = -1.5f;
                    break;
            }
            AmbientTemperatureC = BaseDailyTemperature() + weatherOffset;
        }

        private void UpdateLighting()
        {
            if (sun == null)
                return;

            float sunAngle = (TimeOfDay / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(sunAngle, 24f, 0f);
            float daylight = Mathf.Clamp01(Mathf.Sin((TimeOfDay - 5.2f) / 15.5f * Mathf.PI));
            float cloud = Weather == WeatherType.Clear ? 1f :
                          Weather == WeatherType.Rain ? 0.46f :
                          Weather == WeatherType.Snow ? 0.62f : 0.58f;
            sun.color = Color.white;
            sun.intensity = Mathf.Lerp(0.05f, 82000f * cloud, daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.25f, 1f, daylight);
        }

        public void Restore(int day, float timeOfDay, WeatherType weather)
        {
            Day = Mathf.Max(1, day);
            TimeOfDay = Mathf.Repeat(timeOfDay, 24f);
            Weather = weather;
            windTarget = weather == WeatherType.StrongWind ? 13f : 4f;
            float windAngle = Mathf.Repeat(day * 73f + timeOfDay * 11f, 360f) * Mathf.Deg2Rad;
            WindDirection = new Vector3(Mathf.Cos(windAngle), 0f, Mathf.Sin(windAngle));
            weatherHoursRemaining = 4f;
            RecalculateClimate();
            UpdateLighting();
        }
    }
}
