using UnityEngine;

public class Hitbox : MonoBehaviour
{
    //Reference to the attack script that holds all the important statistical data
    private Attack sourceAttack;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sourceAttack = GetComponentInParent<Attack>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Don't hit ourselves
        if (other.gameObject == sourceAttack.gameObject) return;

        Fighter targetFighter = other.GetComponent<Fighter>();

        //Only register hits on fighters
        if (targetFighter == null)
        {
            return;
        }

        //Calculates knockback direction to push the defender away from the attacker horizontally
        Vector2 knockbackDir = new Vector2(sourceAttack.transform.localScale.x, 0f).normalized;
        Vector2 knockback = knockbackDir * sourceAttack.hitKnockback;

        //Perform attack effects
        targetFighter.TakeDamage(sourceAttack.damage, sourceAttack.hitstunDuration, knockback);
    }
}
