using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    public float speed = 12f;

    [Header("Jump")]
    public float jumpForce = 15f;
    public float lowJumpMultiplier = 7.5f;
    public float gravityScale = 3f;
    public float fallMultiplier = 4f;

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;

    private Rigidbody2D rb;
    private Fighter fighter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        rb.linearDamping = 0f;

        fighter = GetComponent<Fighter>();
    }

    void Update()
    {
        if (!fighter.CanAct()) return;
        
        if(IsGrounded())
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

        //While fighter is on the ground
        if (IsGrounded())
        {
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

            //Sets moving or idle only if fighter isn't attacking
            if (fighter.currentState != FighterState.Attacking)
            {
                if (moveInput != 0f)
                    fighter.SetState(FighterState.Moving);
                else
                    fighter.SetState(FighterState.Idle);
            }
        }

        //Jump
        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        //Sets jump velocity after jumping for standard fighting game rules
        if(rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
        
        //Extra downward gravity to mimic falling (may remove if too fast)
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;

            if (IsGrounded())
            {
                rb.linearVelocity = new Vector2(0f, 0f); 
            }
        }
    }

    bool IsGrounded()
    {
        return Mathf.Abs(rb.linearVelocity.y) < 0.05f; //0.01f
    }
}