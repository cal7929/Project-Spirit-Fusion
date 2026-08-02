using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

//Records player input every frame so that special moves and command normals can be performed,
//aslo allows for input buffering in combos. This input is sent to the command parser to actually read
//In the project settings this script has been set to read BEFORE Attack Controller. This is needed
//Because Unity doesn't guarantee the order in which Update()'s resolve.
public class InputReader : MonoBehaviour
{
    //Struct for keeping track of what buttons were pressed each individual frame
    public struct InputFrame
    {
        public int frameNumber;
        public int direction;
        public bool lightPressed;
        public bool mediumPressed;
        public bool heavyPressed;
        public bool tagPressed;

        //Future inputs
        //specialtagPressed
    }

    [Tooltip("How many frames of history to keep. At 60fps, 30 = half a second.")]
    public int bufferSize = 30;

    private Queue<InputFrame> buffer = new Queue<InputFrame>();
    private int frameCounter;
    private InputFrame lastFrame;

    private Fighter fighter;

    //Numpad-notation grid property. Standard for fighting games:
    //5 is neutral, 4 is back, 6 is forward, 8 is up, and 2 is down (array is flipped). The others are diagonals
    private static readonly int[,] NumpadGrid =
    {
        { 1, 2, 3 },
        { 4, 5, 6 },
        { 7, 8, 9 }
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
            heavyPressed = Keyboard.current.lKey.wasPressedThisFrame,
            tagPressed = Keyboard.current.uKey.wasPressedThisFrame
        };

        buffer.Enqueue(lastFrame);
        while (buffer.Count > bufferSize)
        {
            buffer.Dequeue();
        }
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

    //Converts x/y (-1, 0, 1) input into the numpad notation, mirroring x when facing
    //left so that forward is always the same digit regardless of side.
    int ToNumpadNotation(Vector2Int raw, float facingDir)
    {
        int x = raw.x;
        if (facingDir < 0) x = -x;

        int col = x + 1;      //-1,0,1 = 0,1,2
        int row = raw.y + 1;  //-1,0,1 = 0,1,2
        return NumpadGrid[row, col];
    }

    //Most recent recorded frame. 
    public InputFrame Latest => lastFrame;

    //Returns up to the count of the most recent frames, oldest first. Used by
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