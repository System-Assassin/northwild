using UnityEngine;

namespace Northwild
{
    public sealed class Gatherable : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemId item;
        [SerializeField] private int amount = 1;

        public string InteractionPrompt
        {
            get { return "Gather " + ItemCatalog.DisplayName(item).ToLowerInvariant(); }
        }

        public void Initialise(ItemId itemId, int quantity)
        {
            item = itemId;
            amount = Mathf.Max(1, quantity);
        }

        public void Interact(PlayerInventory inventory)
        {
            if (inventory != null && inventory.Add(item, amount))
                Destroy(gameObject);
        }
    }

    public sealed class WaterSource : MonoBehaviour, IInteractable
    {
        private float nextCollectionTime;

        public string InteractionPrompt { get { return "Collect untreated lake water"; } }

        public void Interact(PlayerInventory inventory)
        {
            if (Time.time < nextCollectionTime || inventory == null)
                return;

            nextCollectionTime = Time.time + 0.6f;
            if (inventory.Add(ItemId.RawWater, 1))
                NorthwildGame.Instance.Notify("Lake water collected. Boil it before drinking.");
        }

        private void OnTriggerStay(Collider other)
        {
            SurvivalVitals vitals = other.GetComponentInParent<SurvivalVitals>();
            if (vitals != null)
                vitals.ApplyColdWaterExposure(Time.deltaTime * 18f);
        }
    }
}
