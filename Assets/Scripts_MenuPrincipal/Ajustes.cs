using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ajustes : MonoBehaviour
{
    public GameObject panelAjustes;

    


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MostrarAjustes()
    {
        panelAjustes.SetActive(true);
    }

    public void QuitarAjustes()
    {
        panelAjustes.SetActive(false);
    }
}
