using UnityEngine;

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

    [Header("Body")]
    [Tooltip("Active while standing or jumping - this fighter's normal hurtbox (visual + Collider2D).")]
    public GameObject standingBody;

    [Tooltip("Active while crouching - a separate, pre-made shorter hurtbox instead of dynamically scaling the standing one. Swapped in/out entirely, same pattern as Hitbox.Activate/Deactivate.")]
    public GameObject crouchingBody;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.2f;
    public float doubleTapWindow = 0.25f;

    // To track when the taps were pressed to avoid false dashing
    private float lastRightTapTime;
    private float lastLeftTapTime;
    private float dashTimer;
    private int dashDirection;
    private float lastRawX = 0f;

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isCrouching = false;

    // Saved once per physics step so Update() and FixedUpdate() don't each fire their own raycast.
    private bool grounded;

    private Collider2D standingCollider;
    private Collider2D crouchingCollider;

    private Rigidbody2D rb;
    private Collider2D col;
    private Fighter fighter;
    private InputReader inputReader;

    // ANIMATION
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.linearDamping = 0f;

        fighter = GetComponent<Fighter>();
        inputReader = GetComponent<InputReader>();

        // ANIMATION
        // Animator is on Cole, not Visual.
        animator = GetComponent<Animator>();

        if (standingBody == null || crouchingBody == null)
        {
            Debug.LogWarning("Movement.standingBody/crouchingBody not fully assigned - crouch hurtbox switching will not work.", this);
        }
        else
        {
            // GetComponentInChildren instead of GetComponent, so this still
            // works if the Collider2D ends up one level deeper.
            standingCollider = standingBody.GetComponentInChildren<Collider2D>();
            crouchingCollider = crouchingBody.GetComponentInChildren<Collider2D>();

            if (standingCollider == null)
                Debug.LogWarning("Movement.standingBody has no Collider2D on it or its children.", this);

            if (crouchingCollider == null)
                Debug.LogWarning("Movement.crouchingBody has no Collider2D on it or its children.", this);
        }

        // Start standing.
        SetCrouchingBody(false);
    }

    void Update()
    {
        UpdateAttackStance(grounded);

        // Runs unconditionally, even mid-attack.
        HandleCrouching();

        if (!fighter.CanAct())
        {
            UpdateAnimation();
            return;
        }

        if (fighter.currentState == FighterState.Attacking)
        {
            moveInput = 0f;
            UpdateAnimation();
            return;
        }

        HandleGroundedInput();
        HandleJumpInput();

        // ANIMATION
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!fighter.CanAct()) return;

        grounded = IsGrounded();

        // Runs every physics step regardless of FighterState so
        // AttackStance stays correct even while an attack is in progress.
        UpdateAttackStance(grounded);

        HandleMovementPhysics(grounded);

        HandleJumpingPhysics(grounded);
    }

    // State and stance are very different.
    // Stance determines attack type and state is what the fighter is actually doing.
    void UpdateAttackStance(bool isGrounded)
    {
        if (!isGrounded)
        {
            fighter.SetStance(AttackStance.Jumping);
        }
        else if (inputReader.Latest.isCrouchHeld)
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

        // Can't move during the dash
        if (fighter.currentState == FighterState.Dashing) return;

        // Crouching posture is tracked via currentStance.
        if (fighter.currentStance == AttackStance.Crouching)
        {
            moveInput = 0f;
            return;
        }

        moveInput = 0f;

        float currentRawX = inputReader.Latest.rawX;

        // Detect a fresh tap back or forward.
        if (currentRawX != 0f && lastRawX == 0f)
        {
            if (currentRawX < 0f)
            {
                if (Time.time - lastLeftTapTime <= doubleTapWindow)
                    StartDash(-1);

                lastLeftTapTime = Time.time;
            }
            else if (currentRawX > 0f)
            {
                if (Time.time - lastRightTapTime <= doubleTapWindow)
                    StartDash(1);

                lastRightTapTime = Time.time;
            }
        }

        lastRawX = currentRawX;

        // Normal movement
        moveInput = currentRawX;
    }

    void HandleJumpInput()
    {
        if (inputReader.Latest.isJumpPressed
            && fighter.EffectiveStance != AttackStance.Crouching
            && grounded)
        {
            jumpPressed = true;
            fighter.SetState(FighterState.Jumping);
        }

        jumpHeld = inputReader.Latest.isJumpHeld;
    }

    void HandleMovementPhysics(bool isGrounded)
    {
        // Handle Dash Physics
        if (fighter.currentState == FighterState.Dashing)
        {
            rb.linearVelocity = new Vector2(
                dashDirection * dashSpeed,
                rb.linearVelocity.y
            );

            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                fighter.SetState(FighterState.Idle);
            }

            // Skip normal movement during a dash.
            return;
        }

        // Horizontal movement
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(
                moveInput * speed,
                rb.linearVelocity.y
            );

            // Only move if not attacking and not crouching.
            if (fighter.currentState != FighterState.Attacking
                && fighter.EffectiveStance != AttackStance.Crouching)
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
        // Jumping
        if (jumpPressed && isGrounded)
        {
            // Launch the player
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            // Reset jumping so it doesn't double jump.
            jumpPressed = false;

            // Keep the fighter in the Jumping state.
            fighter.SetState(FighterState.Jumping);
        }

        // Jump shorter if button is tapped.
        if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up
                * Physics2D.gravity.y
                * (lowJumpMultiplier - 1)
                * Time.fixedDeltaTime;
        }

        // Fall faster for better fighting game movement.
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up
                * Physics2D.gravity.y
                * (fallMultiplier - 1)
                * Time.fixedDeltaTime;

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    0f
                );
            }
        }
    }

    // Swaps which hurtbox GameObject is active.
    void HandleCrouching()
    {
        bool shouldCrouch =
            fighter.EffectiveStance == AttackStance.Crouching;

        // Nothing changed, nothing to do.
        if (shouldCrouch == isCrouching) return;

        SetCrouchingBody(shouldCrouch);
        isCrouching = shouldCrouch;
    }

    // Activates the requested body and deactivates the other.
    void SetCrouchingBody(bool crouching)
    {
        if (standingBody != null)
            standingBody.SetActive(!crouching);

        if (crouchingBody != null)
            crouchingBody.SetActive(crouching);

        col = crouching
            ? crouchingCollider
            : standingCollider;
    }

    bool IsGrounded()
    {
        if (col == null) return false;

        // Check if airborne due to non-jumping reasons.
        Vector2 origin = new Vector2(
            col.bounds.center.x,
            col.bounds.min.y
        );

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    void StartDash(int direction)
    {
        fighter.SetState(FighterState.Dashing);

        dashDirection = direction;
        dashTimer = dashDuration;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    void UpdateAnimation()
    {
        if (animator == null)
            return;

        // Don't let Movement override attack animations.
        if (fighter.currentState == FighterState.Attacking)
            return;

       
        if (fighter.currentStance == AttackStance.Crouching)
        {
            PlayAnimation("Crouch");
        }

        if (fighter.currentState == FighterState.Blockstun)
        {
            PlayAnimation("Blockstun");
        }

        if (fighter.currentState == FighterState.Hitstun)
        {
            PlayAnimation("Hitstun");
        }

        // Jumping will be added next.
        if (fighter.currentState == FighterState.Jumping)
            return;

        // Dash will be added next.
        if (fighter.currentState == FighterState.Dashing)
            return;

        // Walking
        if (fighter.currentState == FighterState.Moving)
        {
            PlayAnimation("Walk");
            return;
        }

        // Idle
        if (fighter.currentState == FighterState.Idle
            && fighter.currentStance == AttackStance.Standing)
        {
            PlayAnimation("Idle");
        }
    }

    void PlayAnimation(string animationName)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            return;

        animator.Play(animationName);
    }
}