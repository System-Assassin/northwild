using UnityEngine;

namespace Northwild
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        private PlayerCameraRig cameraRig;
        private PlayerInventory inventory;
        private IInteractable current;

        public string CurrentPrompt { get; private set; }

        public void Configure(PlayerCameraRig rig, PlayerInventory playerInventory)
        {
            cameraRig = rig;
            inventory = playerInventory;
        }

        private void Update()
        {
            current = null;
            CurrentPrompt = string.Empty;
            if (cameraRig == null || cameraRig.PlayerCamera == null)
                return;

            Ray ray = cameraRig.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3.6f, ~0, QueryTriggerInteraction.Collide))
            {
                MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    IInteractable interactable = behaviour as IInteractable;
                    if (interactable == null)
                        continue;
                    current = interactable;
                    CurrentPrompt = interactable.InteractionPrompt;
                    break;
                }
            }

            if (current != null && Input.GetKeyDown(KeyCode.E))
                current.Interact(inventory);
        }
    }

    public sealed class BushcraftActions : MonoBehaviour
    {
        private PlayerInventory inventory;
        private SurvivalVitals vitals;

        public void Configure(PlayerInventory playerInventory, SurvivalVitals survivalVitals)
        {
            inventory = playerInventory;
            vitals = survivalVitals;
        }

        private void Update()
        {
            if (inventory == null || vitals == null || vitals.IsDead)
                return;

            if (Input.GetKeyDown(KeyCode.B)) BuildCampfire();
            if (Input.GetKeyDown(KeyCode.H)) BuildShelter();
            if (Input.GetKeyDown(KeyCode.T)) UseNearbyFire(FireAction.Tinder);
            if (Input.GetKeyDown(KeyCode.K)) UseNearbyFire(FireAction.Kindling);
            if (Input.GetKeyDown(KeyCode.L)) UseNearbyFire(FireAction.Fuel);
            if (Input.GetKeyDown(KeyCode.I)) UseNearbyFire(FireAction.Ignite);
            if (Input.GetKeyDown(KeyCode.P)) UseNearbyFire(FireAction.Boil);
            if (Input.GetKeyDown(KeyCode.R)) DrinkWater();
            if (Input.GetKeyDown(KeyCode.O)) EatCloudberries();
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F7)) CreateTestFire();
#endif
        }

        private Vector3 PlacementPosition()
        {
            Vector3 position = transform.position + transform.forward * 2.4f;
            if (NorthwildGame.Instance != null && NorthwildGame.Instance.World != null)
                position.y = NorthwildGame.Instance.World.HeightAt(position) + 0.08f;
            return position;
        }

        private void BuildCampfire()
        {
            const int stonesNeeded = 6;
            if (!inventory.Has(ItemId.Stone, stonesNeeded))
            {
                NorthwildGame.Instance.Notify("A stable fire ring needs 6 stones.");
                return;
            }

            inventory.Remove(ItemId.Stone, stonesNeeded);
            Campfire.Create(PlacementPosition());
            NorthwildGame.Instance.Notify("Fire ring built. Add tinder, kindling and fuel in that order.");
        }

        private void BuildShelter()
        {
            if (!inventory.Has(ItemId.Stick, 8) || !inventory.Has(ItemId.Log, 3))
            {
                NorthwildGame.Instance.Notify("A lean-to needs 8 sticks and 3 logs.");
                return;
            }

            inventory.Remove(ItemId.Stick, 8);
            inventory.Remove(ItemId.Log, 3);
            Shelter.Create(PlacementPosition(), transform.eulerAngles.y);
            NorthwildGame.Instance.Notify("Lean-to built. Stay behind its roof to reduce wind and rain exposure.");
        }

        private void UseNearbyFire(FireAction action)
        {
            Campfire fire = Campfire.ClosestTo(transform.position, 3.2f);
            if (fire == null)
            {
                NorthwildGame.Instance.Notify("Move closer to a fire ring.");
                return;
            }
            fire.Perform(action, inventory);
        }

        private void DrinkWater()
        {
            if (!inventory.Remove(ItemId.SafeWater, 1))
            {
                NorthwildGame.Instance.Notify("You have no boiled water.");
                return;
            }

            vitals.Drink(28f);
            NorthwildGame.Instance.Notify("You drink one litre of boiled water.");
        }

        private void EatCloudberries()
        {
            if (!inventory.Remove(ItemId.Cloudberry, 1))
            {
                NorthwildGame.Instance.Notify("You have no food ready to eat.");
                return;
            }

            vitals.Eat(42f, 1.5f);
            NorthwildGame.Instance.Notify("You eat a handful of cloudberries.");
        }

#if UNITY_EDITOR
        private void CreateTestFire()
        {
            Campfire.CreateLitForTesting(PlacementPosition());
            NorthwildGame.Instance.Notify("Prototype fire created with particle effects and six hours of fuel.");
        }
#endif
    }
}
