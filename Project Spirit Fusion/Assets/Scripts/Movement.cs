using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public float speed = 8f;

    [Header("Jump")]
    public float jumpForce = 15f;
    public float lowJumpMultiplier = 7.5f;
    public float gravityScale = 3f;
    public float fallMultiplier = 4f;

    [Header("Ground Check")]
    [Tooltip("Set this to whatever layer your floor/stage cube is on.")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    //Will likely be reworked when sprites get added
    [Header("Crouch")]
    public float crouchScaleY = 0.5f;

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isCrouching = false;

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
        bool grounded = IsGrounded();
        fighter.isAirborne = !grounded;

        if (!fighter.CanAct()) return;

        if (fighter.currentState == FighterState.Attacking)
        {
            moveInput = 0f;
            return;
        }

        if (grounded)
        {
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
                if (Keyboard.current.aKey.isPressed)
                    moveInput = -1f;
                else if (Keyboard.current.dKey.isPressed)
                    moveInput = 1f;
            }
        }

        //No jumping while crouching
        if (Keyboard.current.wKey.wasPressedThisFrame && fighter.currentState != FighterState.Crouching)
            jumpPressed = true;
        jumpHeld = Keyboard.current.wKey.isPressed;

        //Update the crouch geometry whenever the state changes.
        //This is for convenience, once sprites get added, much of the crouching logic will change
        UpdateCrouchScale();
    }

    void FixedUpdate()
    {
        if (!fighter.CanAct()) return;

        bool grounded = IsGrounded();

        if (grounded)
        {
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

            if (fighter.currentState != FighterState.Attacking
                && fighter.currentState != FighterState.Crouching)
            {
                fighter.SetState(moveInput != 0f ? FighterState.Moving : FighterState.Idle);
            }
        }

        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
            fighter.SetState(FighterState.Idle);
        }

        if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            if (grounded)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    //Scales the fighter cube down when crouching and back to normal when crouch is released.
    //This method can be removed once animations are added, this is just for the crude build we have now.
    void UpdateCrouchScale()
    {
        bool shouldCrouch = fighter.currentState == FighterState.Crouching;

        if (shouldCrouch && !isCrouching)
        {
            isCrouching = true;

            //Preserve the current X sign so facing direction isn't lost.
            float xSign = Mathf.Sign(transform.localScale.x);
            float crouchY = standingScale.y * crouchScaleY;
            transform.localScale = new Vector3(standingScale.x * xSign, crouchY, standingScale.z);

            //Shift down so feet stay planted since unity scales from center.
            float heightDiff = (standingScale.y - crouchY) * 0.5f;
            transform.position -= new Vector3(0f, heightDiff, 0f);
        }
        else if (!shouldCrouch && isCrouching)
        {
            isCrouching = false;

            float xSign = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(standingScale.x * xSign, standingScale.y, standingScale.z);

            float crouchY = standingScale.y * crouchScaleY;
            float heightDiff = (standingScale.y - crouchY) * 0.5f;
            transform.position += new Vector3(0f, heightDiff, 0f);
        }
    }

    bool IsGrounded()
    {
        if (col == null) return false;
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }
}