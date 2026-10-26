using System.Collections.Generic;
using LumberJack.Core;
using LumberJack.WorldTime;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Floresta que se renova, uma vez por dia do jogo: árvores adultas soltam mudas da mesma espécie perto delas,
    /// sementes de fora fazem nascer espécies da lista em qualquer lugar livre (nenhuma espécie some de vez) e tocos
    /// de espécies que rebrotam soltam um broto ao lado. Árvore derrubada vira toco que apodrece e libera o lugar.
    /// Muda só nasce em chão da camada Ground, com uma coluna livre em cima e fora das zonas proibidas (filhos
    /// <see cref="NoPlantZone"/>). Os sorteios usam a seed da área e o número do dia: todos os clientes veem as mesmas mudas.
    /// </summary>
    public class ForestArea : MonoBehaviour
    {
        private const string WoodLayerName = "Wood";
        private const string GroundLayerName = "Ground";
        // A coluna livre começa um pouco acima do chão para não encostar nele
        private const float ClearanceLift = 0.05f;
        private const float GroundProbeHeight = 30f;
        private const int MaxOverlaps = 16;
        private const string SaplingNameFormat = "{0} Sapling {1}";
        private const string SproutNameFormat = "{0} Sprout {1}";
        // O broto nasce na borda do toco, um pouco para dentro
        private const float SproutEdgeFraction = 0.8f;

        [SerializeField] private WorldClock _clock;
        [SerializeField] private ForestDefinition _definition;
        [Tooltip("Onde ficam as árvores da área (as que já estão lá e as que nascerem)")]
        [SerializeField] private Transform _treesParent;
        [Tooltip("Tamanho (m) da área, centrada neste objeto")]
        [SerializeField] private Vector3 _size = new(80f, 12f, 80f);
        [Tooltip("Mesma seed, mesmas mudas em todo cliente")]
        [SerializeField] private int _seed = 1;

        private readonly List<ChoppableTree> _trees = new();
        private readonly List<ChoppableTree> _parents = new();
        private readonly List<PendingSprout> _sprouts = new();
        private readonly List<NoPlantZone> _zones = new();
        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private int _woodLayer;
        private int _groundLayer;
        private int _probeMask;
        private int _plantedCount;

        public Vector3 Size => _size;
        public ForestDefinition Definition => _definition;
        public Transform TreesParent => _treesParent;
        public int PendingSproutCount => _sprouts.Count;

        private void Awake()
        {
            _woodLayer = LayerMask.NameToLayer(WoodLayerName);
            _groundLayer = LayerMask.NameToLayer(GroundLayerName);
            // Tudo bloqueia a muda, menos o jogador
            _probeMask = Physics.AllLayers & ~LayerMask.GetMask("Player", "FirstPerson", "Ignore Raycast");
            GetComponentsInChildren(_zones);
        }

        private void OnEnable()
        {
            _clock.DayChanged += HandleDayChanged;
        }

        private void Start()
        {
            foreach (ChoppableTree tree in _treesParent.GetComponentsInChildren<ChoppableTree>())
            {
                if (Contains(tree.transform.position)) Register(tree);
            }
        }

        private void OnDisable()
        {
            _clock.DayChanged -= HandleDayChanged;
        }

        private void OnDestroy()
        {
            foreach (ChoppableTree tree in _trees)
            {
                if (tree) tree.Felled -= HandleTreeFelled;
            }
        }

        public bool Contains(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return Mathf.Abs(offset.x) <= _size.x * 0.5f && Mathf.Abs(offset.y) <= _size.y * 0.5f &&
                   Mathf.Abs(offset.z) <= _size.z * 0.5f;
        }

        private void HandleDayChanged()
        {
            // Tocos que sumiram e árvores destruídas liberam o lugar
            _trees.RemoveAll(tree => !tree);
            PlantDueSprouts();

            var random = new SeededRandom(SeededRandom.Combine((uint)_seed, (uint)_clock.DayIndex));
            SeedFromParents(ref random);
            SeedFromOutside(ref random);
        }

        private void HandleTreeFelled(ChoppableTree tree)
        {
            tree.Felled -= HandleTreeFelled;
            // Toco não cresce mais: apodrece até sumir
            if (tree.TryGetComponent(out TreeGrowth growth)) Destroy(growth);
            var proceduralTree = tree.GetComponent<ProceduralTree>();
            TreeLifecycleSettings lifecycle = proceduralTree.Species.Lifecycle;
            tree.gameObject.AddComponent<StumpDecay>().Begin(_clock, _clock.TotalHours, lifecycle.StumpDecayDays);
            ScheduleSprout(tree, proceduralTree, lifecycle);
        }

        // Mudas caem perto das árvores adultas, da mesma espécie delas
        private void SeedFromParents(ref SeededRandom random)
        {
            float chance = _definition.ChanceFor(_clock.Season);
            _parents.Clear();
            foreach (ChoppableTree tree in _trees)
            {
                if (!tree.IsFelled && !tree.TryGetComponent(out TreeGrowth _)) _parents.Add(tree);
            }

            foreach (ChoppableTree parent in _parents)
            {
                // Sorteia tudo antes de testar o lugar: a sequência fica igual em todo cliente
                bool drops = random.NextFloat() < chance;
                float angle = random.NextAngle();
                float distance = _definition.SpreadDistance.Pick(ref random);
                float yaw = random.NextAngle() * Mathf.Rad2Deg;
                int seed = random.RangeInclusive(1, int.MaxValue - 1);
                if (!drops || _trees.Count >= _definition.MaxTrees) continue;

                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (TryFindSpot(parent.transform.position + offset, out Vector3 spot))
                {
                    Plant(parent.GetComponent<ProceduralTree>().Species, seed, spot, yaw, SaplingNameFormat);
                }
            }
        }

        // Vento e pássaros trazem sementes de longe: espécie sorteada pela lista, num lugar livre qualquer
        private void SeedFromOutside(ref SeededRandom random)
        {
            bool arrives = random.NextFloat() < _definition.OutsideChanceFor(_clock.Season);
            float x = (random.NextFloat() - 0.5f) * _size.x;
            float z = (random.NextFloat() - 0.5f) * _size.z;
            float yaw = random.NextAngle() * Mathf.Rad2Deg;
            int seed = random.RangeInclusive(1, int.MaxValue - 1);
            TreeSpeciesDefinition species = _definition.PickSpecies(ref random);
            if (!arrives || species == null || _trees.Count >= _definition.MaxTrees) return;

            if (TryFindSpot(transform.position + new Vector3(x, 0f, z), out Vector3 spot))
            {
                Plant(species, seed, spot, yaw, SaplingNameFormat);
            }
        }

        // Carvalho e bétula rebrotam: um broto da mesma espécie aparece na borda do toco alguns dias depois
        private void ScheduleSprout(ChoppableTree stump, ProceduralTree tree, TreeLifecycleSettings lifecycle)
        {
            uint stumpSeed = SeededRandom.Combine((uint)_seed, (uint)tree.Seed);
            var random = new SeededRandom(SeededRandom.Combine(stumpSeed, (uint)_clock.DayIndex));
            bool resprouts = random.NextFloat() < lifecycle.ResproutChance;
            float angle = random.NextAngle();
            float yaw = random.NextAngle() * Mathf.Rad2Deg;
            int seed = random.RangeInclusive(1, int.MaxValue - 1);
            if (!resprouts) return;

            float radius = stump.TryGetComponent(out Renderer stumpRenderer)
                ? Mathf.Min(stumpRenderer.bounds.extents.x, stumpRenderer.bounds.extents.z) * SproutEdgeFraction
                : 0f;
            Vector3 position = stump.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            double dueHours = _clock.TotalHours + _definition.ResproutDelayDays * GameCalendar.HoursPerDay;
            _sprouts.Add(new PendingSprout(tree.Species, seed, position, yaw, dueHours));
        }

        private void PlantDueSprouts()
        {
            for (int i = _sprouts.Count - 1; i >= 0; i--)
            {
                PendingSprout sprout = _sprouts[i];
                if (sprout.DueHours > _clock.TotalHours) continue;

                _sprouts.RemoveAt(i);
                // O broto ocupa o lugar do toco: não passa pelo espaço mínimo nem pelo limite da área
                Plant(sprout.Species, sprout.Seed, sprout.Position, sprout.Yaw, SproutNameFormat);
            }
        }

        private void Register(ChoppableTree tree)
        {
            _trees.Add(tree);
            tree.Felled += HandleTreeFelled;
        }

        // Lugar livre: dentro da área e fora das zonas proibidas, longe de outras árvores e tocos, e a primeira coisa
        // de cima para baixo é chão de terra (camada Ground, plano), com uma coluna sem nada em cima
        private bool TryFindSpot(Vector3 position, out Vector3 spot)
        {
            spot = position;
            if (!Contains(position) || IsInZone(position) || IsNearTree(position)) return false;

            Vector3 probe = position + Vector3.up * GroundProbeHeight;
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit ground, GroundProbeHeight * 2f, _probeMask,
                    QueryTriggerInteraction.Ignore)) return false;
            // Telhado, pedra, tora, concreto: não é terra
            if (ground.collider.gameObject.layer != _groundLayer) return false;
            if (Vector3.Angle(ground.normal, Vector3.up) > _definition.MaxSlope || !Contains(ground.point)) return false;

            float radius = _definition.Clearance;
            Vector3 bottom = ground.point + Vector3.up * (radius + ClearanceLift);
            Vector3 top = ground.point + Vector3.up * Mathf.Max(radius + ClearanceLift, _definition.ClearanceHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _overlaps, _probeMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(_overlaps[i], ground.collider)) return false;
            }

            spot = ground.point;
            return true;
        }

        private bool IsInZone(Vector3 position)
        {
            foreach (NoPlantZone zone in _zones)
            {
                if (zone.Contains(position)) return true;
            }
            return false;
        }

        private bool IsNearTree(Vector3 position)
        {
            float minSpacingSqr = _definition.MinSpacing * _definition.MinSpacing;
            foreach (ChoppableTree tree in _trees)
            {
                if (!tree) continue;
                Vector3 offset = tree.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minSpacingSqr) return true;
            }
            return false;
        }

        private void Plant(TreeSpeciesDefinition species, int seed, Vector3 position, float yaw, string nameFormat)
        {
            _plantedCount++;
            var sapling = new GameObject(string.Format(nameFormat, species.name, _plantedCount)) { layer = _woodLayer };
            sapling.transform.SetParent(_treesParent, false);
            sapling.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            sapling.AddComponent<ProceduralTree>().Configure(species, seed);
            ChoppableTree tree = sapling.AddComponent<ChoppableTree>();
            sapling.AddComponent<TreeGrowth>().Begin(_clock, _clock.TotalHours);
            Register(tree);
        }

        private readonly struct PendingSprout
        {
            public PendingSprout(TreeSpeciesDefinition species, int seed, Vector3 position, float yaw, double dueHours)
            {
                Species = species;
                Seed = seed;
                Position = position;
                Yaw = yaw;
                DueHours = dueHours;
            }

            public TreeSpeciesDefinition Species { get; }
            public int Seed { get; }
            public Vector3 Position { get; }
            public float Yaw { get; }
            public double DueHours { get; }
        }
    }
}
