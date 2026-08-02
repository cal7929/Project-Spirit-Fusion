using System.Collections;
using UnityEngine;

//Owns tag-team swapping for ONE player's pair of fighters (Player 1, for
//now). Only one fighter is ever active/enabled at a time - the other sits
//fully deactivated at benchPoint until tagged in.
//
//Mirrors standard MvC-style tag: the tag button swaps control and position,
//it's blocked while the active fighter is in hitstun/blockstun/knockdown
//(otherwise it'd be a free, universal combo escape - the same reason
//AttackController gates starting an attack on CanAct()), and there's a
//short cooldown after tagging in before it can be used again.
//
//Lives as its own script rather than folded into a GameManager, matching
//this project's existing pattern of single-purpose components
//(InputReader -> CommandParser -> AttackController) - tag/roster management
//is per-player squad logic, not match-level state like round timers.
public class TagController : MonoBehaviour
{
    //Cached references for one fighter slot, resolved once in Awake so
    //Update/PerformTag don't need repeated GetComponent calls.
    private struct FighterRig
    {
        public GameObject root;
        public Fighter fighter;
        public Movement movement;
        public AttackController attackController;
        public InputReader inputReader;
        public Rigidbody2D rb;
    }

    [Header("Roster")]
    [Tooltip("This player's two fighters. Each should already be a fully set up fighter (Fighter/Movement/AttackController/InputReader/CommandParser/Rigidbody2D) exactly like a normal single-fighter build.")]
    public GameObject fighter1;
    public GameObject fighter2;

    [Header("Tag Positions")]
    [Tooltip("Where the benched fighter waits, and where the outgoing fighter exits to.")]
    public Transform benchPoint;

    [Header("Timing")]
    [Tooltip("Seconds the tag-out/tag-in slide takes.")]
    public float tagDuration = 0.35f;

    [Tooltip("Extra height added mid-slide for a hop/jump feel instead of a flat slide.")]
    public float jumpArcHeight = 1.5f;

    [Tooltip("Seconds after tagging in before you can tag again.")]
    public float tagCooldown = 1f;

    private FighterRig[] rigs = new FighterRig[2];
    private int activeIndex = 0;
    private bool isTagging = false;
    private float cooldownTimer = 0f;

    //Whichever fighter is currently in play - what GameManager should treat
    //as "this team's fighter" for opponent-tracking and pushbox purposes.
    public Fighter ActiveFighter => rigs[activeIndex].fighter;
    public Rigidbody2D ActiveRigidbody => rigs[activeIndex].rb;

    //Both fighters regardless of active/benched state - GameManager needs
    //this once at Start to wire up opponent references and cross-team
    //collision ignoring for whoever ISN'T active yet too.
    public Fighter[] AllFighters => new Fighter[] { rigs[0].fighter, rigs[1].fighter };

    void Awake()
    {
        rigs[0] = BuildRig(fighter1);
        rigs[1] = BuildRig(fighter2);

        //Fighter 1 starts active and in play, Fighter 2 starts benched.
        rigs[0].root.SetActive(true);
        rigs[1].root.SetActive(false);
        rigs[1].root.transform.position = benchPoint.position;
    }

    FighterRig BuildRig(GameObject root)
    {
        return new FighterRig
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
            cooldownTimer -= Time.deltaTime;

        //Ignore new tag presses entirely while a swap is already in progress.
        if (isTagging) return;

        FighterRig active = rigs[activeIndex];

        if (!active.inputReader.Latest.tagPressed) return;
        if (cooldownTimer > 0f) return;

        //Industry-standard gate: can't tag out of hitstun/blockstun/
        //knockdown, or tagging would be a free universal combo escape. Same
        //CanAct() check AttackController already uses to gate attacks.
        if (!active.fighter.CanAct()) return;

        StartCoroutine(PerformTag());
    }

    IEnumerator PerformTag()
    {
        isTagging = true;

        int outgoingIndex = activeIndex;
        int incomingIndex = 1 - activeIndex;
        FighterRig outgoing = rigs[outgoingIndex];
        FighterRig incoming = rigs[incomingIndex];

        Vector3 tagPosition = outgoing.root.transform.position;
        Vector3 benchPosition = benchPoint.position;

        //Freeze both fighters' own input-driven behaviour and physics for
        //the slide, since position is being driven by hand here - otherwise
        //Movement's gravity/velocity would fight this coroutine.
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
        SetControllable(outgoing, true); //restored for next time it's tagged back in
        SetControllable(incoming, true);

        activeIndex = incomingIndex;
        cooldownTimer = tagCooldown;
        isTagging = false;
    }

    //Toggles whether a fighter responds to input/physics. InputReader is
    //deliberately left running on both throughout - it's harmless to keep
    //recording, and matters if buffering ever needs to carry through a tag.
    void SetControllable(FighterRig rig, bool controllable)
    {
        rig.movement.enabled = controllable;
        rig.attackController.enabled = controllable;
        if (rig.rb != null) rig.rb.simulated = controllable;
    }
}