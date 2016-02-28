using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AmbientFlicker : Strip
{
    // Assign names to the list indicies
    //  STATE: State of the machine
    //  START_FLICKER: When it started flickering
    //  LAST_FLICKER: Time of last flicker
    //  BROKEN: Whether or not the light is broken
    private enum TupleElem { STATE, START_FLICKER, LAST_FLICKER, BROKEN };
    

    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }


    /* Override the methods given in Strip.cs to add functionality
     */
    public override void Action(GameObject caller)
    {
        SmartBrokenLight sbl = caller.GetComponent<SmartBrokenLight>();
        bool flicker = false,
             stopFlicker = false;
        float willBreak = UnityEngine.Random.Range(0, 1),
              willStopFlicker = UnityEngine.Random.Range(0, 1);
        int nextState = -1;

        // Can't do anything if the calling object does not have the correct functionality
        if (sbl != null)
        {
            // Check preconditions and state
            //
            // A light will flicker if one of the following conditions are met:
            //  (1) A nearby light is not flickering
            //  (2) The light hasn't flickered in the past lastFlicker seconds
            //  (3) It is going to break
            // ... and it is not already flickering and not currently broken
            //
            // A light will stop flickering if one of the following conditions are met:
            //  (1) It has been flickering for flickerDuration seconds
            //  (2) It randomly decides to stop flickering

            // Grab preconditions by id
            List<string> properties = bb.GetProperties(sbl.GetKey());

            // Conditionally stop flickering
            stopFlicker = int.Parse(properties[(int)TupleElem.STATE]) == (int)SmartBrokenLight.States.FLICKER
                && (willStopFlicker > sbl.probOfStopFlicker
                    || sbl.OverdueForFlickerStop(float.Parse(properties[(int)TupleElem.START_FLICKER]))
                   );

            flicker = int.Parse(properties[(int)TupleElem.STATE]) != (int)SmartBrokenLight.States.FLICKER
                && properties[(int)TupleElem.BROKEN] == "false"
                && (sbl.OverdueForFlicker(float.Parse(properties[(int)TupleElem.LAST_FLICKER]))
                    || willBreak > sbl.probBreak);

            for (int i = (int)TupleElem.BROKEN + 1; i < properties.Count; i++)
            {
                flicker &= int.Parse(properties[(int)TupleElem.STATE]) != (int)SmartBrokenLight.States.FLICKER;
            }

            // Add/Delete conditions from the BlackBoard
            //  Add condition: Changed STATE to ON, FLICKER, or DEAD
            //  Add condition: Is the light broken?

            if (flicker)
            {
                nextState = (int)SmartBrokenLight.States.FLICKER;

                if (willBreak > sbl.probBreak)
                    bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.BROKEN, "will break");
            }
            else if (stopFlicker && properties[(int)TupleElem.BROKEN] == "will break")
            {
                nextState = (int)SmartBrokenLight.States.DEAD;
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.BROKEN, "true");
            }
            else if (stopFlicker && properties[(int)TupleElem.BROKEN] == "false")
            {
                nextState = (int)SmartBrokenLight.States.ON;
            }

            bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.STATE, nextState.ToString());

            // Perform some action, maybe change state
            // If not flickering, determine whether or not to start flickering
            // Otherwise, determine whether or not to stop flickering
            if (nextState > -1)
                sbl.InduceTransition(nextState);

            // Deferred-Add/Delete conditions from the BlackBoard
            //  Continue flickering
            //      Add condition: LAST_FLICKER is += Time.deltaTime if still flickering
            //  Stop flickering
            //      (Change nothing)
            if (flicker)
            {
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.LAST_FLICKER, (float.Parse(properties[(int)TupleElem.LAST_FLICKER]) + Time.deltaTime).ToString());
            }
        }
    }
}