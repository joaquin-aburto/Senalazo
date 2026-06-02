using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class CartitasController : MonoBehaviour
{
    public RectTransform panelDestino;
    public GameObject prefabCarta;
    public Sprite[] spritesLetras;
    public float delayEntreCartas = 0.3f;
    public float duracionMovimiento = 6f;
    public float distanciaEntreCartas = 200f;

    private string palabraArriba = "señalazo";
    private string palabraBaseAbajo = "hola";

    void Start()
    {
        StartCoroutine(GenerarCartasBucle());
    }

    void OnEnable()
    {
        StartCoroutine(GenerarCartasBucle());
    }

    System.Collections.IEnumerator GenerarCartasBucle()
    {
        while (true)
        {
            // Generar la fila de arriba (señalazo) - ORDEN NORMAL
            foreach (char letra in palabraArriba)
            {
                Sprite sprite = BuscarSpritePorNombre(letra.ToString());
                if (sprite != null)
                {
                    GenerarCarta(sprite, 0); // 0 = fila de arriba
                }
                yield return new WaitForSeconds(delayEntreCartas);
            }

            // Tomamos el apodo del jugador desde PlayerPrefs
            string apodo = PlayerPrefs.GetString("Apodo", "");
            string palabraAbajoCompleta = palabraBaseAbajo + " " + apodo; // Agregamos espacio

            // Generar la fila de abajo (hola + espacio + apodo)
            foreach (char letra in palabraAbajoCompleta)
            {
                Sprite sprite = BuscarSpritePorNombre(letra.ToString().ToLower());
                if (sprite != null)
                {
                    GenerarCarta(sprite, 1); // 1 = fila de abajo
                }
                else if (letra == ' ') // Si es un espacio, buscamos un sprite llamado "space" o similar
                {
                    Sprite espacioSprite = BuscarSpritePorNombre("space") ?? BuscarSpritePorNombre(" ");
                    if (espacioSprite != null)
                    {
                        GenerarCarta(espacioSprite, 1);
                    }
                }
                yield return new WaitForSeconds(delayEntreCartas);
            }

            // Pausa antes de reiniciar
            yield return new WaitForSeconds(1f);
        }
    }

    Sprite BuscarSpritePorNombre(string letra)
    {
        foreach (var sprite in spritesLetras)
        {
            if (sprite != null && sprite.name == letra)
            {
                return sprite;
            }
        }
        Debug.LogWarning($"No se encontró sprite para la letra: {letra}");
        return null;
    }

    void GenerarCarta(Sprite sprite, int fila)
    {
        GameObject nuevaCarta = Instantiate(prefabCarta, panelDestino);
        RectTransform rect = nuevaCarta.GetComponent<RectTransform>();
        Image imagen = nuevaCarta.GetComponent<Image>();   

        imagen.sprite = sprite;
        rect.sizeDelta = new Vector2(200f, 220f);
        nuevaCarta.transform.SetAsFirstSibling();

        float panelHeight = panelDestino.rect.height;
        float panelWidth = panelDestino.rect.width;

        // Posiciones Y: una arriba, una abajo
        float y = (fila == 0) ? panelHeight / 3f : -panelHeight / 3f;

        // Posición inicial: derecha de la pantalla (fuera de vista)
        float startX = panelWidth / 2f + rect.sizeDelta.x / 2f + distanciaEntreCartas;
        // Posición final: izquierda de la pantalla (fuera de vista)
        float endX = -panelWidth / 2f - rect.sizeDelta.x / 2f;

        // Posición inicial con desplazamiento acumulado (para que no aparezcan todas juntas)
        float offsetInicial = distanciaEntreCartas * (fila == 0 ? palabraArriba.Length : (palabraBaseAbajo + " " + PlayerPrefs.GetString("Apodo", "")).Length);
        rect.anchoredPosition = new Vector2(startX + offsetInicial, y);

        // Movimiento de derecha a izquierda
        rect.DOAnchorPosX(endX, duracionMovimiento)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(nuevaCarta));
    }
}