using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

//Records raw input every frame into a fixed-size ring buffer so that motion
//inputs (quarter circles, dragon punch shapes, etc.) and buffered button
//presses can be read back a few frames later by CommandParser.
//
//IMPORTANT SETUP NOTE: this needs to record BEFORE AttackController reads it
//each frame. Unity doesn't guarantee Update() order between components by
//default - set this via Edit > Project Settings > Script Execution Order,
//placing InputBuffer before AttackController. Otherwise AttackController may
//read last frame's data.
public class InputReader : MonoBehaviour
{
    //One frame's worth of recorded input. Direction uses numpad notation
    //(1-9, 5 = neutral) already adjusted for which way the fighter is facing,
    //so "6" always means "forward" regardless of player side.
    public struct InputFrame
    {
        public int frameNumber;
        public int direction;
        public bool lightPressed;
        public bool mediumPressed;
        public bool heavyPressed;
    }

    [Tooltip("How many frames of history to keep. At 60fps, 30 = half a second, which is plenty for most motion inputs.")]
    public int bufferSize = 30;

    private Queue<InputFrame> buffer = new Queue<InputFrame>();
    private int frameCounter;
    private InputFrame lastFrame;

    private Fighter fighter;

    //Numpad-notation grid. Row = vertical (down/mid/up), Col = back/neutral/forward.
    private static readonly int[,] NumpadGrid =
    {
        { 1, 2, 3 }, // down-back, down, down-forward
        { 4, 5, 6 }, // back, neutral, forward
        { 7, 8, 9 }  // up-back, up, up-forward
    };

    void Awake()
    {
        fighter = GetComponent<Fighter>();
    }

    void Update()
    {
        Record();
    }

    void Record()
    {
        frameCounter++;

        Vector2Int raw = ReadRawDirection();
        int direction = ToNumpadNotation(raw, fighter.facingDir);

        lastFrame = new InputFrame
        {
            frameNumber = frameCounter,
            direction = direction,
            lightPressed = Keyboard.current.jKey.wasPressedThisFrame,
            mediumPressed = Keyboard.current.kKey.wasPressedThisFrame,
            heavyPressed = Keyboard.current.lKey.wasPressedThisFrame
        };

        buffer.Enqueue(lastFrame);
        while (buffer.Count > bufferSize)
            buffer.Dequeue();
    }

    Vector2Int ReadRawDirection()
    {
        int x = 0, y = 0;
        if (Keyboard.current.aKey.isPressed) x -= 1;
        if (Keyboard.current.dKey.isPressed) x += 1;
        if (Keyboard.current.sKey.isPressed) y -= 1;
        if (Keyboard.current.wKey.isPressed) y += 1;
        return new Vector2Int(x, y);
    }

    //Converts raw x/y (-1, 0, 1) into numpad notation, mirroring x when facing
    //left so that "forward" is always the same digit regardless of side.
    int ToNumpadNotation(Vector2Int raw, float facingDir)
    {
        int x = raw.x;
        if (facingDir < 0) x = -x;

        int col = x + 1;      // -1,0,1 -> 0,1,2
        int row = raw.y + 1;  // -1,0,1 -> 0,1,2
        return NumpadGrid[row, col];
    }

    //Most recent recorded frame. Use this for simple "was a button just
    //pressed" checks (normals) instead of reading Keyboard.current directly -
    //keeps all input reading in one place.
    public InputFrame Latest => lastFrame;

    //Returns up to `count` of the most recent frames, oldest first. Used by
    //CommandParser to scan for motion patterns.
    public InputFrame[] GetRecentFrames(int count)
    {
        InputFrame[] all = buffer.ToArray();
        int start = Mathf.Max(0, all.Length - count);
        int length = all.Length - start;
        InputFrame[] result = new InputFrame[length];
        System.Array.Copy(all, start, result, 0, length);
        return result;
    }
}
