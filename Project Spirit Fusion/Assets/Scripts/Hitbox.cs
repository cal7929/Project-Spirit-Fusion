using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [Tooltip("Only enable for projectile attacks")]
    public bool destroyOnHit = false;

    private AttackData attackData;
    private Fighter owner;

    //Tracks who's already been hit during the current activation, prevents bugs with multihitting 
    //on lingering hitboxes or if a single hit move hits two hittable hitboxes.
    private HashSet<Fighter> hitThisActivation = new HashSet<Fighter>();

    //Called by AttackController when this attack's active frames begin.
    public void Activate(AttackData data, Fighter ownerFighter)
    {
        attackData = data;
        owner = ownerFighter;
        hitThisActivation.Clear();
        gameObject.SetActive(true);
    }

    //Called by AttackController when active frames end or on a cancel.
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    //Deals the damage based on attack data
    void OnTriggerEnter2D(Collider2D other)
    {
        Fighter targetFighter = other.GetComponentInParent<Fighter>();

        //Attacks don't hit your own hitbox
        if (targetFighter == null) return;
        if (targetFighter == owner) return;

        //Already hit this target during this activation
        if (!hitThisActivation.Add(targetFighter)) return;

        Vector2 knockbackDir = new Vector2(owner.facingDir, 0f).normalized;
        Vector2 knockback = knockbackDir * attackData.hitKnockback;

        targetFighter.TakeDamage(attackData.damage, attackData.hitstunFrames, knockback, attackData.type);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}