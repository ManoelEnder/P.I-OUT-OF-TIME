using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public class LensDistortionProcessor : InputProcessor<Vector2>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        InputSystem.RegisterProcessor<LensDistortionProcessor>();
    }

    public override Vector2 Process(Vector2 value, InputControl control)
    {
        if (LensDistortionCompensator.Instance == null)
            return value;

        return LensDistortionCompensator.Instance.UndistortScreenPoint(value);
    }
}