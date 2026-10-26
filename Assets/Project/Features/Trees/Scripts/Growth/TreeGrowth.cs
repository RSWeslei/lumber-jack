using LumberJack.WorldTime;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Muda crescendo até virar árvore adulta: o tamanho sai do relógio (quando nasceu e as estações desde então)
    /// e é atualizado uma vez por hora do jogo. Adulta, o componente sai: árvore pronta não custa nada.
    /// </summary>
    [RequireComponent(typeof(ProceduralTree))]
    public class TreeGrowth : MonoBehaviour
    {
        private const int SeasonCount = 4;

        private WorldClock _clock;
        private TreeLifecycleSettings _lifecycle;
        private readonly double[] _seasonRates = new double[SeasonCount];
        private Transform _transform;
        private bool _isStarted;

        /// <summary>Horas do relógio em que a muda nasceu.</summary>
        public double PlantedHours { get; private set; }
        /// <summary>De 0 (muda nova) a 1 (adulta).</summary>
        public double Growth { get; private set; }

        private void OnEnable()
        {
            if (_isStarted) _clock.HourChanged += HandleHourChanged;
        }

        private void OnDisable()
        {
            if (_isStarted) _clock.HourChanged -= HandleHourChanged;
        }

        /// <summary>Começa a crescer a partir de <paramref name="plantedHours"/> (horas do relógio).</summary>
        public void Begin(WorldClock clock, double plantedHours)
        {
            _clock = clock;
            _lifecycle = GetComponent<ProceduralTree>().Species.Lifecycle;
            for (int season = 0; season < SeasonCount; season++) _seasonRates[season] = _lifecycle.RateFor((Season)season);
            _transform = transform;
            PlantedHours = plantedHours;
            _isStarted = true;
            if (isActiveAndEnabled) _clock.HourChanged += HandleHourChanged;
            HandleHourChanged();
        }

        private void HandleHourChanged()
        {
            Growth = TreeGrowthCalculator.Growth(PlantedHours, _clock.TotalHours, _lifecycle.GrowingDays, _seasonRates,
                _clock.Calendar, _clock.Latitude);
            _transform.localScale = Vector3.one * (float)TreeGrowthCalculator.Scale(Growth, _lifecycle.SaplingScale);
            if (Growth >= 1.0) Destroy(this);
        }
    }
}
