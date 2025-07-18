using UnityEngine;

namespace Encounter.Runtime.Environments
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefabToSpawn;
        [SerializeField] private float spawnInterval = 2f;
    
        [Header("Random Spawn Area")]
        //The spawn area is a in a range around the JellySpawner object.
        [SerializeField] private float spawnRangeX = 50f;
        [SerializeField] private float spawnRangeZ = 50f;
        [SerializeField] private float spawnHeight = -50f;
    
        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
        
            if (_timer >= spawnInterval)
            {
                SpawnObject();
                _timer = 0f;
            }
        }

        private void SpawnObject()
        {
            // Generate random position within the spawn area
            Vector3 randomPosition = GetRandomSpawnPosition();
        
            // Spawn at random position
            Instantiate(prefabToSpawn, randomPosition, transform.rotation);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Random X and Z coordinates around the spawner's position
            float randomX = transform.position.x + Random.Range(-spawnRangeX, spawnRangeX);
            float randomZ = transform.position.z + Random.Range(0, spawnRangeZ);
        
            return new Vector3(randomX, transform.position.y + spawnHeight, randomZ);
        }
    
        // Optional: Manual spawn function you can call from other scripts
        public void SpawnNow()
        {
            SpawnObject();
        }
    
        // Visualize the spawn area in the Scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, 
                new Vector3(spawnRangeX * 2, 0.1f, spawnRangeZ * 2));
        }
    }
}