using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetrahedron
{
    public Node Node1;
    public Node Node2;
    public Node Node3;
    public Node Node4;
    public List<PesadoDeVertices> ListaVerticesPesados = new List<PesadoDeVertices>();

    public Tetrahedron(Node n1, Node n2, Node n3, Node n4)
    {
        Node1 = n1;
        Node2 = n2;
        Node3 = n3;
        Node4 = n4;
    }

    public float CalcularVolumen()
    {
        return Mathf.Abs(Vector3.Dot((Node2.pos - Node1.pos), Vector3.Cross((Node3.pos - Node1.pos), (Node4.pos - Node1.pos)))) / 6;
    }

    //Identificación del tetraedro contenedor
    public bool IsInside(Vector3 posVertice)
    {
        var centroCara1 = (Node1.pos + Node2.pos + Node3.pos) / 3;
        var centroCara2 = (Node1.pos + Node3.pos + Node4.pos) / 3;
        var centroCara3 = (Node1.pos + Node2.pos + Node4.pos) / 3;
        var centroCara4 = (Node2.pos + Node3.pos + Node4.pos) / 3;
        var normal1 = Vector3.Cross(Node1.pos - Node2.pos, Node3.pos - Node2.pos);
        var normal2 = Vector3.Cross(Node1.pos - Node3.pos, Node4.pos - Node3.pos);
        var normal3 = Vector3.Cross(Node2.pos - Node1.pos, Node4.pos - Node1.pos);
        var normal4 = Vector3.Cross(Node3.pos - Node2.pos, Node4.pos - Node3.pos);
        var productoEscalar1 = Vector3.Dot(centroCara1 - posVertice, normal1);
        var productoEscalar2 = Vector3.Dot(centroCara2 - posVertice, normal2);
        var productoEscalar3 = Vector3.Dot(centroCara3 - posVertice, normal3);
        var productoEscalar4 = Vector3.Dot(centroCara4 - posVertice, normal4);

        //El vertice esta dentro del tetraedro respecto a esta cara
        if (productoEscalar1 > 0)
        {
            if (productoEscalar2 > 0)
            {
                if (productoEscalar3 > 0)
                {
                    if (productoEscalar4 > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
public struct PesadoDeVertices
{
    public int IdMesh;

    public float w1;
    public float w2;
    public float w3;
    public float w4;

    public PesadoDeVertices(int idx, float w1, float w2, float w3, float w4)
    {
        IdMesh = idx;

        this.w1 = w1;
        this.w2 = w2;
        this.w3 = w3;
        this.w4 = w4;
    }
}