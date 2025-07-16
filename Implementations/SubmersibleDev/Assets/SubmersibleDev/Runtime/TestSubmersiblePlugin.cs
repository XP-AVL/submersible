using UnityEngine;

namespace SubmersibleDev.Runtime
{
    public class TestSubmersiblePlugin : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.LogWarning(Submersible.Runtime.Submersible.Test());
        }
    }
}
