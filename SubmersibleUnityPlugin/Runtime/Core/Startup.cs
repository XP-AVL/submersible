using System;
using System.Collections;
using UnityEngine;

namespace Submersible.Runtime.Core
{
    /// <summary>
    /// Starts up the experience and goes away.
    /// </summary>
    public class Startup : MonoBehaviour
    {
        [SerializeField] private Loader[] loaders;

        private void Awake()
        {
            StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            // Ensure we don't get destroyed on a scene load.
            DontDestroyOnLoad(gameObject);
            
            // Load the loaders in order
            foreach (var loader in loaders)
            {
                // Load
                loader.Load();
                
                // Wait
                yield return new WaitWhile(() => loader.Status == Loader.LoadingStatus.Loading);
                
                // Continue or abort
                switch (loader.Status)
                {
                    case Loader.LoadingStatus.Loaded:
                    case Loader.LoadingStatus.LoadingSkipped:
                        break;
                    case Loader.LoadingStatus.None:
                    case Loader.LoadingStatus.Loading:
                    case Loader.LoadingStatus.FailedToLoad:
                        Debug.LogWarning($"Loading aborted at {loader.name} with status {loader.Status}.");
                        CleanUp();
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            CleanUp();
        }

        private void CleanUp()
        {
            Destroy(gameObject);
        }
    }
}
