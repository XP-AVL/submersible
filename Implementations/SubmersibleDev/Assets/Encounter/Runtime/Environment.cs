using UnityEngine;

namespace Encounter.Runtime
{
    public class Environment : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawnPoint;
        
        public Transform PlayerSpawnPoint => playerSpawnPoint;
    }
}