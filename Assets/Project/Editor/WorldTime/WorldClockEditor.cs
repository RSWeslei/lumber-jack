using LumberJack.WorldTime;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>Botões para pular o tempo no Play (testar nascer/pôr do sol, meses e estações).</summary>
    [CustomEditor(typeof(WorldClock))]
    public class WorldClockEditor : UnityEditor.Editor
    {
        private const double HoursPerDay = 24.0;
        private const int MonthsPerSeason = 3;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;

            var clock = (WorldClock)target;
            GameDate date = clock.Date;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Agora", $"{date.HourOfDay:00}:{date.Minute:00}, dia {date.Day + 1}/{date.Month + 1}, ano {date.Year}");
            EditorGUILayout.LabelField("Estação e fase", $"{clock.Season} · {clock.Phase} · sol {clock.SunElevation:0}°");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 hora")) clock.AdvanceHours(1.0);
            if (GUILayout.Button("+6 horas")) clock.AdvanceHours(6.0);
            if (GUILayout.Button("+1 dia")) clock.AdvanceHours(HoursPerDay);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 mês")) clock.AdvanceHours(HoursPerDay * date.DaysPerMonth);
            if (GUILayout.Button("+1 estação")) clock.AdvanceHours(HoursPerDay * date.DaysPerMonth * MonthsPerSeason);
            if (GUILayout.Button(clock.IsPaused ? "Continuar" : "Pausar")) clock.SetPaused(!clock.IsPaused);
            EditorGUILayout.EndHorizontal();
            // Mantém os valores de cima andando com o tempo
            Repaint();
        }
    }
}
