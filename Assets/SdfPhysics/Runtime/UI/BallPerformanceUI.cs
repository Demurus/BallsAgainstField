using SdfPhysics.Runtime.Simulation;
using UnityEngine;

namespace SdfPhysics.Runtime.UI
{
    /// <summary>
    /// Shows average frame time and frame rate over a short interval
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallPerformanceUI : MonoBehaviour
    {
        [SerializeField] private BallSimulationController _simulation;
        [SerializeField, Min(0.05f)] private float _refreshInterval = 0.25f;

        private GUIStyle _labelStyle;
        private float _elapsed;
        private int _frameCount;
        private string _readout = "Balls: --\nFrame: -- ms\nFPS: --";

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            _frameCount++;

            if (_elapsed < _refreshInterval)
            {
                return;
            }

            var framesPerSecond = _frameCount / _elapsed;
            var frameTimeMilliseconds = _elapsed * 1000f / _frameCount;
            var ballCount = _simulation != null ? _simulation.BallCount : 0;
            _readout = $"Balls: {ballCount:N0}\nFrame: {frameTimeMilliseconds:F2} ms\nFPS: {framesPerSecond:F1}";
            _elapsed = 0f;
            _frameCount = 0;
        }

        private void OnGUI()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            GUI.Label(new Rect(16f, 16f, 300f, 90f), _readout, _labelStyle);
        }

        private void OnValidate()
        {
            _refreshInterval = Mathf.Max(0.05f, _refreshInterval);
        }
    }
}
