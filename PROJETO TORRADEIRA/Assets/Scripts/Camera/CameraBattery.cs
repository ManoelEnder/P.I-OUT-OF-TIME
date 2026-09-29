using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CameraBattery : MonoBehaviour
{
    [SerializeField] private int maxBattery = 10;
    [SerializeField] private Image[] batteryBars;
    [SerializeField] private float drainInterval = 3f;

    private int currentBattery;
    private float drainTimer;

    public bool IsEmpty => currentBattery <= 0;
    public bool IsFull => currentBattery >= maxBattery;
    public bool CanReceive => currentBattery < maxBattery;

    public void Initialize()
    {
        currentBattery = maxBattery;
        drainTimer = 0f;
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
        currentBattery = Mathf.Clamp(value, 0, maxBattery);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (batteryBars == null)
            return;

        for (int i = 0; i < batteryBars.Length; i++)
        {
            if (batteryBars[i] != null)
                batteryBars[i].enabled = i < currentBattery;
        }
    }
}
