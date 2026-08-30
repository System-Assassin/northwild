using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    public static class NorthwildParticleFactory
    {
        private static Texture2D softParticleTexture;

        public static GameObject CreateCampfireEffects(Transform parent)
        {
            GameObject root = new GameObject("Fire Particle Effects");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            ParticleSystem outerFlames = CreateOuterFlames(root.transform);
            ParticleSystem innerFlames = CreateInnerFlames(root.transform);
            ParticleSystem smoke = CreateSmoke(root.transform);
            ParticleSystem embers = CreateEmbers(root.transform);
            root.AddComponent<CampfireParticleController>().Configure(outerFlames, innerFlames, smoke, embers);
            return root;
        }

        public static ParticleSystem CreateRain(Transform parent)
        {
            ParticleSystem system = CreateSystem("Rain", parent);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.65f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.034f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.58f, 0.72f, 0.82f, 0.42f),
                new Color(0.82f, 0.9f, 0.95f, 0.68f));
            main.maxParticles = 2400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 1f, 28f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = -21f;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.07f;
            renderer.lengthScale = 4.5f;
            renderer.material = ParticleMaterial(new Color(0.72f, 0.84f, 0.92f, 0.72f));
            system.Play();
            return system;
        }

        public static ParticleSystem CreateSnow(Transform parent)
        {
            ParticleSystem system = CreateSystem("Snow", parent);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.82f, 0.88f, 0.93f, 0.58f),
                new Color(1f, 1f, 1f, 0.9f));
            main.maxParticles = 1800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 1f, 26f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0.2f;
            velocity.y = -2.25f;
            velocity.z = 0.08f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.55f;
            noise.frequency = 0.32f;
            noise.scrollSpeed = 0.22f;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = ParticleMaterial(Color.white);
            system.Play();
            return system;
        }

        private static ParticleSystem CreateOuterFlames(Transform parent)
        {
            ParticleSystem system = CreateSystem("Outer Orange Flames", parent);
            system.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.88f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.72f, 1.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.68f);
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 68f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.28f;

            Gradient colour = new Gradient();
            colour.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.88f, 0.24f), 0f),
                    new GradientColorKey(new Color(1f, 0.24f, 0.025f), 0.55f),
                    new GradientColorKey(new Color(0.25f, 0.035f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.72f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colourOverLife = system.colorOverLifetime;
            colourOverLife.enabled = true;
            colourOverLife.color = new ParticleSystem.MinMaxGradient(colour);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.45f, 1f, 1f));

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.16f;
            renderer.lengthScale = 1.7f;
            renderer.material = ParticleMaterial(new Color(1f, 0.46f, 0.05f, 1f));
            system.Play();
            return system;
        }

        private static ParticleSystem CreateInnerFlames(Transform parent)
        {
            ParticleSystem system = CreateSystem("Inner Yellow Flames", parent);
            system.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.48f, 1.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 52f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.2f;

            Gradient colour = new Gradient();
            colour.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.72f), 0f),
                    new GradientColorKey(new Color(1f, 0.72f, 0.08f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.22f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.84f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colourOverLife = system.colorOverLifetime;
            colourOverLife.enabled = true;
            colourOverLife.color = new ParticleSystem.MinMaxGradient(colour);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 0.9f));

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 1.35f;
            renderer.material = ParticleMaterial(new Color(1f, 0.82f, 0.2f, 1f));
            system.Play();
            return system;
        }

        private static ParticleSystem CreateSmoke(Transform parent)
        {
            ParticleSystem system = CreateSystem("Smoke", parent);
            system.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 5.5f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = 0.18f;

            Gradient colour = new Gradient();
            colour.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.22f, 0.21f, 0.19f), 0f),
                    new GradientColorKey(new Color(0.48f, 0.5f, 0.5f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.42f, 0f),
                    new GradientAlphaKey(0.22f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colourOverLife = system.colorOverLifetime;
            colourOverLife.enabled = true;
            colourOverLife.color = new ParticleSystem.MinMaxGradient(colour);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.45f, 1f, 1.9f));

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.18f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;

            system.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial(new Color(0.42f, 0.44f, 0.44f, 0.42f));
            system.Play();
            return system;
        }

        private static ParticleSystem CreateEmbers(Transform parent)
        {
            ParticleSystem system = CreateSystem("Embers", parent);
            system.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.3f, 0.01f), new Color(1f, 0.86f, 0.16f));
            main.gravityModifier = -0.035f;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 11f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.22f;

            ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
            colour.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(new Color(1f, 0.12f, 0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colour.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;

            system.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial(new Color(1f, 0.42f, 0.02f, 1f));
            system.Play();
            return system;
        }

        private static ParticleSystem CreateSystem(string name, Transform parent)
        {
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.AddComponent<ParticleSystem>();
        }

        private static Material ParticleMaterial(Color tint)
        {
            Shader shader = Shader.Find("HDRP/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.color = tint;
            material.mainTexture = SoftParticleTexture();
            if (shader.name == "HDRP/Unlit")
            {
                material.SetColor("_UnlitColor", tint);
                material.SetTexture("_UnlitColorMap", SoftParticleTexture());
                material.SetFloat("_SurfaceType", 1f);
                material.SetFloat("_BlendMode", 0f);
                material.SetFloat("_EnableFogOnTransparent", 1f);
                HDMaterial.ValidateMaterial(material);
            }
            return material;
        }

        private static Texture2D SoftParticleTexture()
        {
            if (softParticleTexture != null)
                return softParticleTexture;

            const int size = 32;
            softParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            softParticleTexture.name = "Generated Soft Particle";
            softParticleTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            softParticleTexture.SetPixels(pixels);
            softParticleTexture.Apply(false, false);
            return softParticleTexture;
        }
    }

    public sealed class CampfireParticleController : MonoBehaviour
    {
        private ParticleSystem outerFlames;
        private ParticleSystem innerFlames;
        private ParticleSystem smoke;
        private ParticleSystem embers;

        public void Configure(
            ParticleSystem outer,
            ParticleSystem inner,
            ParticleSystem smokeSystem,
            ParticleSystem emberSystem)
        {
            outerFlames = outer;
            innerFlames = inner;
            smoke = smokeSystem;
            embers = emberSystem;
        }

        private void Update()
        {
            if (NorthwildGame.Instance == null || NorthwildGame.Instance.Climate == null)
                return;

            WorldClimate climate = NorthwildGame.Instance.Climate;
            Vector3 direction = climate.WindDirection;
            float wind = climate.WindMetresPerSecond;
            ApplyWind(outerFlames, direction * Mathf.Min(0.72f, wind * 0.028f));
            ApplyWind(innerFlames, direction * Mathf.Min(0.46f, wind * 0.018f));
            ApplyWind(smoke, direction * wind * 0.14f);
            ApplyWind(embers, direction * wind * 0.1f);
        }

        private static void ApplyWind(ParticleSystem system, Vector3 horizontalVelocity)
        {
            if (system == null)
                return;
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }
    }

    public sealed class WeatherParticles : MonoBehaviour
    {
        private WorldClimate climate;
        private Transform player;
        private Transform effectsRoot;
        private ParticleSystem rain;
        private ParticleSystem snow;

        public void Configure(WorldClimate worldClimate, Transform playerTransform)
        {
            climate = worldClimate;
            player = playerTransform;
            effectsRoot = new GameObject("Local Weather Particles").transform;
            effectsRoot.SetParent(transform);
            rain = NorthwildParticleFactory.CreateRain(effectsRoot);
            snow = NorthwildParticleFactory.CreateSnow(effectsRoot);
        }

        private void LateUpdate()
        {
            if (climate == null || player == null || effectsRoot == null)
                return;

            effectsRoot.position = player.position + Vector3.up * 11f;
            float wind = climate.WindMetresPerSecond;
            Vector3 windDirection = climate.WindDirection;

            ParticleSystem.VelocityOverLifetimeModule rainVelocity = rain.velocityOverLifetime;
            rainVelocity.x = windDirection.x * wind * 0.18f;
            rainVelocity.z = windDirection.z * wind * 0.18f;
            ParticleSystem.VelocityOverLifetimeModule snowVelocity = snow.velocityOverLifetime;
            snowVelocity.x = windDirection.x * wind * 0.12f;
            snowVelocity.z = windDirection.z * wind * 0.12f;

            ParticleSystem.EmissionModule rainEmission = rain.emission;
            rainEmission.rateOverTime = climate.Weather == WeatherType.Rain
                ? 820f * climate.Precipitation
                : 0f;

            ParticleSystem.EmissionModule snowEmission = snow.emission;
            snowEmission.rateOverTime = climate.Weather == WeatherType.Snow
                ? 330f * climate.Precipitation
                : 0f;
        }
    }
}
