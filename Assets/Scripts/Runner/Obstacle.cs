using UnityEngine;

namespace BananaRun.Runner
{
    public class Obstacle : MonoBehaviour
    {
        public Vector3 size = new Vector3(1.2f, 1.2f, 1.2f);

        private void Awake()
        {
            // 보장: BoxCollider 존재 및 트리거 설정
            var col = GetComponent<Collider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            // 정밀 충돌 판정을 위해 트리거로 설정 (Physics.OverlapCapsule에서 감지)
            col.isTrigger = true;
        }
    }
}


