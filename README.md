# StickRun (BananaRun)

Unity 기반 3D 러너 게임 프로젝트입니다.

## 주요 기능
- 다양한 아이템: 코인, 자석, 무적, 스피드 부스트, 로켓, 슬로우다운
- 각 아이템별 시각적 이펙트(프리팹) 지원
- 장애물/아이템이 멀리서 미리 보이도록 스폰 거리 조정
- 점수 및 UI 관리
- 게임 오버 및 재시작 기능

## 폴더 구조
- `Assets/Scripts/Runner/` : 주요 게임 로직 스크립트
- `Assets/Scenes/` : 게임 씬
- `Assets/Prefabs/` : 아이템/이펙트 프리팹
- `Assets/Resources/` : 리소스 파일

## 실행 방법
1. Unity로 프로젝트 폴더를 엽니다.
2. `RunnerSample` 씬을 실행합니다.
3. Inspector에서 각 아이템/플레이어에 이펙트 프리팹을 할당할 수 있습니다.

## 커스텀 설정
- 장애물/아이템 스폰 거리: `ObstacleSpawner`의 `spawnAheadDistance` 값 조정
- 각 아이템 효과/이펙트: `GameManager`, `RunnerPlayer` 스크립트 참고

## 기여
PR 및 이슈 환영합니다!

## 라이선스
MIT License
