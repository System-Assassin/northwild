using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BushcraftPlayerController : MonoBehaviour
    {
        private CharacterController controller;
        private float verticalVelocity;
        private bool crouching;

        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsCrouching { get { return crouching; } }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (NorthwildGame.Instance == null || NorthwildGame.Instance.Vitals == null || NorthwildGame.Instance.Vitals.IsDead)
                return;
            if (NorthwildGame.Instance.InventoryOpen)
            {
                IsMoving = false;
                IsSprinting = false;
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
            IsMoving = input.sqrMagnitude > 0.01f;

            crouching = Input.GetKey(KeyCode.LeftControl);
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && vertical > 0.1f && !crouching &&
                          NorthwildGame.Instance.Vitals.Fatigue > 4f;

            float speed = crouching ? 2.15f : IsSprinting ? 6.1f : 3.7f;
            Vector3 move = transform.right * input.x + transform.forward * input.z;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

            move.y = verticalVelocity;
            controller.Move(move * speed * Time.deltaTime);

            float targetHeight = crouching ? 1.25f : 1.9f;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, Time.deltaTime * 4f);
        }
    }

    public sealed class PlayerCameraRig : MonoBehaviour
    {
        private Transform target;
        private ProceduralSurvivorVisual playerVisual;
        private Camera playerCamera;
        private float yaw;
        private float pitch = 8f;
        private bool firstPerson = true;
        private bool cursorCaptured = true;

        public Camera PlayerCamera { get { return playerCamera; } }
        public bool FirstPerson { get { return firstPerson; } }

        public void Configure(Transform playerTarget, ProceduralSurvivorVisual visibleModel)
        {
            target = playerTarget;
            playerVisual = visibleModel;
            yaw = target.eulerAngles.y;

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(transform);
            playerCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<HDAdditionalCameraData>();
            playerCamera.nearClipPlane = 0.04f;
            playerCamera.farClipPlane = 400f;
            playerCamera.fieldOfView = 72f;
            cameraObject.AddComponent<AudioListener>();
            SetCursor(true);
        }

        private void Update()
        {
            if (target == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                SetCursor(!cursorCaptured);
            if (!cursorCaptured && Input.GetMouseButtonDown(0))
                SetCursor(true);

            if (cursorCaptured && (NorthwildGame.Instance == null || !NorthwildGame.Instance.InventoryOpen))
            {
                yaw += Input.GetAxis("Mouse X") * 2.4f;
                pitch -= Input.GetAxis("Mouse Y") * 2.1f;
                pitch = Mathf.Clamp(pitch, -78f, 82f);
                target.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                firstPerson = !firstPerson;
                if (NorthwildGame.Instance != null)
                    NorthwildGame.Instance.Notify(firstPerson ? "First-person view." : "Third-person view.");
            }
        }

        private void LateUpdate()
        {
            if (target == null || playerCamera == null)
                return;

            float crouchOffset = Input.GetKey(KeyCode.LeftControl) ? -0.48f : 0f;
            Vector3 eye = target.position + Vector3.up * (0.72f + crouchOffset);
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);

            if (firstPerson)
            {
                playerCamera.transform.position = eye;
                playerCamera.transform.rotation = viewRotation;
            }
            else
            {
                Vector3 focus = eye + Vector3.up * 0.15f;
                Vector3 desired = focus - viewRotation * Vector3.forward * 4.2f;
                RaycastHit hit;
                if (Physics.Linecast(focus, desired, out hit, ~0, QueryTriggerInteraction.Ignore))
                    desired = hit.point + hit.normal * 0.18f;

                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desired, 14f * Time.deltaTime);
                playerCamera.transform.rotation = viewRotation;
            }

            if (playerVisual != null)
                playerVisual.SetVisible(!firstPerson);
        }

        private void SetCursor(bool captured)
        {
            cursorCaptured = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
