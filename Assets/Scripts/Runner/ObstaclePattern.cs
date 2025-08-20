using UnityEngine;

namespace BananaRun.Runner
{
    [System.Serializable]
    public class ObstacleData
    {
        [Tooltip("상대적 Z 위치 (패턴 시작점 기준)")]
        public float relativeZ = 0f;
        
        [Tooltip("레인 번호 (0=왼쪽, 1=가운데, 2=오른쪽)")]
        public int laneIndex = 1;
        
        [Tooltip("장애물 타입 인덱스")]
        public int obstacleTypeIndex = 0;
    }

    [CreateAssetMenu(fileName = "New Obstacle Pattern", menuName = "BananaRun/Obstacle Pattern")]
    public class ObstaclePattern : ScriptableObject
    {
        [Header("Pattern Info")]
        public string patternName = "기본 패턴";
        
        [Tooltip("패턴 전체 길이 (Z축)")]
        public float patternLength = 20f;
        
        [Header("Obstacles")]
        public ObstacleData[] obstacles;
        
        [Header("Spawn Settings")]
        [Tooltip("이 패턴이 스폰될 확률 가중치")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;
        
        [Tooltip("최소 난이도 (가상 거리 기준)")]
        public float minDifficulty = 0f;
        
        [Tooltip("최대 난이도 (이후로는 스폰 안 됨)")]
        public float maxDifficulty = 1000f;
    }
}
