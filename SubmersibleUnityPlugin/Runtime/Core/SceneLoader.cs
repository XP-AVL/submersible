using UnityEngine;
using UnityEngine.SceneManagement;

namespace Submersible.Runtime.Core
{
    /// <summary>
    /// Handles loading of Unity scenes asynchronously with a designated loading mode.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneLoader_", menuName = "Submersible/Core/Scene Loader")]
    public class SceneLoader : Loader
    {
        [SerializeField] private string sceneName;
        [SerializeField] private LoadSceneMode loadSceneMode;

        public override void Load()
        {
            Status = LoadingStatus.Loading;
            
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

            if (asyncOperation == null)
            {
                Status = LoadingStatus.FailedToLoad;
                return;
            }

            asyncOperation.completed += (_) =>
            {
                var loadedScene = SceneManager.GetSceneByName(sceneName);
                Status = loadedScene.isLoaded ? LoadingStatus.Loaded : LoadingStatus.FailedToLoad;
            };
        }
    }
}