using System.Collections;
using UnityEngine;

//Manages each player's team and allows them to tag between their active and benched fighter.
public class TagController : MonoBehaviour
{
    //References for a fighter slot on your team, resolved once in Awake so
    //Update/PerformTag doesn't need to repeat GetComponent calls.
    private struct FighterSlot
    {
        public GameObject root;
        public Fighter fighter;
        public Movement movement;
        public AttackController attackController;
        public InputReader inputReader;
        public Rigidbody2D rb;
    }

    [Header("Roster")]
    public GameObject fighter1;
    public GameObject fighter2;

    //Where the deactive fighter idles until they are tagged in
    [Header("Tag Positions")]
    public Transform benchPoint;

    public float tagDuration = 0.35f;

    public float jumpArcHeight = 1.5f;

    public float tagCooldown = 1f;

    private FighterSlot[] slots = new FighterSlot[2];
    private int activeIndex = 0;
    private bool isTagging = false;
    private float cooldownTimer = 0f;

    //Whichever fighter is currently in play for GameManager's opponent and pushbox tracking.
    public Fighter ActiveFighter => slots[activeIndex].fighter;
    public Rigidbody2D ActiveRigidbody => slots[activeIndex].rb;

    public Fighter[] AllFighters => new Fighter[] { slots[0].fighter, slots[1].fighter };

    void Awake()
    {
        slots[0] = BuildRig(fighter1);
        slots[1] = BuildRig(fighter2);

        //Fighter 1 starts active and in play, Fighter 2 starts benched.
        slots[0].root.SetActive(true);
        slots[1].root.SetActive(false);
        slots[1].root.transform.position = benchPoint.position;
    }

    FighterSlot BuildRig(GameObject root)
    {
        return new FighterSlot
        {
            root = root,
            fighter = root.GetComponent<Fighter>(),
            movement = root.GetComponent<Movement>(),
            attackController = root.GetComponent<AttackController>(),
            inputReader = root.GetComponent<InputReader>(),
            rb = root.GetComponent<Rigidbody2D>()
        };
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        //No tagging while a tag is taking place
        if (isTagging) return;

        FighterSlot active = slots[activeIndex];

        if (!active.inputReader.Latest.tagPressed) return;
        if (cooldownTimer > 0f) return;

        //You can only tag in neutral
        if (!active.fighter.CanAct() || active.fighter.currentStance != AttackStance.Standing) return;

        StartCoroutine(PerformTag());
    }

    IEnumerator PerformTag()
    {
        isTagging = true;

        //Tracks which fighter is going in and out
        int outgoingIndex = activeIndex;
        int incomingIndex = 1 - activeIndex;
        FighterSlot outgoing = slots[outgoingIndex];
        FighterSlot incoming = slots[incomingIndex];

        Vector3 tagPosition = outgoing.root.transform.position;
        Vector3 benchPosition = benchPoint.position;

        //Prevents movement during the tag
        SetControllable(outgoing, false);
        incoming.root.SetActive(true);
        SetControllable(incoming, false);
        incoming.root.transform.position = benchPosition;

        float t = 0f;
        while (t < tagDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / tagDuration);
            float arc = Mathf.Sin(p * Mathf.PI) * jumpArcHeight;

            outgoing.root.transform.position = Vector3.Lerp(tagPosition, benchPosition, p) + Vector3.up * arc;
            incoming.root.transform.position = Vector3.Lerp(benchPosition, tagPosition, p) + Vector3.up * arc;

            yield return null;
        }

        outgoing.root.transform.position = benchPosition;
        incoming.root.transform.position = tagPosition;

        outgoing.root.SetActive(false);
        SetControllable(outgoing, true); 
        SetControllable(incoming, true);

        activeIndex = incomingIndex;
        cooldownTimer = tagCooldown;
        isTagging = false;
    }

    //Sets whether or not a fighter can be controller by input
    void SetControllable(FighterSlot rig, bool controllable)
    {
        rig.movement.enabled = controllable;
        rig.attackController.enabled = controllable;
        if (rig.rb != null) rig.rb.simulated = controllable;
    }
}