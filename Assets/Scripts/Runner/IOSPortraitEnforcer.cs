using UnityEngine;

namespace BananaRun.Runner
{
    public static class IOSPortraitEnforcer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ForcePortrait()
        {
#if UNITY_IOS || UNITY_EDITOR
            // 세로 고정 및 자동 회전 제한
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false; // 필요 시 true
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;

            // 권장 런타임 품질 설정
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        }
    }
}


