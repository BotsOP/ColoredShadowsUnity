using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private bool invertYAxis = false;
    
    // Components
    private CharacterController characterController;
    private Camera playerCamera;
    
    // Movement variables
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isRunning;
    private bool isCrouching;
    
    // Jump variables
    private bool isGrounded;
    private float lastGroundedTime;
    private float jumpBufferCounter;
    
    // Mouse look variables
    private float verticalRotation;
    private Vector2 mouseInput;
    
    // Crouch variables
    private float currentHeight;
    private Vector3 originalCameraPosition;
    
    // Audio variables
    private float footstepTimer;
    
    // Input variables
    private Vector2 movementInput;
    private bool jumpInput;
    private bool runInput;
    private bool crouchInput;
    
    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Initialize values
        currentHeight = 2;
        characterController.height = currentHeight;
        
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleInput();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        
        // Apply movement
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleInput()
    {
        // Movement input
        movementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        // Jump input with buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        jumpInput = jumpBufferCounter > 0f;
        
        // Run and crouch input
        runInput = Input.GetButton("Fire3"); // Left Shift by default
        crouchInput = Input.GetButton("Fire1") || Input.GetKey(KeyCode.LeftControl);
        
        // Mouse input
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        
        // Unlock cursor on Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Lock cursor on click
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Horizontal rotation (Y-axis)
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
        
        // Vertical rotation (X-axis)
        float mouseY = invertYAxis ? mouseInput.y : -mouseInput.y;
        verticalRotation += mouseY * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
    
    private void HandleMovement()
    {
        // Check if grounded
        isGrounded = characterController.isGrounded;
        
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
        
        // Determine movement speed
        float targetSpeed = 0f;
        if (movementInput.magnitude > 0.1f)
        {
            if (isCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else if (runInput && !isCrouching)
            {
                targetSpeed = runSpeed;
                isRunning = true;
            }
            else
            {
                targetSpeed = walkSpeed;
                isRunning = false;
            }
        }
        else
        {
            isRunning = false;
        }
        
        // Calculate movement direction
        Vector3 inputDirection = new Vector3(movementInput.x, 0f, movementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        worldDirection.y = 0f; // Keep movement horizontal
        
        // Apply acceleration/deceleration
        Vector3 targetMovement = worldDirection * targetSpeed;
        float lerpSpeed = (targetMovement.magnitude > currentMovement.magnitude) ? acceleration : deceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, lerpSpeed * Time.deltaTime);
        
        // Apply horizontal movement
        velocity.x = currentMovement.x;
        velocity.z = currentMovement.z;
    }
    
    private void HandleJump()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        // Coyote time check
        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        
        if (jumpInput && canJump && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f; // Reset jump buffer
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
    }
}