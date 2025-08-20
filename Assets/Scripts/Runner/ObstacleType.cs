using UnityEngine;

namespace BananaRun.Runner
{
    [System.Serializable]
    public class ObstacleType
    {
        [Header("Basic Info")]
        public string name = "기본 장애물";
        public GameObject prefab; // 프리팹 사용 시
        
        [Header("Procedural Generation")]
        public bool useProcedural = true; // 프리팹 없으면 자동 생성
        public Vector3 size = new Vector3(1.2f, 1.2f, 1.2f);
        public Color color = Color.red;
        
        [Header("Spawn Rules")]
        [Tooltip("어느 레인에 생성될 수 있는지 (0=왼쪽, 1=가운데, 2=오른쪽)")]
        public int[] allowedLanes = { 0, 1, 2 };
        
        [Tooltip("슬라이드로 피할 수 있는가?")]
        public bool canSlideUnder = false;
        
        [Tooltip("점프로 피할 수 있는가?")]
        public bool canJumpOver = true;
        
        [Tooltip("스폰 확률 가중치 (높을수록 자주 나타남)")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;
    }
}
