using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

public class CartaVolteoAnimacion : MonoBehaviour
{
    [Header("Componentes Principales")]
    public Image dorso;
    public Image cara;
    public GameObject cartaAbanicoPrefab;
    public Transform contenedorCartas;

    [Header("Sprites")]
    public Sprite[] cartasAbanico;
    public Sprite[] carasPosibles;
    public float tiempoEntreCambiosCara = 0.1f; 

    [Header("Configuraci�n Animaci�n")]
    public float duracionVolteo = 0.8f;
    public float escalaDuranteVolteo = 1.1f;
    public float tiempoEntreAcciones = 0.3f; 
    public float radioAbanico = 600f;
    public float anguloAbanico = 60f;
    public float duracionAbanico = 0.3f; 
    public float espaciadoCartas = 0.8f;
    public float tiempoEntreCartasAbanico = 0.1f; 

    private List<GameObject> cartasAbanicoActuales = new List<GameObject>();
    private bool mostrandoCara = false;
    private Coroutine animacionCoroutine;
    private ObjectPool<GameObject> cartaPool;

    void Awake()
    {
        cartaPool = new ObjectPool<GameObject>(
            () => {
                GameObject obj = Instantiate(cartaAbanicoPrefab, contenedorCartas);
                obj.SetActive(false);
                return obj;
            },
            carta => carta.SetActive(true),
            carta => carta.SetActive(false),
            carta => Destroy(carta),
            false, 10, 20
        );
    }

    void OnEnable()
    {
        InicializarCarta();
        animacionCoroutine = StartCoroutine(CicloAnimacion());
    }

    void OnDisable()
    {
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
        }

        DOTween.Kill(transform);

        foreach (GameObject carta in cartasAbanicoActuales)
        {
            if (carta != null) cartaPool.Release(carta);
        }
        cartasAbanicoActuales.Clear();
    }

    void InicializarCarta()
    {
        dorso.gameObject.SetActive(true);
        cara.gameObject.SetActive(false);
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    IEnumerator CicloAnimacion()
    {
        while (true)
        {
     
            yield return StartCoroutine(VoltearCarta(true));

            // Muestra el abanico
            yield return StartCoroutine(MostrarAbanicoConCambios());

           
            yield return new WaitForSeconds(5f);

            yield return StartCoroutine(OcultarAbanico());

            yield return StartCoroutine(VoltearCarta(false));

      
            yield return new WaitForSeconds(1f);
        }
    }


    IEnumerator VoltearCarta(bool mostrarCara)
    {
        transform.DOScaleX(0.05f, duracionVolteo / 2).SetEase(Ease.OutQuad);
        transform.DOScaleY(escalaDuranteVolteo, duracionVolteo / 4);

        float anguloInicial = mostrarCara ? 0 : 180;
        float anguloFinal = mostrarCara ? 180 : 0;

        for (float t = 0; t < 1; t += Time.deltaTime / (duracionVolteo / 2))
        {
            float angulo = Mathf.Lerp(anguloInicial, 90, t);
            transform.rotation = Quaternion.Euler(0, angulo, 0);

            if (angulo >= 45 && angulo <= 90 && !mostrandoCara == mostrarCara)
            {
                mostrandoCara = mostrarCara;
                dorso.gameObject.SetActive(!mostrarCara);
                cara.gameObject.SetActive(mostrarCara);

                if (mostrarCara && carasPosibles.Length > 0)
                {
                    cara.sprite = carasPosibles[Random.Range(0, carasPosibles.Length)];
                }
            }
            yield return null;
        }

        for (float t = 0; t < 1; t += Time.deltaTime / (duracionVolteo / 2))
        {
            float angulo = Mathf.Lerp(90, anguloFinal, t);
            transform.rotation = Quaternion.Euler(0, angulo, 0);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, anguloFinal, 0);
        transform.DOScaleX(1f, 0.1f);
        transform.DOScaleY(1f, 0.1f);
    }

    IEnumerator MostrarAbanicoConCambios()
    {
        int cantidadCartas = Mathf.Min(cartasAbanico.Length, 4);
        float anguloEntreCartas = (anguloAbanico / (cantidadCartas - 1)) * espaciadoCartas;
        float anguloInicial = -anguloAbanico / 2 * espaciadoCartas;

        for (int i = 0; i < cantidadCartas; i++)
        {
            if (i < carasPosibles.Length)
            {
                cara.sprite = cartasAbanico[i];
            }

            float angulo = anguloInicial + (i * anguloEntreCartas);
            Vector2 posicion = CalcularPosicionEnArco(angulo, radioAbanico);
            float anguloRotacion = angulo * 0.7f;

            GameObject carta = CrearCartaAbanico(i, posicion, anguloRotacion);
            cartasAbanicoActuales.Add(carta);

            Sequence animacion = DOTween.Sequence();
            animacion.Join(carta.transform.DOScale(1, duracionAbanico).SetEase(Ease.OutBack));
            animacion.Join(carta.GetComponent<RectTransform>().DOAnchorPos(posicion, duracionAbanico));
            animacion.Join(carta.transform.DORotate(new Vector3(0, 0, anguloRotacion), duracionAbanico));

            yield return new WaitForSeconds(tiempoEntreCartasAbanico);
        }
    }

    IEnumerator OcultarAbanico()
    {
        foreach (GameObject carta in cartasAbanicoActuales)
        {
            if (carta != null)
            {
                Sequence animacion = DOTween.Sequence();
                animacion.Append(carta.transform.DOScale(0, 0.2f).SetEase(Ease.InBack)); // Reducido de 0.4f
                animacion.Join(carta.GetComponent<RectTransform>().DOAnchorPos(Vector2.zero, 0.2f));
                animacion.Join(carta.transform.DORotate(Vector3.zero, 0.2f));
                animacion.OnComplete(() => cartaPool.Release(carta));
            }
        }

        yield return new WaitForSeconds(0.2f); // Reducido de 0.4f
        cartasAbanicoActuales.Clear();
    }

    GameObject CrearCartaAbanico(int indiceSprite, Vector2 posicion, float angulo)
    {
        GameObject carta = cartaPool.Get();
        Image img = carta.GetComponentInChildren<Image>();
        RectTransform rt = carta.GetComponent<RectTransform>();

        if (indiceSprite < cartasAbanico.Length)
        {
            img.sprite = cartasAbanico[indiceSprite];
        }

        img.color = Color.white;
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0, 0, angulo);
        rt.localScale = Vector3.zero;

        return carta;
    }

    Vector2 CalcularPosicionEnArco(float angulo, float radio)
    {
        float anguloRad = angulo * Mathf.Deg2Rad;
        return new Vector2(
            radio * Mathf.Sin(anguloRad),
            radio * (1 - Mathf.Cos(anguloRad))
        );
    }
}