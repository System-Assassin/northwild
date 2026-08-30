using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    public sealed class NorthwildHDRPEnvironment : MonoBehaviour
    {
        private WorldClimate climate;
        private VolumeProfile profile;
        private VisualEnvironment visualEnvironment;
        private GradientSky sky;
        private VolumetricClouds clouds;
        private Fog fog;
        private Exposure exposure;
        private IndirectLightingController indirectLighting;
        private WeatherType lastWeather = (WeatherType)(-1);
        private float targetFogDistance = 650f;

        public void Configure(WorldClimate worldClimate)
        {
            climate = worldClimate;

            GameObject volumeObject = new GameObject("Northwild HDRP Atmosphere");
            volumeObject.transform.SetParent(transform, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Northwild Runtime Atmosphere";
            volume.sharedProfile = profile;

            visualEnvironment = profile.Add<VisualEnvironment>(true);
            visualEnvironment.skyType.Override(SkySettings.GetUniqueID<GradientSky>());
            visualEnvironment.cloudType.Override(0);
            visualEnvironment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            visualEnvironment.renderingSpace.Override(RenderingSpace.Camera);

            // A controlled daylight gradient is more stable than the physical-sky defaults
            // while this prototype creates its complete lighting rig at runtime.
            sky = profile.Add<GradientSky>(true);
            sky.skyIntensityMode.Override(SkyIntensityMode.Lux);
            sky.desiredLuxValue.Override(16000f);
            sky.gradientDiffusion.Override(1.35f);
            sky.updateMode.Override(EnvironmentUpdateMode.Realtime);
            sky.updatePeriod.Override(0.5f);

            clouds = profile.Add<VolumetricClouds>(true);
            clouds.enable.Override(true);
            clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);
            clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Quality);
            clouds.shadows.Override(true);
            clouds.shadowResolution.Override(VolumetricClouds.CloudShadowResolution.High512);
            clouds.shadowDistance.Override(6000f);
            clouds.shadowOpacity.Override(0.45f);
            clouds.temporalAccumulationFactor.Override(0.93f);
            clouds.ghostingReduction.Override(true);
            clouds.perceptualBlending.Override(0.35f);

            fog = profile.Add<Fog>(true);
            fog.enabled.Override(true);
            fog.enableVolumetricFog.Override(true);
            fog.colorMode.Override(FogColorMode.SkyColor);
            fog.tint.Override(new Color(0.86f, 0.91f, 0.94f));
            fog.baseHeight.Override(-8f);
            fog.maximumHeight.Override(145f);
            fog.meanFreePath.Override(targetFogDistance);
            fog.maxFogDistance.Override(1350f);
            fog.depthExtent.Override(900f);
            fog.albedo.Override(new Color(0.82f, 0.88f, 0.91f));
            fog.anisotropy.Override(0.35f);

            exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.AutomaticHistogram);
            exposure.meteringMode.Override(MeteringMode.CenterWeighted);
            exposure.compensation.Override(0.15f);
            exposure.limitMin.Override(3f);
            exposure.limitMax.Override(14.2f);
            exposure.histogramPercentages.Override(new Vector2(15f, 95f));
            exposure.adaptationMode.Override(AdaptationMode.Progressive);
            exposure.adaptationSpeedDarkToLight.Override(2.4f);
            exposure.adaptationSpeedLightToDark.Override(1.4f);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments colourAdjustments = profile.Add<ColorAdjustments>(true);
            colourAdjustments.contrast.Override(-4f);
            colourAdjustments.saturation.Override(-7f);

            ScreenSpaceAmbientOcclusion ambientOcclusion = profile.Add<ScreenSpaceAmbientOcclusion>(true);
            ambientOcclusion.intensity.Override(0.72f);
            ambientOcclusion.radius.Override(0.48f);
            ambientOcclusion.directLightingStrength.Override(0.08f);
            ambientOcclusion.temporalAccumulation.Override(true);

            indirectLighting = profile.Add<IndirectLightingController>(true);
            indirectLighting.indirectDiffuseLightingMultiplier.Override(1.25f);
            indirectLighting.reflectionLightingMultiplier.Override(1f);

            ApplyWeather(true);
            UpdateDaylightBalance();
        }

        private void Update()
        {
            if (climate == null || clouds == null)
                return;

            ApplyWeather(false);
            UpdateDaylightBalance();

            float orientation = Mathf.Atan2(climate.WindDirection.z, climate.WindDirection.x) * Mathf.Rad2Deg;
            visualEnvironment.windOrientation.Override(Mathf.Repeat(orientation, 360f));
            visualEnvironment.windSpeed.Override(climate.WindMetresPerSecond * 3.6f);
            fog.meanFreePath.Override(Mathf.MoveTowards(
                fog.meanFreePath.value,
                targetFogDistance,
                Time.deltaTime * 55f));
            fog.maxFogDistance.Override(Mathf.MoveTowards(
                fog.maxFogDistance.value,
                Mathf.Clamp(targetFogDistance * 1.15f, 360f, 1400f),
                Time.deltaTime * 80f));
            fog.depthExtent.Override(Mathf.MoveTowards(
                fog.depthExtent.value,
                Mathf.Clamp(targetFogDistance * 0.78f, 260f, 980f),
                Time.deltaTime * 60f));
        }

        private void ApplyWeather(bool force)
        {
            if (!force && lastWeather == climate.Weather)
                return;

            lastWeather = climate.Weather;
            VolumetricClouds.CloudPresets preset;
            float sunDimmer;
            float ambientDimmer;
            float shadowOpacity;
            Color scatteringTint;

            switch (climate.Weather)
            {
                case WeatherType.Clear:
                    preset = VolumetricClouds.CloudPresets.Sparse;
                    targetFogDistance = 1200f;
                    sunDimmer = 1f;
                    ambientDimmer = 1f;
                    shadowOpacity = 0.32f;
                    scatteringTint = Color.white;
                    break;
                case WeatherType.Rain:
                    preset = VolumetricClouds.CloudPresets.Stormy;
                    targetFogDistance = 330f;
                    sunDimmer = 0.48f;
                    ambientDimmer = 0.62f;
                    shadowOpacity = 0.72f;
                    scatteringTint = new Color(0.78f, 0.83f, 0.9f, 1f);
                    break;
                case WeatherType.Snow:
                    preset = VolumetricClouds.CloudPresets.Overcast;
                    targetFogDistance = 420f;
                    sunDimmer = 0.68f;
                    ambientDimmer = 0.86f;
                    shadowOpacity = 0.48f;
                    scatteringTint = new Color(0.94f, 0.97f, 1f, 1f);
                    break;
                case WeatherType.StrongWind:
                    preset = VolumetricClouds.CloudPresets.Cloudy;
                    targetFogDistance = 720f;
                    sunDimmer = 0.8f;
                    ambientDimmer = 0.84f;
                    shadowOpacity = 0.5f;
                    scatteringTint = new Color(0.86f, 0.9f, 0.96f, 1f);
                    break;
                default:
                    preset = VolumetricClouds.CloudPresets.Overcast;
                    targetFogDistance = 590f;
                    sunDimmer = 0.7f;
                    ambientDimmer = 0.8f;
                    shadowOpacity = 0.4f;
                    scatteringTint = new Color(0.84f, 0.88f, 0.93f, 1f);
                    break;
            }

            clouds.cloudPreset = preset;
            clouds.sunLightDimmer.Override(sunDimmer);
            clouds.ambientLightProbeDimmer.Override(ambientDimmer);
            clouds.shadowOpacity.Override(shadowOpacity);
            clouds.scatteringTint.Override(scatteringTint);
        }

        private void UpdateDaylightBalance()
        {
            if (sky == null || exposure == null || indirectLighting == null)
                return;

            float solarHeight = Mathf.Clamp01(Mathf.Sin((climate.TimeOfDay - 6f) / 12f * Mathf.PI));
            float daylight = Mathf.SmoothStep(0f, 1f, solarHeight);
            float twilightWarmth = Mathf.Clamp01((0.42f - solarHeight) / 0.34f) *
                                    Mathf.Clamp01(solarHeight * 6f);

            Color dayTop;
            Color dayMiddle;
            Color dayBottom;
            float daySkyLux;
            float diffuseMultiplier;

            switch (climate.Weather)
            {
                case WeatherType.Clear:
                    dayTop = new Color(0.045f, 0.15f, 0.36f);
                    dayMiddle = new Color(0.3f, 0.55f, 0.78f);
                    dayBottom = new Color(0.66f, 0.74f, 0.8f);
                    daySkyLux = 18000f;
                    diffuseMultiplier = 1.2f;
                    break;
                case WeatherType.Rain:
                    dayTop = new Color(0.09f, 0.12f, 0.15f);
                    dayMiddle = new Color(0.23f, 0.28f, 0.33f);
                    dayBottom = new Color(0.41f, 0.45f, 0.47f);
                    daySkyLux = 8500f;
                    diffuseMultiplier = 1.05f;
                    break;
                case WeatherType.Snow:
                    dayTop = new Color(0.2f, 0.27f, 0.34f);
                    dayMiddle = new Color(0.48f, 0.57f, 0.65f);
                    dayBottom = new Color(0.72f, 0.76f, 0.78f);
                    daySkyLux = 14500f;
                    diffuseMultiplier = 1.22f;
                    break;
                case WeatherType.StrongWind:
                    dayTop = new Color(0.1f, 0.18f, 0.27f);
                    dayMiddle = new Color(0.32f, 0.43f, 0.53f);
                    dayBottom = new Color(0.52f, 0.57f, 0.6f);
                    daySkyLux = 11000f;
                    diffuseMultiplier = 1.1f;
                    break;
                default:
                    dayTop = new Color(0.13f, 0.17f, 0.21f);
                    dayMiddle = new Color(0.35f, 0.4f, 0.44f);
                    dayBottom = new Color(0.54f, 0.57f, 0.58f);
                    daySkyLux = 10500f;
                    diffuseMultiplier = 1.12f;
                    break;
            }

            Color nightTop = new Color(0.003f, 0.007f, 0.025f);
            Color nightMiddle = new Color(0.015f, 0.025f, 0.055f);
            Color nightBottom = new Color(0.035f, 0.04f, 0.055f);
            Color warmHorizon = new Color(0.72f, 0.33f, 0.11f);

            sky.top.Override(Color.Lerp(nightTop, dayTop, daylight));
            Color horizon = Color.Lerp(nightMiddle, dayMiddle, daylight);
            sky.middle.Override(Color.Lerp(horizon, warmHorizon, twilightWarmth * 0.7f));
            sky.bottom.Override(Color.Lerp(nightBottom, dayBottom, daylight));
            sky.desiredLuxValue.Override(Mathf.Lerp(0.2f, daySkyLux, daylight));

            float daylightExposure = Mathf.Pow(daylight, 0.38f);
            exposure.limitMin.Override(Mathf.Lerp(2.5f, 9.5f, daylightExposure));
            exposure.limitMax.Override(Mathf.Lerp(7f, 14.2f, daylightExposure));
            exposure.compensation.Override(Mathf.Lerp(0.45f, 0.1f, daylight));
            indirectLighting.indirectDiffuseLightingMultiplier.Override(
                Mathf.Lerp(0.75f, diffuseMultiplier, daylight));
        }

        private void OnDestroy()
        {
            if (profile != null)
                Destroy(profile);
        }
    }
}
