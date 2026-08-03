using UnityEngine;

//Handles a projectiles movement and lifetime, all other parts are handled by other scripts
[RequireComponent(typeof(Hitbox))]
public class Projectile : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private int direction; 

    //Called by AttackController right aftern its instantantiated
    public void Launch(AttackData data, Fighter owner, int facingDir)
    {
        speed = data.projectileSpeed;
        lifetime = data.projectileLifetime;
        direction = facingDir;

        GetComponent<Hitbox>().Activate(data, owner);

        //Despawn if it doesn't hit
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;
    }
}



