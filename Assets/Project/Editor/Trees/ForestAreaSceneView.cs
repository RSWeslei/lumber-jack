using System.Text;
using LumberJack.Trees;
using LumberJack.WorldTime;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Desenha as áreas de floresta na Scene view: a área (verde), as zonas sem muda (vermelho), cada árvore com o
    /// espaço que ela ocupa (verde adulta, amarelo-verde muda com o quanto cresceu, marrom toco com o quanto
    /// apodreceu), os anéis onde as adultas soltam mudas e um painel com os números da área. Liga e desliga pelo
    /// painel "Florestas" da Scene view; a área selecionada aparece sempre.
    /// </summary>
    [InitializeOnLoad]
    public static class ForestAreaSceneView
    {
        private const string ShowKey = "LumberJack.Forest.Show";
        private const string LabelsKey = "LumberJack.Forest.Labels";
        private const string RingsKey = "LumberJack.Forest.Rings";
        private const string ShowMenu = "Lumber Jack/Mostrar áreas de floresta";
        private const float LabelHeight = 1.5f;
        private const float FullPercent = 100f;
        private const float DiscFillAlpha = 0.15f;
        private const float VolumeAlpha = 0.25f;
        private const float OutlineThickness = 2f;

        private static readonly Color AreaFill = new(0.3f, 0.85f, 0.35f, 0.06f);
        private static readonly Color AreaOutline = new(0.3f, 0.85f, 0.35f, 0.9f);
        private static readonly Color ZoneFill = new(0.95f, 0.25f, 0.2f, 0.15f);
        private static readonly Color ZoneOutline = new(0.95f, 0.25f, 0.2f, 0.9f);
        private static readonly Color AdultColor = new(0.25f, 0.8f, 0.3f);
        private static readonly Color SaplingColor = new(0.75f, 0.95f, 0.3f);
        private static readonly Color StumpColor = new(0.65f, 0.42f, 0.22f);
        private static readonly Color RingColor = new(1f, 1f, 1f, 0.25f);
        private static readonly string[] SeasonNames = { "Primavera", "Verão", "Outono", "Inverno" };

        private static GUIStyle _labelStyle;
        private static GUIStyle _panelStyle;

        static ForestAreaSceneView()
        {
            SceneView.duringSceneGui += Draw;
        }

        public static bool IsShown
        {
            get => EditorPrefs.GetBool(ShowKey, true);
            set => EditorPrefs.SetBool(ShowKey, value);
        }

        public static bool ShowsLabels
        {
            get => EditorPrefs.GetBool(LabelsKey, true);
            set => EditorPrefs.SetBool(LabelsKey, value);
        }

        public static bool ShowsRings
        {
            get => EditorPrefs.GetBool(RingsKey, true);
            set => EditorPrefs.SetBool(RingsKey, value);
        }

        [MenuItem(ShowMenu)]
        private static void ToggleShown()
        {
            IsShown = !IsShown;
            SceneView.RepaintAll();
        }

        [MenuItem(ShowMenu, true)]
        private static bool ValidateToggleShown()
        {
            Menu.SetChecked(ShowMenu, IsShown);
            return true;
        }

        private static void Draw(SceneView sceneView)
        {
            foreach (ForestArea area in Object.FindObjectsByType<ForestArea>())
            {
                bool isSelected = Selection.activeGameObject == area.gameObject;
                if (!IsShown && !isSelected) continue;
                DrawArea(area);
            }
        }

        private static void DrawArea(ForestArea area)
        {
            EnsureStyles();
            ForestDefinition definition = area.Definition;
            Vector3 center = area.transform.position;
            Vector3 size = area.Size;
            float ground = center.y;

            Handles.DrawSolidRectangleWithOutline(Rectangle(center, size, ground), AreaFill, AreaOutline);
            Handles.color = new Color(AreaOutline.r, AreaOutline.g, AreaOutline.b, VolumeAlpha);
            Handles.DrawWireCube(center, size);

            foreach (NoPlantZone zone in area.GetComponentsInChildren<NoPlantZone>())
            {
                DrawZone(zone);
            }

            int adults = 0;
            int saplings = 0;
            int stumps = 0;
            if (area.TreesParent != null && definition != null)
            {
                foreach (ChoppableTree tree in area.TreesParent.GetComponentsInChildren<ChoppableTree>())
                {
                    if (!area.Contains(tree.transform.position)) continue;
                    switch (DrawTree(tree, definition))
                    {
                        case TreeState.Adult: adults++; break;
                        case TreeState.Sapling: saplings++; break;
                        default: stumps++; break;
                    }
                }
            }

            Vector3 corner = new(center.x - size.x * 0.5f, ground + LabelHeight, center.z + size.z * 0.5f);
            Handles.Label(corner, Summary(area, definition, adults, saplings, stumps), _panelStyle);
        }

        private static void DrawZone(NoPlantZone zone)
        {
            Vector3 half = zone.Size * 0.5f;
            Transform transform = zone.transform;
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            float bottom = -half.y;
            Vector3[] corners =
            {
                position + rotation * new Vector3(-half.x, bottom, -half.z),
                position + rotation * new Vector3(-half.x, bottom, half.z),
                position + rotation * new Vector3(half.x, bottom, half.z),
                position + rotation * new Vector3(half.x, bottom, -half.z)
            };
            Handles.DrawSolidRectangleWithOutline(corners, ZoneFill, ZoneOutline);
            if (ShowsLabels) Handles.Label(position + Vector3.up * LabelHeight, $"Sem mudas: {zone.name}", _labelStyle);
        }

        private static TreeState DrawTree(ChoppableTree tree, ForestDefinition definition)
        {
            Vector3 position = tree.transform.position;
            TreeState state;
            Color color;
            string text;
            if (tree.TryGetComponent(out StumpDecay decay))
            {
                state = TreeState.Stump;
                color = StumpColor;
                text = $"{tree.name}\ntoco: apodreceu {decay.Progress * FullPercent:0}%";
            }
            else if (tree.IsFelled)
            {
                state = TreeState.Stump;
                color = StumpColor;
                text = $"{tree.name}\ntoco";
            }
            else if (tree.TryGetComponent(out TreeGrowth growth))
            {
                state = TreeState.Sapling;
                color = SaplingColor;
                text = $"{tree.name}\nmuda: {growth.Growth * FullPercent:0}%";
            }
            else
            {
                state = TreeState.Adult;
                color = AdultColor;
                text = $"{tree.name}\nadulta";
            }

            Handles.color = new Color(color.r, color.g, color.b, DiscFillAlpha);
            Handles.DrawSolidDisc(position, Vector3.up, definition.MinSpacing);
            Handles.color = color;
            Handles.DrawWireDisc(position, Vector3.up, definition.MinSpacing, OutlineThickness);

            // Onde a adulta solta mudas: o anel entre a menor e a maior distância
            if (state == TreeState.Adult && ShowsRings)
            {
                Handles.color = RingColor;
                Handles.DrawWireDisc(position, Vector3.up, definition.SpreadDistance.Min);
                Handles.DrawWireDisc(position, Vector3.up, definition.SpreadDistance.Max);
            }

            if (ShowsLabels) Handles.Label(position + Vector3.up * LabelHeight, text, _labelStyle);
            return state;
        }

        private static string Summary(ForestArea area, ForestDefinition definition, int adults, int saplings, int stumps)
        {
            StringBuilder text = new();
            text.Append("<b>").Append(area.name).AppendLine("</b>");
            if (definition == null) return text.Append("Sem ForestDefinition").ToString();

            int total = adults + saplings + stumps;
            text.AppendLine($"{adults} adultas · {saplings} mudas · {stumps} tocos  ({total}/{definition.MaxTrees})");
            int current = -1;
            if (Application.isPlaying)
            {
                WorldClock clock = Object.FindAnyObjectByType<WorldClock>();
                if (clock != null) current = (int)clock.Season;
                text.AppendLine($"Rebrotas esperando: {area.PendingSproutCount}");
            }

            text.AppendLine("Chance por dia (mudas por árvore · sementes de fora):");
            for (int season = 0; season < SeasonNames.Length; season++)
            {
                string line = $"  {SeasonNames[season]}: {definition.ChanceFor((Season)season) * FullPercent:0}% · " +
                              $"{definition.OutsideChanceFor((Season)season) * FullPercent:0}%";
                text.AppendLine(season == current ? $"<color=#9CF07A>{line}  ← agora</color>" : line);
            }
            text.Append($"Espaço {definition.MinSpacing:0.#} m · mudas a {definition.SpreadDistance.Min:0.#}–" +
                        $"{definition.SpreadDistance.Max:0.#} m · coluna livre {definition.Clearance:0.#} × " +
                        $"{definition.ClearanceHeight:0.#} m");
            return text.ToString();
        }

        private static Vector3[] Rectangle(Vector3 center, Vector3 size, float y)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            return new[]
            {
                new Vector3(center.x - halfX, y, center.z - halfZ),
                new Vector3(center.x - halfX, y, center.z + halfZ),
                new Vector3(center.x + halfX, y, center.z + halfZ),
                new Vector3(center.x + halfX, y, center.z - halfZ)
            };
        }

        private static void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true,
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _panelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true,
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = Color.white }
            };
        }

        private enum TreeState
        {
            Adult,
            Sapling,
            Stump
        }
    }

    /// <summary>Painel "Florestas" na Scene view: liga e desliga o desenho das áreas, rótulos e anéis.</summary>
    [Overlay(typeof(SceneView), "lumberjack-forests", "Florestas", true)]
    public class ForestAreaOverlay : IMGUIOverlay
    {
        public override void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            ForestAreaSceneView.IsShown = EditorGUILayout.ToggleLeft("Mostrar áreas de floresta", ForestAreaSceneView.IsShown);
            using (new EditorGUI.DisabledScope(!ForestAreaSceneView.IsShown))
            {
                ForestAreaSceneView.ShowsLabels = EditorGUILayout.ToggleLeft("Rótulos das árvores", ForestAreaSceneView.ShowsLabels);
                ForestAreaSceneView.ShowsRings = EditorGUILayout.ToggleLeft("Onde as adultas soltam mudas", ForestAreaSceneView.ShowsRings);
            }
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        }
    }
}
