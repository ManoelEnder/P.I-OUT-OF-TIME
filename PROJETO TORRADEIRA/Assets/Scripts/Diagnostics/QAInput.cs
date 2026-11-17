using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Teclado e mouse virtuais para testes automaticos e para o benchmark da build.
// Os eventos entram pelo Input System, entao os scripts do jogo leem como input real.
public static class QAInput
{
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static readonly HashSet<Key> held = new HashSet<Key>();
    private static bool leftButton;

    public static void Ensure()
    {
        if (keyboard == null || !keyboard.added)
        {
            keyboard = InputSystem.AddDevice<Keyboard>("QAKeyboard");
            held.Clear();
        }

        if (mouse == null || !mouse.added)
        {
            mouse = InputSystem.AddDevice<Mouse>("QAMouse");
            leftButton = false;
        }

        keyboard.MakeCurrent();
        mouse.MakeCurrent();
    }

    public static void Hold(Key key, bool down)
    {
        Ensure();

        if (down)
            held.Add(key);
        else
            held.Remove(key);

        var keys = new Key[held.Count];
        held.CopyTo(keys);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    }

    // Aperta num quadro e solta no seguinte (wasPressedThisFrame dispara uma vez).
    public static IEnumerator Tap(Key key)
    {
        Hold(key, true);
        yield return null;
        yield return null;
        Hold(key, false);
        yield return null;
    }

    public static IEnumerator Click()
    {
        SetLeft(true);
        yield return null;
        yield return null;
        SetLeft(false);
        yield return null;
    }

    public static IEnumerator Scroll(float y)
    {
        QueueMouse(Vector2.zero, new Vector2(0f, y));
        yield return null;
        QueueMouse(Vector2.zero, Vector2.zero);
        yield return null;
    }

    // Movimento do mouse (olhar) por varios quadros.
    public static IEnumerator Look(Vector2 deltaPerFrame, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            QueueMouse(deltaPerFrame, Vector2.zero);
            yield return null;
        }

        QueueMouse(Vector2.zero, Vector2.zero);
        yield return null;
    }

    private static void SetLeft(bool down)
    {
        Ensure();
        leftButton = down;
        QueueMouse(Vector2.zero, Vector2.zero);
    }

    private static void QueueMouse(Vector2 delta, Vector2 scroll)
    {
        Ensure();
        var state = new MouseState { delta = delta, scroll = scroll };
        if (leftButton)
            state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
    }
}
