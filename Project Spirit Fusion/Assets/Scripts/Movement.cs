using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    public float speed = 8f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public float lowJumpMultiplier = 5f;
    public float gravityScale = 4f;
    public float fallMultiplier = 6f;

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        rb.linearDamping = 0f;
    }

    void Update()
    {
        moveInput = 0f;

        if (Keyboard.current.aKey.isPressed)
            moveInput = -1f;
        else if (Keyboard.current.dKey.isPressed)
            moveInput = 1f;

        if (Keyboard.current.wKey.wasPressedThisFrame)
            jumpPressed = true;

        jumpHeld = Keyboard.current.wKey.isPressed;
    }

    void FixedUpdate()
    {
        
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        // Jump
        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        if(rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }


        
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    bool IsGrounded()
    {
        
        return Mathf.Abs(rb.linearVelocity.y) < 0.01f;
    }
}