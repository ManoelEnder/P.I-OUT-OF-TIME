using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CameraBattery : MonoBehaviour
{
    [SerializeField] private int maxBattery = 10;
    [SerializeField] private Image[] batteryBars;
    [SerializeField] private float drainInterval = 3f;

    [Tooltip("Segundos para recuperar uma barra com o visor fechado. Zero desativa.")]
    [SerializeField] private float rechargeInterval = 12f;

    [Tooltip("A recarga passiva para neste numero de barras; pilhas no mapa enchem o resto.")]
    [SerializeField] private int rechargeLimit = 3;

    [Header("Cores")]
    [SerializeField] private Color corNormal = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color corBaixa = new Color(0.9f, 0.18f, 0.22f, 1f);
    [SerializeField] private int lowThreshold = 2;

    private int currentBattery;
    private float drainTimer;
    private float rechargeTimer;
    private bool lowWarningActive;

    public int Current => currentBattery;
    public int Max => maxBattery;
    public bool IsLow => currentBattery <= lowThreshold;

    public bool IsEmpty => currentBattery <= 0;
    public bool IsFull => currentBattery >= maxBattery;
    public bool CanReceive => currentBattery < maxBattery;

    public void Initialize()
    {
        currentBattery = maxBattery;
        drainTimer = 0f;
        rechargeTimer = 0f;
        UpdateUI();
    }

    public bool Tick(float deltaTime)
    {
        drainTimer += deltaTime;

        if (drainTimer < drainInterval)
            return false;

        drainTimer = 0f;
        Use(1);
        return true;
    }

    public void TickRecharge(float deltaTime)
    {
        int limit = Mathf.Min(rechargeLimit, maxBattery);

        // Bateria zerada nao recarrega sozinha: so com pilha do mapa (a camera fica travada).
        if (rechargeInterval <= 0f || currentBattery <= 0 || currentBattery >= limit)
        {
            rechargeTimer = 0f;
            return;
        }

        rechargeTimer += deltaTime;

        if (rechargeTimer < rechargeInterval)
            return;

        rechargeTimer = 0f;
        Add(1);
    }

    // Pisca as barras restantes quando a carga esta baixa e o visor aberto.
    public void UpdateLowWarning(bool visorOpen)
    {
        if (batteryBars == null || (!visorOpen || !IsLow) && !lowWarningActive)
            return;

        lowWarningActive = visorOpen && IsLow;

        float alpha = lowWarningActive
            ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f))
            : 1f;

        Color color = IsLow ? corBaixa : corNormal;
        color.a = alpha;

        foreach (Image bar in batteryBars)
        {
            if (bar != null)
                bar.color = color;
        }
    }

    public void Use(int amount)
    {
        SetBattery(currentBattery - amount);
    }

    public void Add(int amount)
    {
        SetBattery(currentBattery + amount);
    }

    private void SetBattery(int value)
    {
        int clamped = Mathf.Clamp(value, 0, maxBattery);

        if (clamped == currentBattery && batteryBars != null)
            return;

        currentBattery = clamped;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (batteryBars == null)
            return;

        Color color = IsLow ? corBaixa : corNormal;

        // O HUD pode ter menos barras que cargas: cada barra representa uma fracao da bateria.
        int litBars = Mathf.CeilToInt(
            (float)currentBattery / Mathf.Max(1, maxBattery) * batteryBars.Length
        );

        for (int i = 0; i < batteryBars.Length; i++)
        {
            if (batteryBars[i] == null)
                continue;

            batteryBars[i].enabled = i < litBars;
            batteryBars[i].color = color;
        }
    }
}
