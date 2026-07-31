using System;
using UnityEngine;

//Used alongside a hitbox and called by AttackController to spawn a
//projectile that travels independently of the fighter.
[RequireComponent(typeof(Hitbox))]
public class Projectile : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private int direction;

    //AttackController subscribes to this so it knows when it can shoot another projectile
    public event Action OnDespawned;

    //Called by AttackController right after Instantiate. Sends the projectile forward and
    //then activates its own hitbox the way a fighter would, (like a mini fighter)
    public void Launch(AttackData data, Fighter owner, int facingDir)
    {
        speed = data.projectileSpeed;
        lifetime = data.projectileLifetime;
        direction = facingDir;

        GetComponent<Hitbox>().Activate(data, owner);

        //If the projectile whiffs
        Destroy(gameObject, lifetime);
    }

    //Movement
    void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;
    }

    //Triggers the event when the object is destroyed
    void OnDestroy()
    {
        OnDespawned?.Invoke();
    }
}
