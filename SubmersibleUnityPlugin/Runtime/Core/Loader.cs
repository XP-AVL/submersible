using UnityEngine;

namespace Submersible.Runtime.Core
{
    /// <summary>
    /// Something that loads something, usually on startup.
    /// </summary>
    public abstract class Loader : ScriptableObject
    {
        /// <summary>
        /// Represents the status of a loading operation, indicating whether it was successful or not.
        /// </summary>
        public enum LoadingStatus
        {
            None,
            Loading,
            Loaded,
            FailedToLoad,
            LoadingSkipped
        }

        /// <summary>
        /// Gets the current status of the loading operation.
        /// Indicates whether the operation is successfully completed or has failed.
        /// </summary>
        public LoadingStatus Status { get; protected set; } = LoadingStatus.None;

        /// <summary>
        /// Loads the resource or performs the designated operation.
        /// </summary>
        public abstract void Load();
    }
}