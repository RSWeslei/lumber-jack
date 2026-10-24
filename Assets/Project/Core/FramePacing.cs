using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Sem vSync o jogo roda centenas de FPS a mais que o monitor e que o mouse envia leituras:
    /// muitos frames chegam sem movimento do mouse e a câmera gira "aos trancos".
    /// Com vSync desligado, limita o FPS à taxa de atualização do monitor.
    /// </summary>
    public static class FramePacing
    {
        private const int FallbackFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // O limite por targetFrameRate é feito por software e dá micro-travadas; só serve de reserva
            // quando não há vSync. No Editor o vSync é o da Game view (LumberJack.Editor.GameViewVSync)
            if (QualitySettings.vSyncCount > 0) return;

            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = refreshRate > 0 ? Mathf.RoundToInt((float)refreshRate) : FallbackFrameRate;
        }
    }
}
