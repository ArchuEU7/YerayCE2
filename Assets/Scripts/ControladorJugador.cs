using UnityEngine;
using UnityEngine.InputSystem;

namespace AE2
{
    // Lee las acciones del Player Input (asset InputSystem_Actions, mapa Player):
    //   Move   -> moverse
    //   Look   -> mirar
    //   Jump   -> activar o desactivar el vuelo
    //   Elevar -> subir y bajar volando
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ControladorJugador : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float flySpeed = 25f;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float stickLookSpeed = 150f;
        [SerializeField] private float maxPitch = 85f;

        private CharacterController controller;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction flyAction;
        private InputAction elevateAction;

        private float pitch;
        private float verticalVelocity;
        private bool flying;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
            flyAction = playerInput.actions["Jump"];
            elevateAction = playerInput.actions["Elevar"];

            if (cameraPivot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }

            if (cameraPivot == null)
            {
                Debug.LogError("ControladorJugador: el Player no tiene una cámara hija.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursorLocked(true);

            if (flyAction.WasPressedThisFrame())
            {
                flying = !flying;
                verticalVelocity = 0f;
            }

            Look();

            Vector2 move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            if (flying) Fly(move, elevateAction.ReadValue<float>());
            else Walk(move);
        }

        private void Look()
        {
            Vector2 look = lookAction.ReadValue<Vector2>();
            Vector2 delta;

            // El ratón da un desplazamiento en píxeles; el stick, una dirección que se convierte en grados por segundo
            if (playerInput.currentControlScheme == "Gamepad")
            {
                delta = look * stickLookSpeed * Time.deltaTime;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.Locked) return;
                delta = look * mouseSensitivity;
            }

            transform.Rotate(0f, delta.x, 0f);

            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Walk(Vector2 input)
        {
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // Un valor negativo pequeño mantiene al jugador pegado al suelo en las pendientes
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * walkSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void Fly(Vector2 input, float vertical)
        {
            Vector3 direction = cameraPivot.forward * input.y + cameraPivot.right * input.x;
            direction += Vector3.up * vertical;

            controller.Move(Vector3.ClampMagnitude(direction, 1f) * flySpeed * Time.deltaTime);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
