using SdfPhysics.Runtime.Simulation;
using UnityEngine;

namespace SdfPhysics.Runtime.UI
{
    /// <summary>
    /// Shows a short moving-average frame rate so a
    /// single unusually slow frame does not dominate the readout
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallPerformanceUI : MonoBehaviour
    {
        [SerializeField] private BallSimulationController _simulation;
        [SerializeField, Min(0.05f)] private float _refreshInterval = 0.25f;

        private GUIStyle _labelStyle;
        private float _elapsed;
        private int _frameCount;
        private string _readout = "Balls: --\nFPS: --";

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            _frameCount++;

            if (_elapsed < _refreshInterval)
            {
                return;
            }

            var framesPerSecond = _frameCount / _elapsed;
            var ballCount = _simulation != null ? _simulation.BallCount : 0;
            _readout = $"Balls: {ballCount:N0}\nFPS: {framesPerSecond:F1}";
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

            GUI.Label(new Rect(16f, 16f, 300f, 60f), _readout, _labelStyle);
        }

        private void OnValidate()
        {
            _refreshInterval = Mathf.Max(0.05f, _refreshInterval);
        }
    }
}
