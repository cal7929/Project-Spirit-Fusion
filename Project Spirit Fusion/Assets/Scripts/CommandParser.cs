using UnityEngine;
using System.Collections.Generic;

//Watches InputBuffer for known special-move motions (quarter circles, dragon
//punch shapes, charge motions, etc.) and returns the matching AttackData when
//one is completed. AttackController calls TryParseSpecial() once per frame
//alongside its normal-attack handling; specials take priority when both would
//otherwise trigger on the same frame.
//
//This is a simplified/teaching-level matcher (strict in-order subsequence
//matching). See the notes at the bottom of this file for how real fighting
//games extend this further.
public class CommandParser : MonoBehaviour
{
    [Tooltip("All special moves this character can perform. Only entries with a non-empty motionInput are considered.")]
    public List<AttackData> specialMoves = new List<AttackData>();

    [Tooltip("How many frames back to search for a motion. Wider = more lenient timing, but also more prone to false positives.")]
    public int searchWindow = 20;

    private InputReader inputBuffer;

    void Awake()
    {
        inputBuffer = GetComponent<InputReader>();
    }

    //Call once per frame. Returns the AttackData for the first special whose
    //motion was completed with a button press on THIS frame, or null.
    public AttackData TryParseSpecial()
    {
        InputReader.InputFrame[] recent = inputBuffer.GetRecentFrames(searchWindow);
        if (recent.Length == 0) return null;

        InputReader.InputFrame last = recent[recent.Length - 1];
        bool buttonThisFrame = last.lightPressed || last.mediumPressed || last.heavyPressed;
        if (!buttonThisFrame) return null;

        foreach (AttackData move in specialMoves)
        {
            if (!move.IsSpecialMove) continue;
            if (MotionMatches(move.motionInput, recent))
                return move;
        }

        return null;
    }

    //Checks whether the digits in `motion` (e.g. "236") appear, in order, as a
    //subsequence of the directions held across `frames`. A run of frames
    //holding the same direction only counts once, so holding down-forward for
    //several frames doesn't require several separate matching frames.
    bool MotionMatches(string motion, InputReader.InputFrame[] frames)
    {
        int motionIndex = 0;
        int lastDirection = -1;

        foreach (InputReader.InputFrame frame in frames)
        {
            if (motionIndex >= motion.Length) break;

            int wantDigit = motion[motionIndex] - '0';

            if (frame.direction == wantDigit && frame.direction != lastDirection)
            {
                motionIndex++;
            }

            lastDirection = frame.direction;
        }

        return motionIndex >= motion.Length;
    }

    //---- Where to take this next (not implemented here on purpose) ----
    //
    // 1. Diagonal leniency: real engines usually accept a direct 2->6 as
    //    satisfying "236" even if 3 was skipped for a frame or two, since
    //    players roll their thumb through the diagonal fast. You'd loosen
    //    MotionMatches to allow "adjacent enough" directions to count.
    //
    // 2. Charge motions (e.g. "[4]6" = hold back 40+ frames, then forward):
    //    needs tracking HOW LONG a direction was held, not just that it
    //    appeared - a different check than simple subsequence matching.
    //
    // 3. Priority between multiple matching specials: if two moves' motions
    //    both matched this frame, decide a tie-break (e.g. prefer the one
    //    needing the longer/more specific motion).
    //
    // 4. Buffering during hitstop/animation lock: if the parser only checks
    //    "button pressed AND motion matched on the same exact frame," players
    //    who finish the motion slightly before their recovery ends will drop
    //    inputs. Consider allowing the motion-complete flag to persist a few
    //    frames waiting for the button, not just the reverse.
}