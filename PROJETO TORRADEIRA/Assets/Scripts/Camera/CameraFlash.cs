using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CameraFlash : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.07f;
    [SerializeField] private float flashFadeDuration = 0.1f;

    public void Initialize()
    {
        if (flashImage == null)
            return;

        SetAlpha(0f);
        flashImage.gameObject.SetActive(false);
    }

    public IEnumerator Play()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);
        flashImage.transform.SetAsLastSibling();
        SetAlpha(1f);

        yield return new WaitForSeconds(flashDuration);

        float elapsedTime = 0f;

        while (elapsedTime < flashFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, elapsedTime / flashFadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        flashImage.gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }
}
