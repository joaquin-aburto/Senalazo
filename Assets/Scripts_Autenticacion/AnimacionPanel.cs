using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class CombinedAnimationController : MonoBehaviour
{
    
    public Image panelImage;
    public Color colorInicial = Color.white;
    public Color colorFinal = Color.red;
    public float duracionColor = 2f;

    
    public RectTransform panelDestino;
    public GameObject prefabCarta;
    public Sprite[] spritesLetras;
    public float delayEntreCartas = 0.5f;
    public float duracionCaida = 6f;
    public float espacioMinimoEntreCartas = 100f;

    private List<float> posicionesRecientes = new List<float>();
    private Coroutine generacionCartas;

    void OnEnable()
    {
        
        if (panelImage == null)
        {
            Debug.LogError("�Asigna el Image del panel en el Inspector!");
        }
        else
        {
            StartCoroutine(CambiarColor());
        }

        // Comienza a generar cartas cuando se activa el GameObject
        if (generacionCartas == null && panelDestino != null && prefabCarta != null)
        {
            generacionCartas = StartCoroutine(GenerarCartasBucle());
        }
    }

    void OnDisable()
    {
        
        if (generacionCartas != null)
        {
            StopCoroutine(generacionCartas);
            generacionCartas = null;
        }
    }

    IEnumerator CambiarColor()
    {
        Debug.Log("Iniciando animaci�n del panel...");
        float tiempo = 0f;

        while (tiempo < duracionColor)
        {
            if (panelImage == null)
            {
                Debug.LogError("panelImage se hizo null durante la animaci�n");
                yield break;
            }

            panelImage.color = Color.Lerp(colorInicial, colorFinal, tiempo / duracionColor);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (panelImage != null)
        {
            panelImage.color = colorFinal;
            Debug.Log("�Animaci�n del panel completada correctamente!");
        }
    }

    IEnumerator GenerarCartasBucle()
    {
        while (true)
        {
            int index = Random.Range(0, spritesLetras.Length);
            GenerarCarta(index);
            yield return new WaitForSeconds(delayEntreCartas);
        }
    }

    void GenerarCarta(int index)
    {
        GameObject nuevaCarta = Instantiate(prefabCarta, panelDestino);
        nuevaCarta.transform.SetAsFirstSibling();

        RectTransform rect = nuevaCarta.GetComponent<RectTransform>();
        Image imagen = nuevaCarta.GetComponent<Image>();
        imagen.sprite = spritesLetras[index];

        float ancho = panelDestino.rect.width;
        float alto = panelDestino.rect.height;

        // Posici�n X evitando amontonamiento
        float x;
        int intentos = 0;
        do
        {
            x = Random.Range(-ancho / 2f + 50f, ancho / 2f - 50f);
            intentos++;
        } while (posicionesRecientes.Exists(px => Mathf.Abs(px - x) < espacioMinimoEntreCartas) && intentos < 10);

        posicionesRecientes.Add(x);
        if (posicionesRecientes.Count > 5) posicionesRecientes.RemoveAt(0);

        Vector2 inicio = new Vector2(x, alto / 2f + 50f);
        Vector2 destino = new Vector2(x + Random.Range(-20f, 20f), -alto / 2f - 100f);

        rect.anchoredPosition = inicio;
        rect.localScale = Vector3.zero;
        imagen.color = new Color(1f, 1f, 1f, 0f);

        Sequence secuencia = DOTween.Sequence();
        secuencia.Append(rect.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        secuencia.Join(imagen.DOFade(1f, 0.4f));
        secuencia.Append(rect.DOAnchorPos(destino, duracionCaida).SetEase(Ease.Linear));
        secuencia.Join(rect.DORotate(new Vector3(0, 0, Random.Range(-20f, 20f)), duracionCaida));

        // Fade out casi al final (90% de la animaci�n)
        float inicioFadeOut = duracionCaida * 0.9f;
        float duracionFadeOut = duracionCaida * 0.1f;
        secuencia.Insert(inicioFadeOut, imagen.DOFade(0f, duracionFadeOut));

        secuencia.OnComplete(() => Destroy(nuevaCarta));
    }
}