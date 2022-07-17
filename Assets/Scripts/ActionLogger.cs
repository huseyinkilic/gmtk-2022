using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionLogger
{
    // has one function for each thing that can happen
    // creature X from team Y uses move Z
    // creature X from team Y misses move Z
    // creature X from team Y hits creature Q from team W with move Z, dealing U damage
    // creature X from team Y recives the Z condition
    // creature X from team Y is cured of the Z condition
    // can't move because condition
    // changes type
    // etc

    public static void LogMessage(string msg)
    {
        Debug.Log(msg);
    }
}
