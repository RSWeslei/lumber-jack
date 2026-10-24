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
            // O limite por targetFrameRate e feito por software e da micro-travadas; so serve de reserva
            // quando nao ha vSync. No Editor o vSync e o da Game view (LumberJack.Editor.GameViewVSync)
            if (QualitySettings.vSyncCount > 0) return;

            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = refreshRate > 0 ? Mathf.RoundToInt((float)refreshRate) : FallbackFrameRate;
        }
    }
}
