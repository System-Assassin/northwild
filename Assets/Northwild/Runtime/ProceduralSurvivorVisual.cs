using UnityEngine;

namespace Northwild
{
    public sealed class ProceduralSurvivorVisual : MonoBehaviour
    {
        private BushcraftPlayerController movement;
        private Transform modelRoot;
        private Transform torso;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Renderer[] renderers;
        private float gaitPhase;
        private float visualHeight;

        public void Configure(BushcraftPlayerController playerMovement)
        {
            movement = playerMovement;
            BuildModel();
        }

        public void SetVisible(bool visible)
        {
            if (renderers == null)
                return;
            foreach (Renderer visibleRenderer in renderers)
            {
                if (visibleRenderer != null)
                    visibleRenderer.enabled = visible;
            }
        }

        private void BuildModel()
        {
            modelRoot = new GameObject("Articulated Survivor Model").transform;
            modelRoot.SetParent(transform, false);

            Color parka = new Color(0.19f, 0.245f, 0.205f);
            Color parkaDark = new Color(0.115f, 0.15f, 0.13f);
            Color trousers = new Color(0.12f, 0.135f, 0.14f);
            Color leather = new Color(0.13f, 0.085f, 0.055f);
            Color skin = new Color(0.62f, 0.43f, 0.31f);

            GameObject backpack = NorthwildVisuals.Primitive(
                PrimitiveType.Cube,
                "Canvas Backpack",
                modelRoot,
                new Vector3(0f, 0.08f, -0.235f),
                new Vector3(0.42f, 0.56f, 0.2f),
                parkaDark);
            NorthwildVisuals.RemoveCollider(backpack);

            GameObject hood = NorthwildVisuals.Primitive(
                PrimitiveType.Sphere,
                "Weatherproof Parka Hood",
                modelRoot,
                new Vector3(0f, 0.64f, -0.035f),
                new Vector3(0.43f, 0.48f, 0.38f),
                parkaDark);
            NorthwildVisuals.RemoveCollider(hood);

            GameObject torsoObject = NorthwildVisuals.Primitive(
                PrimitiveType.Capsule,
                "Weatherproof Parka Torso",
                modelRoot,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.47f, 0.53f, 0.31f),
                parka);
            torso = torsoObject.transform;
            NorthwildVisuals.RemoveCollider(torsoObject);

            GameObject belt = NorthwildVisuals.Primitive(
                PrimitiveType.Cylinder,
                "Leather Equipment Belt",
                modelRoot,
                new Vector3(0f, -0.22f, 0f),
                new Vector3(0.26f, 0.055f, 0.26f),
                leather);
            NorthwildVisuals.RemoveCollider(belt);

            GameObject head = NorthwildVisuals.Primitive(
                PrimitiveType.Sphere,
                "Survivor Head",
                modelRoot,
                new Vector3(0f, 0.67f, 0.105f),
                new Vector3(0.29f, 0.35f, 0.29f),
                skin);
            NorthwildVisuals.RemoveCollider(head);

            leftArm = CreateLimbPivot(
                "Left Arm",
                new Vector3(-0.34f, 0.34f, 0f),
                new Vector3(0f, -0.31f, 0f),
                new Vector3(0.1f, 0.34f, 0.1f),
                "Weatherproof Parka Sleeve",
                parka,
                true,
                skin);
            rightArm = CreateLimbPivot(
                "Right Arm",
                new Vector3(0.34f, 0.34f, 0f),
                new Vector3(0f, -0.31f, 0f),
                new Vector3(0.1f, 0.34f, 0.1f),
                "Weatherproof Parka Sleeve",
                parka,
                true,
                skin);
            leftLeg = CreateLimbPivot(
                "Left Leg",
                new Vector3(-0.14f, -0.27f, 0f),
                new Vector3(0f, -0.34f, 0f),
                new Vector3(0.115f, 0.37f, 0.115f),
                "Outdoor Trousers",
                trousers,
                false,
                leather);
            rightLeg = CreateLimbPivot(
                "Right Leg",
                new Vector3(0.14f, -0.27f, 0f),
                new Vector3(0f, -0.34f, 0f),
                new Vector3(0.115f, 0.37f, 0.115f),
                "Outdoor Trousers",
                trousers,
                false,
                leather);

            renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        }

        private Transform CreateLimbPivot(
            string name,
            Vector3 pivotPosition,
            Vector3 limbPosition,
            Vector3 limbScale,
            string clothingName,
            Color clothingColour,
            bool hand,
            Color extremityColour)
        {
            Transform pivot = new GameObject(name + " Pivot").transform;
            pivot.SetParent(modelRoot, false);
            pivot.localPosition = pivotPosition;

            GameObject limb = NorthwildVisuals.Primitive(
                PrimitiveType.Cylinder,
                clothingName,
                pivot,
                limbPosition,
                limbScale,
                clothingColour);
            NorthwildVisuals.RemoveCollider(limb);

            Vector3 extremityPosition = hand
                ? new Vector3(0f, -0.67f, 0f)
                : new Vector3(0f, -0.72f, 0.075f);
            Vector3 extremityScale = hand
                ? new Vector3(0.13f, 0.16f, 0.12f)
                : new Vector3(0.16f, 0.16f, 0.29f);
            GameObject extremity = NorthwildVisuals.Primitive(
                hand ? PrimitiveType.Sphere : PrimitiveType.Cube,
                hand ? "Leather Glove" : "Leather Boot",
                pivot,
                extremityPosition,
                extremityScale,
                extremityColour);
            NorthwildVisuals.RemoveCollider(extremity);
            return pivot;
        }

        private void LateUpdate()
        {
            if (movement == null || modelRoot == null)
                return;

            float gaitSpeed = movement.IsSprinting ? 11.5f : 7.2f;
            float gaitAmount = movement.IsMoving ? (movement.IsSprinting ? 38f : 25f) : 0f;
            if (movement.IsMoving)
                gaitPhase += Time.deltaTime * gaitSpeed;
            else
                gaitPhase = Mathf.MoveTowards(gaitPhase, Mathf.Round(gaitPhase / Mathf.PI) * Mathf.PI, Time.deltaTime * 5f);

            float swing = Mathf.Sin(gaitPhase) * gaitAmount;
            leftArm.localRotation = Quaternion.Euler(swing, 0f, -5f);
            rightArm.localRotation = Quaternion.Euler(-swing, 0f, 5f);
            leftLeg.localRotation = Quaternion.Euler(-swing * 0.72f, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(swing * 0.72f, 0f, 0f);

            float crouch = movement.IsCrouching ? -0.24f : 0f;
            float bob = movement.IsMoving ? Mathf.Abs(Mathf.Sin(gaitPhase * 2f)) * 0.025f : 0f;
            visualHeight = Mathf.Lerp(visualHeight, crouch + bob, 12f * Time.deltaTime);
            modelRoot.localPosition = new Vector3(0f, visualHeight, 0f);
            torso.localRotation = Quaternion.Euler(movement.IsCrouching ? 12f : 0f, 0f, 0f);
        }
    }
}
