using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class CombinedCardSystem : MonoBehaviour
{
    [Header("Card Carousel Settings")]
    public List<Sprite> cardSprites;
    public GameObject cardPrefab;
    public float transitionDuration = 0.5f;
    public float sideCardsScale = 0.7f;

    [Header("Positions")]
    public Transform leftPos;
    public Transform centerPos;
    public Transform rightPos;
    public Transform hiddenLeftPos;
    public Transform hiddenRightPos;

    [Header("Buttons")]
    public Button previousButton;
    public Button nextButton;
    public Button selectButton;

    [Header("Card Data")]
    public string[][] cardDataArrays = new string[][]
    {
        new string[] { "q", "e", "v", "n", "r", "k", "d", "h", "p", "u", "b", "c", "x", "t", "g", "l" },
        new string[] { "i", "n", "q", "l", "c", "z", "d", "h", "a", "e", "o", "v", "m", "w", "g", "t" },
        new string[] { "k", "l", "m", "d", "r", "v", "u", "n", "p", "j", "f", "b", "x", "g", "s", "y" },
        new string[] { "j", "c", "n", "e", "a", "h", "y", "f", "l", "ñ", "d", "m", "i", "b", "g", "k" },
        new string[] { "d", "w", "o", "v", "l", "r", "p", "t", "x", "j", "ñ", "b", "u", "q", "m", "z" },
        new string[] { "a", "q", "c", "d", "t", "y", "f", "g", "w", "k", "m", "l", "s", "h", "v", "z" }
    };

    private List<GameObject> activeCards = new List<GameObject>();
    private int currentIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        // Initialize carousel
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);
        selectButton.onClick.AddListener(SaveSelectedCard);

        // Load saved index if exists
        currentIndex = PlayerPrefs.GetInt("CartaSeleccionada", 0);

        InitializeCarousel();
        UpdateButtonsInteractivity();
    }

    void InitializeCarousel()
    {
        ClearAllCards();

        if (cardSprites.Count == 0) return;

        // Make sure we have enough card data
        if (cardSprites.Count != cardDataArrays.Length)
        {
            Debug.LogWarning("El número de sprites no coincide con el número de arreglos de datos. Ajusta uno de ellos.");
            return;
        }

        activeCards.Add(CreateCard(GetLeftIndex(), leftPos.position, sideCardsScale));
        activeCards.Add(CreateCard(currentIndex, centerPos.position, 1f));
        activeCards.Add(CreateCard(GetRightIndex(), rightPos.position, sideCardsScale));
    }

    void ClearAllCards()
    {
        foreach (var card in activeCards)
        {
            if (card != null) Destroy(card);
        }
        activeCards.Clear();
    }

    public void Next()
    {
        if (CanTransition()) StartCoroutine(AnimateTransition(true));
    }

    public void Previous()
    {
        if (CanTransition()) StartCoroutine(AnimateTransition(false));
    }

    bool CanTransition()
    {
        return !isTransitioning && cardSprites.Count > 1;
    }

    IEnumerator AnimateTransition(bool movingNext)
    {
        isTransitioning = true;
        UpdateButtonsInteractivity(false);

        // 1. Crear nueva carta que entrará
        int newCardIndex = movingNext ?
            (currentIndex + 2) % cardSprites.Count :
            (currentIndex - 2 + cardSprites.Count) % cardSprites.Count;

        Vector3 startPos = movingNext ? hiddenRightPos.position : hiddenLeftPos.position;
        GameObject newCard = CreateCard(newCardIndex, startPos, sideCardsScale);

        // 2. Animar cartas existentes
        if (movingNext)
        {
            // Izquierda sale
            activeCards[0].transform.DOMove(hiddenLeftPos.position, transitionDuration)
                .OnComplete(() => Destroy(activeCards[0]));

            // Centro va a izquierda
            activeCards[1].transform.DOMove(leftPos.position, transitionDuration);
            activeCards[1].transform.DOScale(sideCardsScale, transitionDuration);

            // Derecha va a centro
            activeCards[2].transform.DOMove(centerPos.position, transitionDuration);
            activeCards[2].transform.DOScale(1f, transitionDuration);

            // Nueva carta entra a derecha
            newCard.transform.DOMove(rightPos.position, transitionDuration);
        }
        else
        {
            // Derecha sale
            activeCards[2].transform.DOMove(hiddenRightPos.position, transitionDuration)
                .OnComplete(() => Destroy(activeCards[2]));

            // Centro va a derecha
            activeCards[1].transform.DOMove(rightPos.position, transitionDuration);
            activeCards[1].transform.DOScale(sideCardsScale, transitionDuration);

            // Izquierda va a centro
            activeCards[0].transform.DOMove(centerPos.position, transitionDuration);
            activeCards[0].transform.DOScale(1f, transitionDuration);

            // Nueva carta entra a izquierda
            newCard.transform.DOMove(leftPos.position, transitionDuration);
        }

        yield return new WaitForSeconds(transitionDuration);

        // 3. Actualizar lista de cartas
        List<GameObject> newActiveCards = new List<GameObject>();

        if (movingNext)
        {
            newActiveCards.Add(activeCards[1]); // Ex-centro ahora izquierda
            newActiveCards.Add(activeCards[2]); // Ex-derecha ahora centro
            newActiveCards.Add(newCard);        // Nueva carta derecha
        }
        else
        {
            newActiveCards.Add(newCard);        // Nueva carta izquierda
            newActiveCards.Add(activeCards[0]); // Ex-izquierda ahora centro
            newActiveCards.Add(activeCards[1]); // Ex-centro ahora derecha
        }

        // 4. Actualizar índice y estado
        currentIndex = movingNext ?
            (currentIndex + 1) % cardSprites.Count :
            (currentIndex - 1 + cardSprites.Count) % cardSprites.Count;

        activeCards = newActiveCards;
        isTransitioning = false;
        UpdateButtonsInteractivity();
    }

    GameObject CreateCard(int spriteIndex, Vector3 position, float scale)
    {
        GameObject card = Instantiate(cardPrefab, position, Quaternion.identity, transform);
        card.GetComponent<Image>().sprite = cardSprites[spriteIndex];
        card.transform.localScale = Vector3.one * scale;
        return card;
    }

    int GetLeftIndex()
    {
        return (currentIndex - 1 + cardSprites.Count) % cardSprites.Count;
    }

    int GetRightIndex()
    {
        return (currentIndex + 1) % cardSprites.Count;
    }

    void UpdateButtonsInteractivity(bool interactable = true)
    {
        previousButton.interactable = interactable && cardSprites.Count > 1;
        nextButton.interactable = interactable && cardSprites.Count > 1;
    }

    public void SaveSelectedCard()
    {
        if (currentIndex >= 0 && currentIndex < cardDataArrays.Length)
        {
            PlayerPrefs.SetInt("CartaSeleccionada", currentIndex);
            PlayerPrefs.SetString("CartasSeleccionadas", string.Join(",", cardDataArrays[currentIndex]));
            PlayerPrefs.Save();
            Debug.Log($"Carta guardada: Índice {currentIndex}, Datos: {string.Join(",", cardDataArrays[currentIndex])}");
        }
        else
        {
            Debug.LogError("Índice de carta seleccionada fuera de rango");
        }
    }
}