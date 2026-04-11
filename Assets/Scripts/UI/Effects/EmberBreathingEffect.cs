using UnityEngine;
using UnityEngine.UI;

namespace DarkVillage.UI.Effects
{
    [RequireComponent(typeof(Image))]
    public sealed class EmberBreathingEffect : MonoBehaviour
    {
        [Header("Breathing Settings")]
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float panicPulseSpeed = 3.5f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 0.8f;
        [SerializeField] private bool useUnscaledTime = true;

        private Image _emberImage;
        private Color _baseColor;
        private float _currentPulseSpeed;

        private void Awake()
        {
            CacheImage();
            _currentPulseSpeed = Mathf.Max(0.01f, pulseSpeed);
        }

        private void OnEnable()
        {
            CacheImage();
        }

        private void Update()
        {
            if (_emberImage == null)
            {
                return;
            }

            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float wave = Mathf.Sin(time * _currentPulseSpeed);
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, (wave + 1f) * 0.5f);

            Color currentColor = _baseColor;
            currentColor.a = currentAlpha;
            _emberImage.color = currentColor;
        }

        public void Configure(float basePulseSpeed, float minimumAlpha, float maximumAlpha)
        {
            pulseSpeed = Mathf.Max(0.01f, basePulseSpeed);
            minAlpha = Mathf.Clamp01(minimumAlpha);
            maxAlpha = Mathf.Clamp(maximumAlpha, minAlpha, 1f);
            _currentPulseSpeed = pulseSpeed;
            CacheImage();
        }

        public void SetBaseColor(Color baseColor)
        {
            _baseColor = baseColor;
            if (_emberImage != null)
            {
                _emberImage.color = baseColor;
            }
        }

        public void SetPanicMode(bool isPanic)
        {
            _currentPulseSpeed = isPanic
                ? Mathf.Max(panicPulseSpeed, pulseSpeed)
                : Mathf.Max(0.01f, pulseSpeed);
        }

        private void CacheImage()
        {
            if (_emberImage == null)
            {
                _emberImage = GetComponent<Image>();
            }

            if (_emberImage == null)
            {
                return;
            }

            minAlpha = Mathf.Clamp01(minAlpha);
            maxAlpha = Mathf.Clamp(maxAlpha, minAlpha, 1f);
            _baseColor = _emberImage.color;
        }
    }
}
