using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class TPMovement : MonoBehaviour
{  
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private RawImage interactibleUI;
    [SerializeField] private CanvasGroup pauseMenuUI;
    InputMaster controls;

    [SerializeField] private float speed = 6f;
    [SerializeField] private float sprintSpeed = 15f;
    [SerializeField] private float crouchSpeed = 3f;
    private float superSpeed = 100f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float airResistance = 1f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float airControlMultiplier = 1f; //remember to reset
    [SerializeField] private float jumpQueueTimer = 0.1f;
    [SerializeField] private float coyoteTimeLength = 0.1f;
    [SerializeField] private float groundDistance = 1.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float climbDistance = 0.85f;
    [SerializeField] private LayerMask climbMask;
    [SerializeField] private float climbingSpeed = 4f;
    [SerializeField] private float climbJumpMultiplier = 0.5f;
    [SerializeField] private float ledgeDistance = 0.1f;
    [SerializeField] private float mantleDuration = 0.3f;
    [SerializeField] private float turnCornerDuration = 0.3f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float interactibleDetectionRadius = 1.8f;
    [SerializeField] private LayerMask interactibleMask;
    private float turnSmoothVelocity;

    private bool paused;
    private Vector3 velocity;
    private Vector3 climbJumpVelocity;
    private Vector2 movement;
    private bool hasControl;
    private bool grounded;
    private bool jumping;
    private float jumpQueue;
    private bool crouched;
    private bool sprinting;
    private bool superSprinting;
    private float coyoteTime;
    private bool climbing;
    private bool canInteract;
    private Collider currentInteractible;

    void Awake() {
        controls = new InputMaster();
        controls.Enable();
        controls.Player.Jump.performed += _ => jumping = true;
        controls.Player.Crouch.performed += _ => OnCrouch();
        controls.Player.Crouch.canceled += _ => OnReleaseCrouch();
        controls.Player.Sprint.performed += _ => sprinting = true;
        controls.Player.Sprint.canceled += _ => sprinting = false;
        controls.Player.DEV_Supersprint.performed += _ => superSprinting = true;
        controls.Player.DEV_Supersprint.canceled += _ => superSprinting = false;
        controls.Player.Interact.performed += _ => OnInteractPressed();
        controls.Player.Pause.performed += _ => OnPause();
    }

    void Start() {
        paused = false;
        hasControl = true;
        pauseMenuUI.alpha = 0.0f;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
    }

    void Update()
    {
        movement = controls.Player.Movement.ReadValue<Vector2>();
        
        RaycastHit hit;
        Collider[] climbCheckCollisions;
        if (Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, climbDistance + 0.05f, climbMask)) {
            climbing = true;
            climbCheckCollisions = new Collider[] {hit.collider};
        } else {
            climbCheckCollisions = Physics.OverlapSphere(transform.position + transform.forward * (climbDistance/2 + 0.05f), 0.6f, climbMask);
            climbing = climbCheckCollisions.Length > 0;
        }

        Collider[] checkInteractibleRadius = Physics.OverlapSphere(transform.position + transform.forward * interactibleDetectionRadius, interactibleDetectionRadius);
        List<Collider> interactibles = new List<Collider>();
        foreach(Collider c in checkInteractibleRadius) {
            if (c.gameObject.tag == "interactible") {
                interactibles.Add(c);
            }
        }
        if (interactibles.Count > 0) {
            canInteract = true;
            currentInteractible = interactibles[0];
            foreach (Collider c in interactibles) {
                if (c.gameObject.tag == "interactible" && Vector3.Distance(c.gameObject.transform.position, transform.position) < Vector3.Distance(currentInteractible.gameObject.transform.position, transform.position)) {
                    currentInteractible = c;
                }
            }
        } else {
            canInteract = false;
        }
        interactibleUI.enabled = canInteract;
    
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance + controller.skinWidth, groundMask);
        
        if (climbing && grounded && movement.y < 0) climbing = false;
        if (hasControl) 
        {   
            if (climbing) 
            {
                Collider currentlyClimbing = climbCheckCollisions[0];
                
                velocity = Vector3.zero;
                Vector3 closestPoint = Physics.ClosestPoint(transform.position, currentlyClimbing, currentlyClimbing.transform.position, currentlyClimbing.transform.rotation);

                switch (currentlyClimbing.gameObject.tag) 
                {
                    case "sheer_surface": 
                    {
                        transform.LookAt(closestPoint);
                        controller.enabled = false; transform.position = closestPoint + -transform.forward * climbDistance; controller.enabled = true;
                        
                        Vector3 direction = transform.right * movement.x + transform.up * movement.y;

                        if (direction.magnitude > 0.1f) 
                        {
                            if (movement.y > 0 && !Physics.Raycast(transform.position + transform.up * ledgeDistance, transform.forward, climbDistance + 0.05f, climbMask)) {
                                StartCoroutine(Mantle(transform.position, transform.position + transform.up * (ledgeDistance + 1.8f) + transform.forward * (climbDistance + 0.05f), mantleDuration));
                                break;
                            }
                            if (movement.x < 0 && !Physics.Raycast(transform.position + -transform.right * ledgeDistance, transform.forward, climbDistance + 0.05f, climbMask)) {
                                StartCoroutine(TurnCorner(transform.position, transform.position + transform.forward * (ledgeDistance + climbDistance) + -transform.right * (ledgeDistance + climbDistance), turnCornerDuration, transform.position + transform.forward * (ledgeDistance + climbDistance)));
                                break;
                            }
                            if (movement.x > 0 && !Physics.Raycast(transform.position + transform.right * ledgeDistance, transform.forward, climbDistance + 0.05f, climbMask)) {
                                StartCoroutine(TurnCorner(transform.position, transform.position + transform.forward * (ledgeDistance + climbDistance) + transform.right * (ledgeDistance + climbDistance), turnCornerDuration, transform.position + transform.forward * (ledgeDistance + climbDistance)));
                                break;
                            }
                            controller.Move(direction.normalized * climbingSpeed * Time.deltaTime);
                            
                            if (jumping) {
                                float lookingAtWall = Vector3.Dot(transform.forward, cam.transform.forward);

                                if (lookingAtWall < -0.2f && movement.y > 0) {
                                    transform.LookAt(transform.position + cam.transform.forward);
                                    velocity.y = (Mathf.Sqrt(jumpHeight * -2 * gravity)) * climbJumpMultiplier;
                                }

                            }
                        }
                        break;
                    }
                    case "vertical_rope": 
                    {
                        
                        float yOffset = transform.position.y - currentlyClimbing.transform.position.y; 
                        controller.enabled = false; transform.position = currentlyClimbing.transform.position + transform.up * yOffset; controller.enabled = true;
                        
                        float targetAngle = cam.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        
                        Vector3 direction = (transform.forward * movement.y + transform.right * movement.x).normalized;

                        if (direction.magnitude > 0.1f) 
                        {   
                            if (jumping) {
                                transform.LookAt(transform.position + direction);
                                controller.Move(direction * climbDistance);
                                velocity.y = (Mathf.Sqrt(jumpHeight * -2 * gravity)) * climbJumpMultiplier;
                                break;
                            }
                            if (Physics.OverlapSphere(transform.position + (transform.up * ledgeDistance), 0.05f, climbMask).Length > 0 || movement.y >= 0) {
                                controller.Move((transform.up * movement.y) * climbingSpeed * Time.deltaTime);
                            }
                        }

                        break;
                    }
                    case "horizontal_rope": 
                    {
                        Vector3 closestPointOnLine = GetClosestPointOnFiniteLine(transform.position, currentlyClimbing.transform.position + (currentlyClimbing.transform.up * (currentlyClimbing.transform.parent.transform.localScale.y / 2)), currentlyClimbing.transform.position - (currentlyClimbing.transform.up * (currentlyClimbing.transform.parent.transform.localScale.y / 2)));
                        controller.enabled = false; transform.position = closestPointOnLine; controller.enabled = true;

                        float camPointingAlongRope = Vector3.Dot(cam.forward, currentlyClimbing.transform.up);
                        float isCamPointingForward = Mathf.Sign(Vector3.Dot(cam.forward, currentlyClimbing.transform.forward));

                        Vector3 lookDirection;
                        if (Mathf.Abs(camPointingAlongRope) > 0.8f && movement.y != 0) {
                            lookDirection = currentlyClimbing.transform.up * movement.y * Mathf.Sign(camPointingAlongRope);
                        } else {
                            lookDirection = currentlyClimbing.transform.forward * movement.y * isCamPointingForward;
                        }
                        transform.LookAt(transform.position + lookDirection);

                        if (movement.magnitude > 0.1f) 
                        {
                            if (jumping) {
                                if (movement.y != 0 && jumping) {
                                    controller.Move(transform.up * climbDistance);
                                    velocity.y = (Mathf.Sqrt(jumpHeight * -2 * gravity)) * climbJumpMultiplier;
                                    break;
                                }
                            }
                            if (Mathf.Abs(camPointingAlongRope) > 0.8f) {
                                controller.Move(currentlyClimbing.transform.up * movement.y * Mathf.Sign(camPointingAlongRope) * climbingSpeed * Time.deltaTime);
                            } else {
                                controller.Move(-currentlyClimbing.transform.up * movement.x * isCamPointingForward * climbingSpeed * Time.deltaTime);
                            }
                        }
                        break;
                    }
                }
            } 
            else 
            {
                if (jumpQueue > 0) jumpQueue -= Time.deltaTime; 
                if (!grounded && coyoteTime > 0) coyoteTime -= Time.deltaTime; else if (grounded) coyoteTime = coyoteTimeLength;

                if (grounded && velocity.y < 0) {
                    velocity.y = -2f;
                }
                velocity.x = 0; velocity.z = 0;
                
                Vector3 direction = new Vector3(movement.x, 0, movement.y).normalized;

                if (direction.magnitude >= 0.1f) 
                {
                    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);

                    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                    float airMod = grounded ? 1 : airControlMultiplier;

                    float currentSpeed;                
                    if (crouched && grounded) currentSpeed = crouchSpeed;
                    else if (sprinting) currentSpeed = sprintSpeed;
                    else if (superSprinting) currentSpeed = superSpeed;
                    else currentSpeed = speed;

                    velocity += moveDir.normalized * currentSpeed * airMod;
                }

                if (grounded && jumpQueue > 0) {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                    jumpQueue = 0;
                }

                if (jumping) {
                    if (grounded || coyoteTime > 0) {
                        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                    } else {
                        jumpQueue = jumpQueueTimer;
                    }
                }

                velocity.y += gravity * Time.deltaTime;

                controller.Move(velocity * Time.deltaTime);
            }
        }

        jumping = false;
    }

    IEnumerator Mantle(Vector3 startPosition, Vector3 finalPosition, float mantleTime) {
        float startingMantleTime = mantleTime;
        hasControl = false;
        while (mantleTime > 0) {
            mantleTime = Mathf.Clamp(mantleTime - Time.deltaTime, 0f, startingMantleTime);
            controller.enabled = false; transform.position = Vector3.Lerp(startPosition, finalPosition, 1f - MapOntoRange(mantleTime, 0f, startingMantleTime, 0f, 1f)); controller.enabled = true;
            yield return null;
        }
        hasControl = true;
    }

    IEnumerator TurnCorner(Vector3 startPosition, Vector3 finalPosition, float turnCornerTime, Vector3 pointToTurnAround) {
        float startingCornerTime = turnCornerTime;
        hasControl = false;
        while (turnCornerTime > 0) {
            turnCornerTime = Mathf.Clamp(turnCornerTime - Time.deltaTime, 0f, startingCornerTime);
            controller.enabled = false; transform.position = Vector3.Lerp(startPosition, finalPosition, 1f - MapOntoRange(turnCornerTime, 0f, startingCornerTime, 0f, 1f)); controller.enabled = true;
            transform.LookAt(pointToTurnAround);
            yield return null;
        }
        hasControl = true;
    }

    IEnumerator ForceWait(float waitTime) {
        hasControl = false;
        yield return new WaitForSeconds(waitTime);
        hasControl = true;
    }

    Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end) {
        Vector3 dir = line_end - line_start;
        float length = dir.magnitude;
        dir.Normalize();
        float projected_length = Mathf.Clamp(Vector3.Dot(point - line_start, dir), 0f, length);
        return line_start + dir * projected_length;
    }

    float MapOntoRange(float input, float in_min, float in_max, float out_min, float out_max) {
        return out_min + ((out_max - out_min) / (in_max - in_min)) * (input - in_min);
    }

    void OnCrouch() {
        if (!climbing) {
            crouched = true;
            transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            controller.enabled = false; transform.position += Vector3.down * 1.6f; controller.enabled = true;
        }
    }

    void OnReleaseCrouch() {
        if (!climbing) {
            crouched = false;
            controller.enabled = false; transform.position += Vector3.up * 1f; controller.enabled = true;
            transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
        }
    }

    void OnInteractPressed() {
        if (hasControl && canInteract) {
            currentInteractible.gameObject.GetComponent<InteractionHandler>().OnInteraction();
        }
    }

    void OnPause() {
        paused = !paused;
        Time.timeScale = paused ? 0.0f : 1.0f;
        if (paused) {
            pauseMenuUI.alpha = 1.0f;
            pauseMenuUI.interactable = true;
            pauseMenuUI.blocksRaycasts = true;
        } else {
            pauseMenuUI.alpha = 0.0f;
            pauseMenuUI.interactable = false;
            pauseMenuUI.blocksRaycasts = false;
        }
    }

    public void OnResume() {
        paused = false;
        Time.timeScale = 1.0f;
        pauseMenuUI.alpha = 0.0f;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
    }

    public void OnOptions() {
        print("NO DONE YET");
    }

    public void OnExitToMenu() {
        //NO MAIN MENU YET
    }

    public void OnExitToDesktop() {
        Application.Quit();
    }
}
