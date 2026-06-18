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

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;

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
    }

    void Update()
    {
        if (!fighter.CanAct()) return;

        //Roots the character while attacking to avoid sliding around.
        if (fighter.currentState == FighterState.Attacking)
        {
            moveInput = 0f;
        }
        else if (IsGrounded())
        {
            moveInput = 0f;
            if (Keyboard.current.aKey.isPressed)
                moveInput = -1f;
            else if (Keyboard.current.dKey.isPressed)
                moveInput = 1f;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
            jumpPressed = true;
        jumpHeld = Keyboard.current.wKey.isPressed;
    }

    void FixedUpdate()
    {
        if (!fighter.CanAct()) return;

        bool grounded = IsGrounded();

        if (grounded)
        {
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

            if (fighter.currentState != FighterState.Attacking)
            {
                fighter.SetState(moveInput != 0f ? FighterState.Moving : FighterState.Idle);
            }
        }

        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        //Shorter hop if jump isn't held (standard fighting game jump arc).
        if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        //Extra downward gravity to mimic falling.
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            if (grounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
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