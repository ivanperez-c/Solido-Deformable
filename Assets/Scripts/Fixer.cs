using System.Collections.Generic;
using UnityEngine;

public class Fixer : MonoBehaviour {
    //Enlazamos el objeto que vamos a fijar en Unity
    public GameObject objeto;

    private Bounds bounds;
    void Start() {
        //Obtenemos el objeto que tiene el scrit ElasticSolid
        var solidoDeformable = objeto.GetComponent<ElasticSolid>();
        //Obtenemos la lista de nodos del objeto
        var nodes = solidoDeformable.ListaDeNodos;

        //Determinamos la zona que va a fijar
        bounds = GetComponent<Collider>().bounds;

        //Recorremos todos los nodos del objeto
        foreach (var nodo in nodes) {
            //Si los nodos están dentro de bounds(elemento fijador) fijamos los nodos, 
            //si no están dentro realizamos el continue para comprobar el siguiente nodo
            if (!bounds.Contains(nodo.pos)) continue;
            nodo.isFixed = true;
        }
    }
}