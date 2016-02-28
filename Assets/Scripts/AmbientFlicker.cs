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
    private string LIGHT_WILL_BREAK = "will break",
                   LIGHT_IS_BROKEN = "true",
                   LIGHT_NOT_BROKEN = "false";
    

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
             flickerBroken = false,
             stopFlickerBroken = false,
             stopFlicker = false;
        float probFlicker = UnityEngine.Random.Range(0.0f, 1.0f),
              probStopFlicker = UnityEngine.Random.Range(0.0f, 1.0f),
              probBreak = UnityEngine.Random.Range(0.0f, 1.0f);
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
            flicker = LightIsOn(properties)
                && (
                    RollFlicker(probFlicker, sbl.probOfFlicker) ||
                    LastFlickerTooLongAgo(sbl, properties)
                   )
                && !OnCooldown(sbl, properties);

            if (!flicker)
            {
                for (int i = (int)TupleElem.BROKEN + 1; i < properties.Count; i++)
                {
                    List<string> otherProperties = bb.GetProperties(properties[i]);
                    if (otherProperties != null)
                        flicker &= !LightIsFlicker(otherProperties);
                }
            }

            flickerBroken = flicker && RollBreak(probBreak, sbl.probBreak);

            stopFlicker = LightIsFlicker(properties)
                && (
                    RollStopFlicker(probStopFlicker, sbl.probOfStopFlicker) ||
                    StartFlickerTooLongAgo(sbl, properties)
                   )
                && HasFlickeredMinTime(sbl, properties);

            stopFlickerBroken = stopFlicker
                && WillBreak(properties);

            // Add/Delete conditions from the BlackBoard
            //  Add condition: Changed STATE to ON, FLICKER, or DEAD
            //  Add condition: Is the light broken?
            if (flickerBroken)
            {
                nextState = (int)SmartBrokenLight.States.FLICKER;
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.BROKEN, "will break");
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.START_FLICKER, Time.time.ToString());
            }
            else if (flicker)
            {
                nextState = (int)SmartBrokenLight.States.FLICKER;
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.START_FLICKER, Time.time.ToString());
            }
            else if (stopFlickerBroken)
            {
                nextState = (int)SmartBrokenLight.States.DEAD;
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.BROKEN, "true");
            }
            else if (stopFlicker)
            {
                nextState = (int)SmartBrokenLight.States.ON;
            }

            if (LightIsFlicker(properties))
            {
                print(stopFlicker);
            }
            
            //print("Flicker: " + flicker);
            //print("StopFlicker: " + stopFlicker);

            if (nextState > -1)
            {
                bb.UpdateProperty(sbl.GetKey(), (int)TupleElem.STATE, nextState.ToString());

                // Perform some action, maybe change state
                // If not flickering, determine whether or not to start flickering
                // Otherwise, determine whether or not to stop flickering

                sbl.InduceTransition(nextState);
            }

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


    /* Pre-Conditions
     * 
     * All pre-condition checks, at the very least, take in a list of values and return whether or not the condition is met
     */
    // State conditions
    private bool LightIsOn(List<string> properties)
    {
        return int.Parse(properties[(int)TupleElem.STATE]) == (int)SmartBrokenLight.States.ON;
    }
    private bool LightIsFlicker(List<string> properties)
    {
        return int.Parse(properties[(int)TupleElem.STATE]) == (int)SmartBrokenLight.States.FLICKER;
    }
    private bool LightIsDead(List<string> properties)
    {
        return int.Parse(properties[(int)TupleElem.STATE]) == (int)SmartBrokenLight.States.DEAD;
    }

    // Check that the light hasn't flickered in the past lastFlicker seconds
    private bool LastFlickerTooLongAgo(SmartBrokenLight sbl, List<string> properties)
    {
        return Time.time - float.Parse(properties[(int)TupleElem.LAST_FLICKER]) > sbl.maxLastFlicker;
    }

    // Check tht the light has been flickering for flickerDuration seconds
    private bool StartFlickerTooLongAgo(SmartBrokenLight sbl, List<string> properties)
    {
        return Time.time - float.Parse(properties[(int)TupleElem.START_FLICKER]) > sbl.flickerDuration;
    }

    // Check that flickering isn't on cooldown
    private bool OnCooldown(SmartBrokenLight sbl, List<string> properties)
    {
        return Time.time - float.Parse(properties[(int)TupleElem.START_FLICKER]) < sbl.flickerCooldown;
    }

    // Check that the light has flickered for at least minFlickerDuration seconds
    private bool HasFlickeredMinTime(SmartBrokenLight sbl, List<string> properties)
    {
        return Time.time - float.Parse(properties[(int)TupleElem.START_FLICKER]) < sbl.minFlickerDuration;
    }

    // Check if the light will break
    private bool NotBroken(List<string> properties)
    {
        return properties[(int)TupleElem.BROKEN] == LIGHT_NOT_BROKEN;
    }
    private bool WillBreak(List<string> properties)
    {
        return properties[(int)TupleElem.BROKEN] == LIGHT_WILL_BREAK;
    }
    private bool Broken(List<string> properties)
    {
        return properties[(int)TupleElem.BROKEN] == LIGHT_IS_BROKEN;
    }

    // Probability checks
    private bool RollFlicker(float roll, float probOfFlicker)
    {
        return roll <= probOfFlicker;
    }
    private bool RollStopFlicker(float roll, float probOfStopFlicker)
    {
        return roll <= probOfStopFlicker;
    }
    private bool RollBreak(float roll, float probOfBreak)
    {
        return roll <= probOfBreak;
    }
}