using UnityEngine;

namespace RightOfBlood.Prototype
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id = "default";
        public string Id => id;
    }
}
