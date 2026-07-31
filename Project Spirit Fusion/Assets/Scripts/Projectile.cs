using UnityEngine;

//Lives on a projectile prefab (fireball, etc.) alongside a Hitbox component.
//This script ONLY owns movement and lifetime - Hitbox still owns all hit
//detection and damage dealing, unchanged from how melee attacks use it.
//
//Spawned fresh per-use by AttackController (not a pre-placed child like your
//melee hitboxes), since a projectile needs to travel independently of the
//fighter and multiple could be in flight at once.
[RequireComponent(typeof(Hitbox))]
public class Projectile : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private int direction; //+1 or -1, locked in at spawn time

    //Called by AttackController right after Instantiate. Reads its tunables
    //from AttackData so speed/lifetime stay data-driven like everything else,
    //then activates its own Hitbox exactly like a melee attack would.
    public void Launch(AttackData data, Fighter owner, int facingDir)
    {
        speed = data.projectileSpeed;
        lifetime = data.projectileLifetime;
        direction = facingDir;

        GetComponent<Hitbox>().Activate(data, owner);

        //Safety net despawn in case it never hits anything (flies off past
        //the stage edge, etc.) so projectiles don't live forever.
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;
    }
}
