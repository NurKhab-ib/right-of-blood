using UnityEngine;

namespace RightOfBlood.Prototype
{
    public sealed class PrototypeSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id = "default";
        public string Id => id;
    }
}
