using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Sem vSync o jogo roda centenas de FPS a mais que o monitor e que o mouse envia leituras:
    /// muitos frames chegam sem movimento do mouse e a camera gira "aos trancos".
    /// Com vSync desligado, limita o FPS a taxa de atualizacao do monitor.
    /// </summary>
    public static class FramePacing
    {
        private const int FallbackFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // No Editor a Game view ignora o vSync do projeto (tem um toggle proprio), entao limita sempre
#if !UNITY_EDITOR
            if (QualitySettings.vSyncCount > 0) return;
#endif

            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = refreshRate > 0 ? Mathf.RoundToInt((float)refreshRate) : FallbackFrameRate;
        }
    }
}
