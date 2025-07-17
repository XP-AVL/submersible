using UnityEngine;
using UnityEngine.SceneManagement;

namespace Encounter.Runtime
{
    /// <summary>
    /// Starts up the experience and goes away.
    /// </summary>
    public class Startup : MonoBehaviour
    {
        [SerializeField] private string environmentSceneName;
        [SerializeField] private Player playerPrefab;
        
        private void OnEnable()
        {
            DontDestroyOnLoad(gameObject);
            
            // Load the environment
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(environmentSceneName, LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name != environmentSceneName)
            {
                Debug.LogWarning($"SceneManager.OnSceneLoaded: Unexpected scene loaded: {scene.name}");
                return;
            }
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Find the Player Spawn, if there is one
            var playerSpawn = FindFirstObjectByType<PlayerSpawn>();

            if (playerSpawn == null)
            {
                Debug.LogWarning("Environment not found in scene. Using default environment values.");
            }
            
            // Spawn the player
            var playerSpawnPos = playerSpawn ? playerSpawn.transform.position : Vector3.zero;
            var playerSpawnRot = playerSpawn ? playerSpawn.transform.rotation : Quaternion.identity;
            Instantiate(playerPrefab, playerSpawnPos, playerSpawnRot);
            
            // Clean up
            Destroy(gameObject);
        }
    }
}
