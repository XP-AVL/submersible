using UnityEngine;

namespace Submersible.Runtime.Spawners
{
    /// <summary>
    /// Spawn an object repeatedly within a provided range of the spawner
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        /// <summary>
        /// The prefab to spawn
        /// </summary>
        [SerializeField] private GameObject prefabToSpawn;
        
        /// <summary>
        /// The interval at which to spawn the objects
        /// </summary>
        [SerializeField] private float spawnInterval = 2f;

        /// <summary>
        /// The volume in which to spawn the objects
        /// </summary>
        [SerializeField] private Vector3 spawnRange = Vector3.one;
    
        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer < spawnInterval)
            {
                return;
            }
            
            SpawnObject();
            _timer = 0f;
        }

        private void SpawnObject()
        {
            // Generate random position within the spawn area
            var randomPosition = GetRandomSpawnPosition();
        
            // Spawn at random position
            Instantiate(prefabToSpawn, randomPosition, transform.rotation);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Get a random position in local space
            var localPos = new Vector3(
                Random.Range(-spawnRange.x, spawnRange.x),
                Random.Range(-spawnRange.y, spawnRange.y),
                Random.Range(-spawnRange.z, spawnRange.z));
            
            // Return the position in world space
            return transform.TransformPoint(localPos);
        }
    
        // Visualize the spawn area in the Scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, spawnRange * 2f);
        }
    }
}