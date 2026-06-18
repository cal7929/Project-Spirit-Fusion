using UnityEngine;

//This is a data container, not a MonoBehaviour.
//One of these exists for each attack to hold its stats and values.
//Gets assigned in the Inspector on AttackController.
//It is more efficient to use a data container apparently for this type of thing than a MonoBehavior
//I had to look up how to set this up, its not hard at all to understand. (I also learned how to use tooltips, very useful)
[System.Serializable]
public class AttackData
{
    [Header("Attack Name")]
    public string attackName = "Light";

    [Tooltip("The object that acts as this attack's hitbox. Should have a trigger Collider2D and a Hitbox component, and start disabled in the scene.")]
    public GameObject hitboxObject;

    [Header("Damage")]
    public int damage = 10;
    public float hitstunDuration = 0.4f;
    public float hitKnockback = 2f;

    [Header("Frame Data (seconds, not frames yet - fine for now)")]
    public float startupTime = 0.05f;
    public float activeTime = 0.1f;
    public float recoveryTime = 0.2f;
}
