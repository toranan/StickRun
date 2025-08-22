using UnityEngine;

namespace BananaRun.Runner
{
    // 아이템의 종류를 구분하기 위한 열거형
    public enum ItemType
    {
        Coin,       // 점수를 주는 코인
        Magnet,     // 자석 아이템
        Invincible, // 무적 아이템
        SpeedBoost, // 속도 증가 아이템
        Rocket,     // 5초간 공중 비행 아이템
        Slowdown    // 5초간 속도 감소 아이템
    }

    [RequireComponent(typeof(Collider))]
    public class Item : MonoBehaviour
    {
        public ItemType itemType;
        public AudioClip collectionSound; // 아이템 획득 시 재생할 사운드

        private void Awake()
        {
            // 플레이어가 통과할 수 있도록, 콜라이더를 반드시 트리거(Trigger)로 설정합니다.
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 부딪힌 오브젝트가 RunnerPlayer 컴포넌트를 가졌는지 확인합니다.
            var player = other.GetComponent<RunnerPlayer>();
            if (player != null && !player.isDead)
            {
                // GameManager에 아이템 정보와 사운드를 함께 전달합니다.
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnItemCollected(itemType, collectionSound, gameObject);
                }
            }
        }
    }
}
