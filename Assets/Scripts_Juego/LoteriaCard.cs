using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using System.Collections;

public class LoteriaCard : MonoBehaviour
{
    public Image[] cardSlots;
    public Sprite[] allImages;
    public Image handImage; 
    public Image beanImagePrefab; 
    public Image currentCardImage;

    [Header("Animation Settings")]
    public float handMoveDuration = 0.5f; 
    public float handStayDuration = 0.2f; 
    public Ease moveEase = Ease.OutQuad; 
    public Ease downEase = Ease.InQuad; 
    public float punchScaleDuration = 0.2f; 
    public Vector3 punchScale = new Vector3(0.05f, 0.05f, 0f); 
    public Color matchedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public float handFinalYOffset = 50f; 
    public float handStartYOffset = -200f;
    public float overshootAmount = 20f; 
    public float downMoveDurationMultiplier = 0.8f; 

    private Dictionary<string, Image> cardDictionary = new Dictionary<string, Image>();
    private HashSet<string> matchedCards = new HashSet<string>();
    private Dictionary<Image, Image> beanImages = new Dictionary<Image, Image>();

    void Start()
    {
        // Ensure hand is disabled at start
        if (handImage != null)
        {
            handImage.gameObject.SetActive(false);
            handImage.preserveAspect = true;
        }

        InitializeCardDictionary();
        LoadCardsFromPlayerPrefs();
    }

    void InitializeCardDictionary()
    {
        foreach (Image slot in cardSlots)
        {
            if (slot.sprite != null)
            {
                string cardName = slot.sprite.name.ToLower();
                if (!cardDictionary.ContainsKey(cardName))
                {
                    cardDictionary.Add(cardName, slot);
                }
            }
        }
    }

    void LoadCardsFromPlayerPrefs()
    {
        string savedCards = PlayerPrefs.GetString("CartasSeleccionadas", "");
        if (!string.IsNullOrEmpty(savedCards))
        {
            string[] cardNames = savedCards.Split(',');

            for (int i = 0; i < Mathf.Min(cardNames.Length, cardSlots.Length); i++)
            {
                string cardName = cardNames[i].Trim();
                Sprite cardSprite = allImages.FirstOrDefault(s => s.name.ToLower() == cardName.ToLower());
                if (cardSprite != null)
                {
                    cardSlots[i].sprite = cardSprite;
                    cardDictionary[cardName.ToLower()] = cardSlots[i];
                }
            }
        }
    }

    public void CheckCardInSlots(string cardName)
    {
        string lowerName = cardName.ToLower();

        if (matchedCards.Contains(lowerName)) return;

        if (cardDictionary.ContainsKey(lowerName))
        {
            StartCoroutine(HandAndBeanAnimation(cardDictionary[lowerName]));

            if (CardManager.Instance != null)
            {
                CardManager.Instance.RegisterCorrectLetter(lowerName);
            }
        }
    }

    private IEnumerator HandAndBeanAnimation(Image cardImage)
    {
        string cardName = cardImage.sprite.name.ToLower();
        cardImage.raycastTarget = false;

        // Obtener referencia al canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasHeight = canvasRect.rect.height;

        // 1. Posicionar mano centrada y fuera de vista (abajo)
        if (handImage != null)
        {
            // Centrar horizontalmente y posicionar abajo del todo
            handImage.rectTransform.localPosition = new Vector3(
                0, // Centrado en X
                -canvasHeight / 2 - handImage.rectTransform.rect.height / 2, // Fuera de vista abajo
                0);

            handImage.gameObject.SetActive(true);
            handImage.transform.SetAsLastSibling(); // Asegurar que est� sobre todo
        }

        // 2. Animaci�n de subida (m�s desplazamiento que antes)
        if (handImage != null)
        {
            // Subir hasta 150px sobre el borde inferior
            float targetY = -canvasHeight / 2 + 300f;

            yield return handImage.rectTransform.DOLocalMoveY(
                    targetY,
                    handMoveDuration * 0.7f)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();

            // Pausa para que se vea la mano
            yield return new WaitForSeconds(handStayDuration * 0.7f);
        }

        // 3. Animaci�n de bajada
        if (handImage != null)
        {
            // Volver a posici�n inicial (fuera de vista)
            yield return handImage.rectTransform.DOLocalMoveY(
                    -canvasHeight / 2 - handImage.rectTransform.rect.height / 2,
                    handMoveDuration * 0.5f)
                .SetEase(Ease.InSine)
                .WaitForCompletion();

            handImage.gameObject.SetActive(false);
        }

        // 4. Mostrar frijol despu�s de la animaci�n
        if (!beanImages.TryGetValue(cardImage, out Image beanImg))
        {
            if (beanImagePrefab != null)
            {
                beanImg = Instantiate(beanImagePrefab, cardImage.transform);
                beanImg.rectTransform.anchoredPosition = Vector2.zero;
                beanImg.gameObject.SetActive(true);
                beanImages[cardImage] = beanImg;

                // Animaci�n sutil de aparici�n
                beanImg.transform.localScale = Vector3.zero;
                beanImg.transform.DOScale(Vector3.one, 0.25f)
                    .SetEase(Ease.OutBack);
            }
        }
        else
        {
            beanImg.gameObject.SetActive(true);
        }

        // Marcar carta como completada
        matchedCards.Add(cardName);
        cardImage.raycastTarget = true;

        // Efecto m�nimo en la carta
        cardImage.transform.DOPunchScale(new Vector3(0.07f, 0.07f, 0f), 0.25f);
    }

    public bool HasCard(string letter)
    {
        return cardDictionary.ContainsKey(letter.ToLower());
    }
}