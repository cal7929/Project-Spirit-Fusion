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

    //Fired when this projectile is destroyed (hit something, or timed out).
    //AttackController subscribes to this so it knows when its "one
    //projectile in flight" slot frees up again.
    public event Action OnDespawned;

    //Called by AttackController right after Instantiate. Sends the
    //projectile forward and then activates its own hitbox the way a fighter would, (like a mini fighter)
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

    //Covers both despawn paths: timing out (Destroy scheduled in Launch) and
    //being destroyed early by Hitbox's destroyOnHit when it connects.
    void OnDestroy()
    {
        OnDespawned?.Invoke();
    }
}
