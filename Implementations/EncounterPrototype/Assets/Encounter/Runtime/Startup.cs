using UnityEngine;

namespace Encounter.Runtime
{
    public class Startup : MonoBehaviour
    {
        [SerializeField] private Environment environmentPrefab;
        [SerializeField] private Player playerPrefab;

        private Environment _environment;
        private Player _player;
        
        private void OnEnable()
        {
            _environment = Instantiate(environmentPrefab);
            _player = Instantiate(
                playerPrefab, 
                _environment.PlayerSpawnPoint.position, 
                _environment.PlayerSpawnPoint.rotation);
        }

        private void OnDisable()
        {
            Destroy(_environment);
            Destroy(_player);
        }
    }
}
