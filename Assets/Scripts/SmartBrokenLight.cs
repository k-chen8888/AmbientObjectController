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

    // Information about STRIP
    public string flickerStripName = "";

    // Max time since last flicker
    public float maxLastFlicker = 5.0f,
                 flickerDuration = 2.0f;

    // How long to wait between flickers
    public float maxFlickerWait = 0.75f,
                 flickerCooldown = 3.0f,
                 minFlickerDuration = 1.0f;

    // Probability of flickering or reverting to ON
    public float probOfFlicker = 0.2f,
                 probOfStopFlicker = 0.1f;

    // Probability of breaking
    public float probBreak = 0.05f;

    // Reference to the light
    private Light mainLight = null;

    // Neighboring lights
    public GameObject[] neighbors;


    // Use this for initialization
    void Start () {
        // Start on the starting state
        currState = startState;
        SetInitData();

        // Add new states and transitions
        AddNewStates();
        AddNewTransitions();

        // Set the light to its starting state
        if (!SafeStartCoroutine(startState, null))
            StartCoroutine(LightOn());


        // Add to BlackBoard
        registered = RegisterToBlackBoard();
    }
	
	// Update is called once per frame
	void Update () {
        // Try and flicker
        strips[flickerStripName].Action(self);
        print(currState);
    }


    /* States */
    protected IEnumerator LightOn(string[] args = null)
    {
        while (currState == (int)States.ON)
        {
            mainLight.enabled = true;
            yield return null;
        }

        yield break;
    }

    protected IEnumerator LightFlicker(string[] args = null)
    {
        float nextChange = Time.time;
        
        while (currState == (int)States.FLICKER)
        {
            // Just keep on keeping on...
            if (Time.time >= nextChange)
            {
                mainLight.enabled = !mainLight.enabled;
                nextChange += Random.Range(0.0f, maxFlickerWait);
            }
            yield return null;
        }

        yield break;
    }

    protected IEnumerator LightDead(string[] args = null)
    {
        while (currState == (int)States.DEAD)
        {
            // Can't go anywhere anymore, also the light is permanently off
            mainLight.enabled = false;
            yield return null;
        }

        yield break;
    }

    // Because a custom starting configuration was defined, need to override LeaveBadState()
    protected override IEnumerator LeaveBadState(string[] args = null)
    {
        StopAllCoroutines();
        currState = nextState;
        ResetObject();
        
        if (SafeStartCoroutine(nextState))
            currState = nextState;
        else
            StartCoroutine(LightOn());

        nextState = NOT_A_STATE;
        yield break;
    }

    // Externally generate a transition to the next state
    public override bool InduceTransition(int next)
    {
        nextState = next;
        // Start the next state
        if (ExistsTransition(nextState))
        {
            // Complete change to state
            currState = next;
            if (SafeStartCoroutine(currState))
            {
                nextState = NOT_A_STATE;
                return true;
            }
        }

        // Whoops! Try to recover...
        nextState = startState;
        StartCoroutine(LeaveBadState(null));
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
            properties.Add(n.GetComponent<SmartBrokenLight>().objectName);
        }

        // Attempt to register the object
        selfKey = bb.Register(self, properties, objectName);
        return selfKey != null;
    }

    // Override to add new states to the FSM
    protected override void AddNewStates()
    {
        // Remove the old default start state
        states.Remove(INIT_STATE);

        // Replace the old default LeaveBadState()
        states.Remove(BAD_STATE);
        states.Add(BAD_STATE, LeaveBadState);

        // Add the new states
        states.Add((int)States.ON, LightOn);
        states.Add((int)States.FLICKER, LightFlicker);
        states.Add((int)States.DEAD, LightDead);
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

        if (mainLight == null)
            mainLight = GetComponent<Light>();

        mainLight.enabled = true;
    }
}
