using UnityEngine;

public struct LevelCompletedEvent { }

// The liquid has all landed in the target; the source tube is still on its way home.
public struct PourFlowEndedEvent
{
    public TubeView From;
    public TubeView To;
}

public struct PourAnimationCompletedEvent
{
    public TubeView From;
    public TubeView To;
}

public struct PourCompletedEvent
{
    public TubeView From;
    public TubeView To;
}

public struct TubeClickedEvent
{
    public TubeView Tube;
}

// A tap on the board that is not on or beside any bottle that takes taps.
public struct EmptySpaceClickedEvent { }

public struct ShakeCompletedEvent
{
    public TubeView Tube;
}

public struct TubeSolvedEvent
{
    public Color SolvedColor;
}

public struct DeadlockEvent { }