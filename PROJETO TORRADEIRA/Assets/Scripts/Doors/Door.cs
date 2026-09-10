using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Configuração")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 180f;

    [Header("Tremida da Porta")]
    [SerializeField] private float lockedShakeAngle = 10f;
    [SerializeField] private float lockedShakeSpeed = 35f;
    [SerializeField] private float lockedShakeDuration = 0.2f;

    [Header("Efeito de Batida")]
    [SerializeField] private float bounceAmount = 6f;
    [SerializeField] private float bounceSpeed = 8f;
    [SerializeField] private float damping = 3.5f;

    private bool isOpen;
    private bool bouncing;
    private bool hasBounced;
    private bool shaking;

    private float shakeTimer;
    private float bounceTimer;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.rotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        HandleShake();
        HandleDoorMovement();
    }

    public bool CanInteract()
    {
        return !shaking && !bouncing;
    }

    public string GetInteractionMessage()
    {
        return isOpen
            ? "E para fechar"
            : "E para abrir";
    }

    public void Interact()
    {
        if (shaking || bouncing)
        {
            return;
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            hasBounced = false;
            bouncing = false;
        }
        else
        {
            bouncing = false;
            hasBounced = true;
        }
    }

    private void HandleShake()
    {
        if (!shaking)
        {
            return;
        }

        shakeTimer += Time.deltaTime;

        float progress =
            1f -
            Mathf.Clamp01(
                shakeTimer / lockedShakeDuration
            );

        float offset =
            Mathf.Sin(
                shakeTimer * lockedShakeSpeed
            ) *
            lockedShakeAngle *
            progress;

        transform.rotation =
            closedRotation *
            Quaternion.Euler(0f, offset, 0f);

        if (shakeTimer >= lockedShakeDuration)
        {
            shaking = false;
            transform.rotation = closedRotation;
        }
    }

    private void HandleDoorMovement()
    {
        if (shaking)
        {
            return;
        }

        if (!isOpen)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    closedRotation,
                    speed * Time.deltaTime
                );

            return;
        }

        if (!bouncing && !hasBounced)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    openRotation,
                    speed * Time.deltaTime
                );

            if (
                Quaternion.Angle(
                    transform.rotation,
                    openRotation
                ) < 0.1f
            )
            {
                bouncing = true;
                bounceTimer = 0f;
            }

            return;
        }

        if (bouncing)
        {
            bounceTimer += Time.deltaTime;

            float offset =
                Mathf.Sin(
                    bounceTimer * bounceSpeed
                ) *
                bounceAmount *
                Mathf.Exp(
                    -damping * bounceTimer
                );

            transform.rotation =
                openRotation *
                Quaternion.Euler(
                    0f,
                    offset,
                    0f
                );

            if (Mathf.Abs(offset) < 0.05f)
            {
                bouncing = false;
                hasBounced = true;
                transform.rotation = openRotation;
            }

            return;
        }

        transform.rotation = openRotation;
    }
}