using UnityEngine;

namespace Northwild
{
    [DefaultExecutionOrder(-1000)]
    public sealed class NorthwildGame : MonoBehaviour
    {
        public static NorthwildGame Instance { get; private set; }

        public WorldGenerator World { get; private set; }
        public WorldClimate Climate { get; private set; }
        public GameObject Player { get; private set; }
        public PlayerInventory Inventory { get; private set; }
        public SurvivalVitals Vitals { get; private set; }
        public PlayerInteraction Interaction { get; private set; }
        public bool InventoryOpen { get; private set; }
        public string LatestMessage { get; private set; }
        public float MessageExpiresAt { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Climate = gameObject.AddComponent<WorldClimate>();
            Climate.Initialise();
            World = gameObject.AddComponent<WorldGenerator>();
            World.Generate();
            CreatePlayer();
            gameObject.AddComponent<WeatherParticles>().Configure(Climate, Player.transform);

            PrototypeHUD hud = gameObject.AddComponent<PrototypeHUD>();
            hud.Configure(this);
            gameObject.AddComponent<NorthwildSaveSystem>().Configure(this);
            Notify("Find water, shelter and dry fire material before the weather turns.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                InventoryOpen = !InventoryOpen;
        }

        public void Notify(string message)
        {
            LatestMessage = message;
            MessageExpiresAt = Time.unscaledTime + 5.5f;
        }

        private void CreatePlayer()
        {
            Player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Player.name = "Survivor";
            Player.transform.position = World.PlayerSpawn;
            Player.GetComponent<Renderer>().sharedMaterial = NorthwildVisuals.Material(new Color(0.19f, 0.25f, 0.3f));

            Collider primitiveCollider = Player.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                primitiveCollider.enabled = false;
                Destroy(primitiveCollider);
            }

            CharacterController controller = Player.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.34f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 48f;

            BushcraftPlayerController movement = Player.AddComponent<BushcraftPlayerController>();
            Inventory = Player.AddComponent<PlayerInventory>();
            Vitals = Player.AddComponent<SurvivalVitals>();
            Vitals.Configure(movement, Climate);

            GameObject rigObject = new GameObject("Survivor Camera Rig");
            PlayerCameraRig cameraRig = rigObject.AddComponent<PlayerCameraRig>();
            cameraRig.Configure(Player.transform, Player.GetComponent<Renderer>());

            Interaction = Player.AddComponent<PlayerInteraction>();
            Interaction.Configure(cameraRig, Inventory);
            Player.AddComponent<BushcraftActions>().Configure(Inventory, Vitals);
        }
    }
}
