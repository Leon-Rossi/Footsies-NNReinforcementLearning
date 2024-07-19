using UnityEngine;

namespace Footsies
{

    public class CameraController : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }

}