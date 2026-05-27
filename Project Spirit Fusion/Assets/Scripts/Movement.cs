using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public float speed = 5f;
    private bool isGrounded;

    private Vector2 direction;
    private Vector2 velocity;
    private Vector2 position;

    private Vector2 screenBounds;

    private Rigidbody2D rb;


    void Start()
    {
        screenBounds.y = Camera.main.orthographicSize;
        screenBounds.x = screenBounds.y * Camera.main.aspect;

        rb = GetComponent<Rigidbody2D>();
    }


    void Update()
    {
        direction = Vector2.zero;


        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            
            direction.y += 1f;
           
        }
    
                
        if (Keyboard.current.aKey.isPressed)
            direction.x -= 1f;
        if (Keyboard.current.dKey.isPressed)
            direction.x += 1f;


        direction = direction.normalized;
    }

    private void FixedUpdate()
    {
        velocity = speed * direction;

        position = rb.position + velocity  * Time.deltaTime;

        if (position.x > screenBounds.x)
            position.x = -screenBounds.x;
        else if (position.x < -screenBounds.x)
            position.x = screenBounds.x;

        if (position.y > screenBounds.y)
            position.y = -screenBounds.y;
        else if (position.y < -screenBounds.y)
            position.y = screenBounds.y;



        rb.MovePosition(position);
    }
}
