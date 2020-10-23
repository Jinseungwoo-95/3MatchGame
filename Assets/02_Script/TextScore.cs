using System.Collections;
using UnityEngine;
using TMPro;

public class TextScore : MonoBehaviour
{
    RectTransform rectTransform;
    TextMeshPro textMeshPro;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textMeshPro = GetComponent<TextMeshPro>();
    }

    private void OnEnable()
    {
        Color color = textMeshPro.color;
        color.a = 1;
        textMeshPro.color = color;
        StartCoroutine(FadeOut());
    }

    void Update()
    {
        rectTransform.Translate(Vector3.up * Time.deltaTime);
    }

    public void SetInfo(Vector3 pos, int score)
    {
        rectTransform.localPosition = pos;
        textMeshPro.text = score.ToString();
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1f);
        Color color = textMeshPro.color;

        for (float alpha = 1; alpha > 0; alpha -= 0.02f)
        {
            color.a = alpha;
            textMeshPro.color = color;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
