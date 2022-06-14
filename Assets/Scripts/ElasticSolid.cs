using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


public class ElasticSolid : MonoBehaviour
{
    public ElasticSolid()
    {
        pausa = true;
        timeStep = 0.01f;
        gravedad = new Vector3(0.0f, -9.81f, 0.0f);
        metodoDeIntegracion = Integration.Symplectic;
        visible = true;
    }

    public enum Integration
    {
        Explicit = 0,
        Symplectic = 1
    }

    #region Variables editables en Unity
    [Header("Controlar Simulación")]
    public bool pausa;
    public bool visible;
    public float timeStep;

    [Header("Ajustar Simulación")]
    public Vector3 gravedad;
    public Integration metodoDeIntegracion;
    public float fuerzaDeTraccion = 500;
    public float densidadDeMasa; 
    public float densidadDeRigidez; 
    public float damping = 1.0f;

    [Header("Ficheros TetGen")]
    public TextAsset ficheroNode;
    public TextAsset ficheroEle;
    #endregion

    #region Variables fijas
    private Mesh mesh;
    private Vector3[] vertices;

    public List<Node> ListaDeNodos = new List<Node>();
    public List<Spring> ListaDeMuelles = new List<Spring>();
    public List<Tetrahedron> ListaTetraedros = new List<Tetrahedron>();

    //Diccionario para evitar las aristas repetidas (Requisito 4)
    private Dictionary<Vector2, Spring> aristasUnicas = new Dictionary<Vector2, Spring>();
    #endregion


    public void Awake()
    {

        #region Carga de una malla de tetraedros por fichero
        //Leemos el fichero de los nodos - ayuda de parser
        /*El fichero de nodos está compuesto por x filas que tienen un índice y las tres posiciones de los nodos
         * El primer elemento de la fila es el indice de la misma, por lo que nos interesa empezar a mirar a partir del
         * seundo elemento de cada una, formando las posiciones de los nodos con los elementos 2, 3 y 4 de cada fila
         * 
         * Se emplea CultureInfo.InvariantCulture para conservar el formato del archivo y poder leerlo correctamente
         */
        string[] linea = ficheroNode.text.Split(new string[] { " ", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 5; i < linea.Length; i += 4)
        {
            float x = float.Parse(linea[i], CultureInfo.InvariantCulture);
            float y = float.Parse(linea[i + 1], CultureInfo.InvariantCulture);
            float z = float.Parse(linea[i + 2], CultureInfo.InvariantCulture);

            ListaDeNodos.Add(new Node(transform.TransformPoint(new Vector3(x, y, z))));
        }

        //Leemos el fichero que indica de que nodos se compone cada tetraedro - ayuda de parser
        /*El fichero de tetraedros está compuesto por x filas que tienen un índice y los cuatro nodos que forman cada tetraedro
         * El primer elemento de la fila es el índice de la misma, por lo que nos interesa empezar a mirar a partir del
         * segundo elemento de cada una, formando los tetraedros con los elementos 2, 3, 4 de cada fila
         * 
         * Se emplea CultureInfo.InvariantCulture para conservar el formato del archivo y poder leerlo correctamente
         */
        linea = ficheroEle.text.Split(new string[] { " ", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        var contador = 0;
        for (int i = 4; i < linea.Length; i += 5)
        {
            var nodo1 = int.Parse(linea[i], CultureInfo.InvariantCulture) - 1;
            var nodo2 = int.Parse(linea[i + 1], CultureInfo.InvariantCulture) - 1;
            var nodo3 = int.Parse(linea[i + 2], CultureInfo.InvariantCulture) - 1;
            var nodo4 = int.Parse(linea[i + 3], CultureInfo.InvariantCulture) - 1;

            var tetraedroAuxiliar = new Tetrahedron(ListaDeNodos[nodo1], ListaDeNodos[nodo2], ListaDeNodos[nodo3], ListaDeNodos[nodo4]);
            ListaTetraedros.Add(tetraedroAuxiliar);

            //Obtenemos el volumen del tetraedro
            var VolumenTetraedro = tetraedroAuxiliar.CalcularVolumen();

            //Guardamos todos los nodos de cada tetraedro en un array auxiliar
            int[] nodosAuxiliares = { nodo1, nodo2, nodo3, nodo4 };

            //Recorremos la lista auxiliar de nodos de tetraedros por pares
            for (int j = 0; j < nodosAuxiliares.Length - 1; j++)
            {
                for (int k = j + 1; k < nodosAuxiliares.Length; k++)
                {
                    //Vector2Int para transformar de floar a int
                    //Se ordenan los nodos para que siempre sigan un mismo orden y poder comparlarlas eficientemente, el nodo mas pequeño primero y el mas grande
                    //despues
                    Vector2Int arista = new Vector2Int(Mathf.Min(nodosAuxiliares[j], nodosAuxiliares[k]), Mathf.Max(nodosAuxiliares[j], nodosAuxiliares[k]));
                    contador += 1;
                    //Si la arista no esta en el diccionario se añade 
                    if (!aristasUnicas.ContainsKey(arista))
                    {
                        //Creamos la nueva arista
                        Spring nuevaArista = new Spring(ListaDeNodos[arista.x], ListaDeNodos[arista.y]);
                        //Calculamos la masa del tetraedro en esa arista
                        nuevaArista.MasaTetraedro(VolumenTetraedro / 6);
                        //Añadimos la masa y el tetraedro al diccionario
                        aristasUnicas.Add(arista, nuevaArista);
                        //Creamos la arista
                        ListaDeMuelles.Add(nuevaArista);
                    }
                }
            }
        }
        //Comprobación de que las aristas creadas son únicas
        Debug.Log("Total aristas contando duplicadas: " + contador);
        Debug.Log("Total aristas sin contar duplicadas: " + ListaDeMuelles.Count);

        #endregion

        #region Malla de triángulos embebida
        //Accedemos a la malla de triángululos del GameObject
        mesh = GetComponent<MeshFilter>().mesh;

        //Recorremos todos los vertices de la malla
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            //Recorremos la lista de tetraedros
            for (int j = 0; j < ListaTetraedros.Count; j++)
            {
                //Calculamos volumen total de cada tetraedro para poder calcular después las coordenadas baricéntricas del punto p dentro del tetraedro
                var V = ListaTetraedros[j].CalcularVolumen();

                //Comprobar si los tetraedros están dentro de los vértices de la malla
                //Para un punto 𝑝 interno a un tetraedro, la coordenada baricéntrica 𝑤𝑖 correpondiente a uno de los nodos 𝑝𝑖 del tetraedro se calcula como: 𝑤𝑖=𝑉𝑖/𝑉
                //Donde 𝑉 es el volumen total del tetraedro, y 𝑉𝑖 es el volumen del tetraedro formado por el punto 𝑝 y los otros 3 nodos del tetraedro, excluyendo a 𝑝𝑖
                if (ListaTetraedros[j].IsInside(transform.TransformPoint(mesh.vertices[i])))
                {
                    var v1 = VolumenTetraedro(transform.TransformPoint(mesh.vertices[i]), ListaTetraedros[j].Node2.pos, ListaTetraedros[j].Node3.pos, ListaTetraedros[j].Node4.pos);
                    var v2 = VolumenTetraedro(transform.TransformPoint(mesh.vertices[i]), ListaTetraedros[j].Node1.pos, ListaTetraedros[j].Node3.pos, ListaTetraedros[j].Node4.pos);
                    var v3 = VolumenTetraedro(transform.TransformPoint(mesh.vertices[i]), ListaTetraedros[j].Node1.pos, ListaTetraedros[j].Node2.pos, ListaTetraedros[j].Node4.pos);
                    var v4 = VolumenTetraedro(transform.TransformPoint(mesh.vertices[i]), ListaTetraedros[j].Node1.pos, ListaTetraedros[j].Node2.pos, ListaTetraedros[j].Node3.pos);
                    var w1 =  v1 / V;
                    var w2 =  v2 / V;
                    var w3 =  v3 / V;
                    var w4 =  v4 / V;

                    //Peso de cada vertice dependiendo de en que tetraedo esta incluido
                    ListaTetraedros[j].ListaVerticesPesados.Add(new PesadoDeVertices(i, w1, w2, w3, w4));
                }

                //Calculo de la masa del tetraedro
                var MasaTetraedro = densidadDeMasa * V;

                //Actualzamos la masa de cada nodo del tetraedro
                ListaTetraedros[j].Node1.ActualizarMasa(MasaTetraedro * 0.25f);
                ListaTetraedros[j].Node2.ActualizarMasa(MasaTetraedro * 0.25f);
                ListaTetraedros[j].Node3.ActualizarMasa(MasaTetraedro * 0.25f);
                ListaTetraedros[j].Node4.ActualizarMasa(MasaTetraedro * 0.25f);
            }
        }

        //Calcular la masa de cada nodo dependiendo de la cantidad de tetraedros a los que pertenece
        foreach (var nodo in ListaDeNodos)
        {
            nodo.masa = nodo.masa / nodo.CantTetras;
        }
        #endregion
    }

    public void Update()
    {
        //Si esta pausado
        if (Input.GetKeyUp(KeyCode.P))
            pausa = !pausa;

        if (Input.GetKeyUp(KeyCode.V))
            visible = !visible;

        //Guardamos los vertices del mesh
        vertices = mesh.vertices;

        foreach (var tetra in ListaTetraedros)
        {
            foreach (var ver in tetra.ListaVerticesPesados)
            {
                //Se calcula la posicion de cada vertice dependiendo de la posición de los nodos y el peso de los vértices
                vertices[ver.IdMesh] = tetra.Node1.pos * ver.w1 + tetra.Node2.pos * ver.w2 + tetra.Node3.pos * ver.w3 + tetra.Node4.pos * ver.w4;
                //Pasamos la posicion de los vertices de corrdenadas globales a locales
                vertices[ver.IdMesh] = transform.InverseTransformPoint(vertices[ver.IdMesh]);
            }
        }
        //Guardamos los vertices actualizados en el mesh
        mesh.vertices = vertices;
    }

    public void FixedUpdate()
    {
        if (pausa)
            return; // Not simulating

        foreach (var node in ListaDeNodos)
        {
            node.force = Vector3.zero;
            node.ComputeForces(gravedad, damping);
        }

        foreach (var spring in ListaDeMuelles)
        {
            spring.ComputeForces(fuerzaDeTraccion, densidadDeRigidez, damping);
        }

        foreach (var node in ListaDeNodos)
        {
            if (metodoDeIntegracion == Integration.Symplectic)
            {
                node.SolveForcesSymplectic(timeStep);
            }
            if (metodoDeIntegracion == Integration.Explicit)
            {
                node.SolveForcesExplicit(timeStep);
            }
        }

        foreach (var spring in ListaDeMuelles)
        {
            spring.UpdateLength();
        }
    }
    public float VolumenTetraedro(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Mathf.Abs(Vector3.Dot((p1 - p0), Vector3.Cross((p2 - p0), (p3 - p0)))) / 6;
    }

    //Depuración visual de los nodos y muelles usando la herramienta de guizmos (requisito 1)
    public void OnDrawGizmos()
    {
        //Comprobamos si los nodos van a ser visibles
        if (visible == true)
        {
            //Recorremos la lista de nodos y los pintamos
            foreach (var nodo in ListaDeNodos)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(nodo.pos, 0.2f);
            }
            //Recorremos la lista de muelles y los pintamos
            foreach (var muelle in ListaDeMuelles)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(muelle.nodeA.pos, muelle.nodeB.pos);
            }
        }
    }
}