using UnityEngine;
using System.Collections.Generic;

//Watches InputBuffer for known special-move motion notation and returns the matching AttackData when
//one is completed. AttackController calls TryParseSpecial() once per frame alongside its normal-attack
//handling so that specials take priority when both trigger on the same frame.
public class CommandParser : MonoBehaviour
{
    //All special moves this character can perform.
    public List<AttackData> specialMoves = new List<AttackData>();

    //How many frames back to search for a motion. Larger number = more leniant timing, but more likely to perform unintentional moves.
    public int searchWindow = 20;

    private InputReader inputBuffer;

    void Awake()
    {
        inputBuffer = GetComponent<InputReader>();
    }

    //Called once per frame. Returns the AttackData for the first special whose
    //motion was completed with the matching button press on this frame.
    public AttackData TryParseSpecial()
    {
        InputReader.InputFrame[] recent = inputBuffer.GetRecentFrames(searchWindow);
        if (recent.Length == 0) return null;

        InputReader.InputFrame last = recent[recent.Length - 1];

        AttackStrength? pressedStrength = null;
        if (last.lightPressed) pressedStrength = AttackStrength.Light;
        else if (last.mediumPressed) pressedStrength = AttackStrength.Medium;
        else if (last.heavyPressed) pressedStrength = AttackStrength.Heavy;

        if (pressedStrength == null) return null;

        foreach (AttackData move in specialMoves)
        {
            if (!move.IsSpecialMove) continue;
            if (move.strength != pressedStrength.Value) continue;
            if (MotionMatches(move.motionInput, recent))
                return move;
        }

        return null;
    }

    //Allows for diagonals to be skipped for leniant inputs, standard in all modern fighting games,
    //Older games didn't have this, as such their inputs were extremely strict.
    private static readonly Dictionary<int, int[]> Adjacent = new Dictionary<int, int[]>
    {
        { 1, new[] { 2, 4 } },
        { 2, new[] { 1, 3 } },
        { 3, new[] { 2, 6 } },
        { 4, new[] { 1, 7 } },
        { 6, new[] { 3, 9 } },
        { 7, new[] { 4, 8 } },
        { 8, new[] { 7, 9 } },
        { 9, new[] { 6, 8 } },
    };

    //Checks the frames for any matching motion inputs. 
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
            //Handles the motion leniancy
            else if (motionIndex + 1 < motion.Length)
            {
                int nextWantDigit = motion[motionIndex + 1] - '0';
                bool skippedStepIsAdjacent = Adjacent.TryGetValue(wantDigit, out int[] neighbors)
                    && System.Array.IndexOf(neighbors, frame.direction) != -1;

                if (frame.direction == nextWantDigit && skippedStepIsAdjacent && frame.direction != lastDirection)
                {
                    motionIndex += 2;
                }
            }

            lastDirection = frame.direction;
        }

        return motionIndex >= motion.Length;
    }
}