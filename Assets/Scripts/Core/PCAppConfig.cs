using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vortex.Core
{
    public class PCAppConfig : MonoBehaviour
    {
        private const float DoubleEscWindow = 0.35f;
        private float _lastEscTime = -1f;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            bool escPressed = false;
            bool altF4Pressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
                bool altHeld = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
                altF4Pressed = altHeld && Keyboard.current.f4Key.wasPressedThisFrame;
            }
#else
            escPressed = Input.GetKeyDown(KeyCode.Escape);
            altF4Pressed = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.F4);
#endif

            if (altF4Pressed)
            {
                QuitApplication();
                return;
            }

            if (escPressed)
            {
                float timeNow = Time.unscaledTime;
                if (timeNow - _lastEscTime <= DoubleEscWindow)
                {
                    QuitApplication();
                }
                else
                {
                    _lastEscTime = timeNow;
                }
            }
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
