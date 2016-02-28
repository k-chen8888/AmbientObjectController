using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/* A "broken" light that flickers every once in a while
 *
 * The probability of flickering is dependent on
 *  (1) Whether nearby lights have just flickered
 *  (2) How long it has been since any light flickered
 *
 * Sometimes, a light can completely go out with some probability, after which it will remain off and never flicker
 */
public class SmartBrokenLight : AmbientObject
{
    // States that the light can be in
    public enum States { ON, DEAD, FLICKER };

    // Max time since last flicker
    public float maxLastFlicker = 3.0f,
                 flickerDuration = 3.0f;

    // How long to wait between flickers
    public float maxFlickerWait = 0.25f;

    // Probability of flickering or reverting to ON
    public float probOfFlicker = 0.1f,
                 probOfStopFlicker = 0.1f;

    // Probability fo breaking
    public float probBreak = 0.1f;

    // Reference to the light
    private Light mainLight;

    // Neighboring lights
    public GameObject[] neighbors;


    // Use this for initialization
    void Start () {
        mainLight = GetComponent<Light>();
        
        // Start on the starting state
        currState = startState;
        SetInitData();

        // Add new states and transitions
        AddNewStates();
        AddNewTransitions();

        // Set the light to its starting state
        IEnumerator next;
        SafeStartCoroutine(states.TryGetValue(startState, out next) ? next : LightOn());

        // Add to BlackBoard
        RegisterToBlackBoard();
    }
	
	// Update is called once per frame
	void Update () {
        // Try and flicker
        strips[selfKey].Action(self);
    }


    /* States */
    protected IEnumerator LightOn()
    {
        while (true)
        {
            if (currState == (int)States.ON)
            {
                mainLight.enabled = true;
                yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    protected IEnumerator LightFlicker()
    {
        float nextChange = Time.time;

        while (true)
        {
            if (currState == (int)States.FLICKER)
            {
                // Just keep on keeping on...
                if (Time.time > nextChange)
                {
                    mainLight.enabled = !mainLight.enabled;
                    nextChange += Random.Range(0, maxFlickerWait);
                }
                yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    protected IEnumerator LightDead()
    {
        while (true)
        {
            if (currState == (int)States.DEAD)
            {
                // Can't go anywhere anymore, also the light is permanently off
                mainLight.enabled = false;
                yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    // Because a custom starting configuration was defined, need to override LeaveBadState()
    protected override IEnumerator LeaveBadState()
    {
        StopAllCoroutines();
        currState = nextState;
        ResetObject();

        IEnumerator next;
        if (states.TryGetValue(nextState, out next))
        {
            currState = nextState;
            SafeStartCoroutine(next);
        }
        else
        {
            currState = startState;
            SafeStartCoroutine(LightOn());
        }

        nextState = NOT_A_STATE;
        yield break;
    }

    // Externally generate a transition to the next state
    public bool InduceTransition(int next)
    {
        nextState = next;
        if (ExistsTransition(nextState))
        {
            IEnumerator coroutine;

            // Start the next state
            if (states.TryGetValue(nextState, out coroutine))
            {
                // Complete change to state
                currState = nextState;
                nextState = NOT_A_STATE;
                SafeStartCoroutine(coroutine);
                return true;
            }
        }

        // Whoops! Try to recover...
        nextState = startState;
        SafeStartCoroutine(LeaveBadState());
        return false;
    }


    /* Utilities
     */
    // Override to add actual information to the BlackBoard
    protected override bool RegisterToBlackBoard()
    {
        // Get basic properties
        List<string> properties = new List<string>
        {
            currState.ToString(),
            Time.deltaTime.ToString(),
            Time.deltaTime.ToString(),
            "false"
        };

        // Get names of neighbors
        foreach (GameObject n in neighbors)
        {
            properties.Add(n.GetComponent<SmartBrokenLight>().objectName));
        }

        // Attempt to register the object
        selfKey = bb.Register(self, properties, objectName);
        return selfKey != null;
    }
    
    // Check whether or not this object is overdue for a flicker
    public bool OverdueForFlicker(float lastFlicker)
    {
        return ((Time.time - lastFlicker) > maxLastFlicker);
    }

    // Check whether or not this object is overdue for the flicker to stop
    public bool OverdueForFlickerStop(float lastFlicker)
    {
        return ((Time.time - lastFlicker) > flickerDuration);
    }

    // Override to add new states to the FSM
    protected override void AddNewStates()
    {
        // Remove the old default start state
        states.Remove(INIT_STATE);

        // Replace the old default LeaveBadState()
        states.Remove(BAD_STATE);
        states.Add(BAD_STATE, LeaveBadState());

        // Add the new states
        states.Add((int)States.ON, LightOn());
        states.Add((int)States.FLICKER, LightFlicker());
        states.Add((int)States.DEAD, LightDead());
    }

    // Override to add new transitions to the FSM
    protected override void AddNewTransitions()
    {
        /* All possible transitions:
         *  The light can switch between being ON or FLICKER
         *      (ON, FLICKER)
         *      (FLICKER, ON)
         *
         *  A light in FLICKER may die
         *      (FLICKER, DEAD)
         */

        transitions.Add((int)States.ON, new List<int>(new int[] {
            (int)States.FLICKER
        }));

        transitions.Add((int)States.FLICKER, new List<int>(new int[] {
            (int)States.ON,
            (int)States.DEAD
        }));
    }

    // Override the reset function to also turn the light back on
    protected override void SetInitData()
    {
        base.SetInitData();
        mainLight.enabled = true;
    }
}
