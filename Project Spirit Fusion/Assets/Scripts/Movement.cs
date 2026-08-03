using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public float speed = 8f;

    [Header("Jump Stats")]
    public float jumpForce = 15f;
    public float lowJumpMultiplier = 7.5f;
    public float gravityScale = 3f;
    public float fallMultiplier = 4f;

    [Header("Ground Check")]
    [Tooltip("Set this to whatever layer your floor/stage cube is on.")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    //Amount to reduce hitbox by when crouching
    [Header("Crouch")]
    public float crouchScaleY = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.2f;
    public float doubleTapWindow = 0.25f; // Max seconds between taps

    private float lastRightTapTime;
    private float lastLeftTapTime;
    private float dashTimer;
    private int dashDirection;

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isCrouching = false;

    //Cached once per physics step so Update() and FixedUpdate() don't each fire their own raycast.
    private bool grounded;

    private Vector3 standingScale;

    private Rigidbody2D rb;
    private Collider2D col;
    private Fighter fighter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = gravityScale;
        rb.linearDamping = 0f;
        fighter = GetComponent<Fighter>();

        standingScale = transform.localScale;
    }

    void Update()
    {
        UpdateAttackStance(grounded);

        if (!fighter.CanAct()) return;

        if (fighter.currentState == FighterState.Attacking)
        {
            moveInput = 0f;
            return;
        }

        if (!fighter.isDummy)
        {
            HandleGroundedInput();
            HandleJumpInput();
        }

        //Update the crouch hitbox whenever the state changes.
        HandleCrouching();
    }

    void FixedUpdate()
    {
        if (!fighter.CanAct()) return;

        grounded = IsGrounded();

        //Runs every physics step regardless of FighterState so
        //AttackStance stays correct even while an attack is in progress 
        UpdateAttackStance(grounded);

        HandleMovementPhysics(grounded);

        HandleJumpingPhysics(grounded);
    }

    //State and stance are very different, Stance determines attack type
    //and state is what the fighter is actually doing.
    void UpdateAttackStance(bool isGrounded)
    {
        if (!isGrounded)
        {
            fighter.SetStance(AttackStance.Jumping);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            fighter.SetStance(AttackStance.Crouching);
        }
        else
        {
            fighter.SetStance(AttackStance.Standing);
        }
    }

    void HandleGroundedInput()
    {
        if (!grounded) return;

        // Don't read normal movement inputs if we are already dashing
        if (fighter.currentState == FighterState.Dashing) return;

        if (Keyboard.current.sKey.isPressed)
        {
            moveInput = 0f;
            fighter.SetState(FighterState.Crouching);
        }
        else
        {
            if (fighter.currentState == FighterState.Crouching)
                fighter.SetState(FighterState.Idle);

            moveInput = 0f;

            // --- DASH CHECK LOGIC ---
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                if (Time.time - lastLeftTapTime <= doubleTapWindow)
                    StartDash(-1); // Dash Left

                lastLeftTapTime = Time.time;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                if (Time.time - lastRightTapTime <= doubleTapWindow)
                    StartDash(1); // Dash Right

                lastRightTapTime = Time.time;
            }
            // ------------------------

            // Normal movement
            if (Keyboard.current.aKey.isPressed)
            {
                moveInput = -1f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                moveInput = 1f;
            }
        }
    }

    void HandleJumpInput()
    {
        //No jumping while crouching, and no queuing a second jump while
        //already airborne.
        if (Keyboard.current.wKey.wasPressedThisFrame && fighter.currentState != FighterState.Crouching && grounded)
        {
            jumpPressed = true;
            fighter.SetState(FighterState.Jumping);
        }
        jumpHeld = Keyboard.current.wKey.isPressed;
    }

    void HandleMovementPhysics(bool isGrounded)
    {
        // Handle Dash Physics
        if (fighter.currentState == FighterState.Dashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                fighter.SetState(FighterState.Idle);
            }
            return; // Skip normal movement physics while dashing
        }

        // Horizontal movement
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

            // Only move if not attacking and not crouching
            if (fighter.currentState != FighterState.Attacking
                && fighter.currentState != FighterState.Crouching)
            {
                if (moveInput != 0f)
                {
                    fighter.SetState(FighterState.Moving);
                }
                else
                {
                    fighter.SetState(FighterState.Idle);
                }
            }
        }
    }

    void HandleJumpingPhysics(bool isGrounded)
    {
        //Jumping
        if (jumpPressed && isGrounded)
        {
            //Launch the player
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            //Reset jumping so it doesn't double jump
            jumpPressed = false;

            fighter.SetState(FighterState.Idle);
        }

        //Jump shorter if button is tapped
        if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        //Fall faster for better fighting game movement
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    //Scales the fighter cube down when crouching and back to normal when crouch is released.
    //This method is used for the player's hitbox, the sprite animation will be in a different state machine
    void HandleCrouching()
    {
        bool shouldCrouch = fighter.currentState == FighterState.Crouching;

        //Nothing changed, nothing to do.
        if (shouldCrouch == isCrouching) return;

        //Preserve the current X sign so facing direction isn't lost.
        float xSign = Mathf.Sign(transform.localScale.x);
        float crouchY = standingScale.y * crouchScaleY;
        //Shift by this much so feet stay planted since unity scales from center.
        float heightDiff = (standingScale.y - crouchY) * 0.5f;

        float targetY = shouldCrouch ? crouchY : standingScale.y;
        float positionDelta = shouldCrouch ? -heightDiff : heightDiff;

        transform.localScale = new Vector3(standingScale.x * xSign, targetY, standingScale.z);
        transform.position += new Vector3(0f, positionDelta, 0f);

        isCrouching = shouldCrouch;
    }

    bool IsGrounded()
    {
        if (col == null) return false;

        //Check if airborne due to non-jumping reasons, creates a ray that shoots downward to detect the ground layer
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void StartDash(int direction)
    {
        fighter.SetState(FighterState.Dashing);
        dashDirection = direction;
        dashTimer = dashDuration;
    }
}