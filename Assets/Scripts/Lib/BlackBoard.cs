using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/* Emulates a tuple space using a dictionary
 */
public class BlackBoard : MonoBehaviour
{
	// Main dictionary that stores tuples as a name indexed to a List<string> of properties
	private Dictionary<string, List<string>> flags = new Dictionary<string, List<string>>();
	
	// A list of SmartObjects that can interact with each other
	private Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();
	
	
	/* Initialization
	 */
	// Adds a SmartObject or a key to a dictionary of objects, and adds all properties that the BlackBoard can manage to the dictionary of flags
	public string Register(GameObject smartObj, List<string> properties, string key = null)
	{
        // Can't do anything if no object or key was given
        if (smartObj == null && key == null)
            return null;
        else
        {
            // Generate a new key if none is provided or if the key provided is already in the dictionary
            if (key != null && flags.ContainsKey(key) == false && objects.ContainsKey(key) == false)
            {
                objects.Add(key, smartObj);
                flags.Add(key, properties);

                return key;
            }
            else
            {
                string newKey = smartObj.GetHashCode().ToString();

                // The only thing that can cause a failure here is if the hash has already been added (no object can be added more than once)
                if (flags.ContainsKey(newKey) == false && objects.ContainsKey(newKey) == false)
                {
                    objects.Add(newKey, smartObj);
                    flags.Add(newKey, properties);

                    return smartObj.GetHashCode().ToString();
                }
                else
                    return null;
            }
        }
	}
    public string Register(string key, List<string> properties)
    {
        // Can't do anything if no object or key was given
        if (key == null)
            return null;
        else
        {
            // Generate a new key if none is provided or if the key provided is already in the dictionary
            if (key != null && flags.ContainsKey(key) == false && objects.ContainsKey(key) == false)
            {
                // Note that no object is added if no key is provided
                flags.Add(key, properties);
                return key;
            }
        }

        // Note that nothing happens if the key already exists in the dictionary
        return null;
    }


    /* Base functionality
	 */
    // Checks that a key exists in the dictionary
    public bool Exists(string key)
	{
		List<string> test;
		return flags.TryGetValue(key, out test);
	}

    // Gets properties by key
    public List<string> GetProperties(string key)
    {
        List<string> properties;
        if (flags.TryGetValue(key, out properties))
        {
            return properties;
        }

        return null;
    }

    // Update a property by key, index, and value
    public bool UpdateProperty(string key, int index, string value)
    {
        List<string> properties;
        if (flags.TryGetValue(key, out properties))
        {
            properties[index] = value;
            return true;
        }

        return false;
    }
	
	// Checks that a key maps to the given values
	public bool IsMatch(string key, List<string> values)
	{
		List<string> test;
		if (flags.TryGetValue(key, out test))
		{
			if (values.Count != test.Count)
				return false;
			
			for (int i = 0; i < values.Count; i++)
			{
				if (test[i] != values[i])
					return false;
			}
			
			return true;
		}
		
		return false;
	}
	
	// Add a key to the dictionary
	public void AddProperty(string key, List<string> values)
	{
		flags.Add(key, values);
	}
	
	// Remove a key from the dictionary
	public bool RemoveProperty(string key)
	{
		return flags.Remove(key);
	}
}