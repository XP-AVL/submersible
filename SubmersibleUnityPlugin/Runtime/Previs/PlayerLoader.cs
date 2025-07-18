using Submersible.Runtime.Core;
using UnityEngine;

namespace Submersible.Runtime.Previs
{
    /// <summary>
    /// Handles the loading and instantiation of player-related prefabs in the scene,
    /// based on the specified player type configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerLoader_", menuName = "Submersible/Previs/Player Loader")]
    public class PlayerLoader : Loader
    {
        private enum PlayerType
        {
            None,
            XR,
            Desktop
        }
        
        [SerializeField] private PlayerType playerType = PlayerType.Desktop;
        [SerializeField] private GameObject xrPlayerPrefab;
        [SerializeField] private GameObject desktopPlayerPrefab;
        
        public override void Load()
        {
            if (playerType == PlayerType.None)
            {
                Status = LoadingStatus.LoadingSkipped;
                return;
            }
            
            // Find the spawn point if it exists
            var spawnPoint = FindFirstObjectByType<PlayerSpawn>();
            
            // Spawn the player
            var pos = spawnPoint ? spawnPoint.transform.position : Vector3.zero;
            var rot = spawnPoint ? spawnPoint.transform.rotation : Quaternion.identity;
            var prefab = playerType == PlayerType.XR ? xrPlayerPrefab : desktopPlayerPrefab;
            Instantiate(prefab, pos, rot);
            
            Status = LoadingStatus.Loaded;
        }
    }
}