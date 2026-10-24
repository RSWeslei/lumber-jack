using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// A Game view ignora o vSync do projeto e tem um toggle proprio ("VSync (Game view only)"),
    /// desligado por padrao. Sem ele o Play roda sem sincronia com o monitor e a camera da
    /// micro-travadas. Liga o toggle em todas as Game views ao entrar no Play.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewVSync
    {
        private const string GameViewTypeName = "UnityEditor.GameView";
        // Propriedade interna do Unity (sem API publica); se mudar de nome, so avisa no Console
        private const string VSyncPropertyName = "vSyncEnabled";
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static GameViewVSync()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            System.Type gameViewType = typeof(EditorWindow).Assembly.GetType(GameViewTypeName);
            PropertyInfo vSync = gameViewType != null ? gameViewType.GetProperty(VSyncPropertyName, InstanceMembers) : null;
            if (vSync == null || !vSync.CanWrite)
            {
                Debug.LogWarning("Nao consegui ligar o VSync da Game view; ligue a mao no menu da Game view.");
                return;
            }

            foreach (Object gameView in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                vSync.SetValue(gameView, true);
            }
        }
    }
}
