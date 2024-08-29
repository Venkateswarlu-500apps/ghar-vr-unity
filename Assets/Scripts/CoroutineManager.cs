using UnityEngine;
using System.Collections; // Import the namespace required for IEnumerator

public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager _instance;

    // Singleton Instance
    public static CoroutineManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Create a new GameObject if the instance doesn't exist
                GameObject obj = new GameObject("CoroutineManager");
                _instance = obj.AddComponent<CoroutineManager>();
                DontDestroyOnLoad(obj); // Make sure this object persists across scene loads
            }
            return _instance;
        }
    }

    // Method to start a coroutine from the CoroutineManager
    public static void StartManagedCoroutine(IEnumerator coroutine)
    {
        Instance.StartCoroutine(coroutine);
    }
}
