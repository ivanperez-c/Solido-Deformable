using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 force;
    public bool isFixed;
    public float masa;
    public int CantTetras;

    public Node(Vector3 position)
    {
        pos = position;
        vel = Vector3.zero;
        force = Vector3.zero;
        isFixed = false;
    }

    public void ComputeForces(Vector3 gravity, float damping)
    {
        force += (masa * gravity) - (vel * damping);
    }

    public void SolveForcesSymplectic(float timeStep)
    {
        //Le aplicamos la fuerza simpléctica a los nodos si no estan fijos
        if (!isFixed)
        {
            vel += force * timeStep;
            pos += vel * timeStep;
        }
    }
    public void SolveForcesExplicit(float timeStep)
    {
        //Le aplicamos la fuerza explícita a los nodos si no estan fijos
        if (!isFixed)
        {
            pos += vel * timeStep;
            vel += force * timeStep;
        }
    }

    public void ActualizarMasa(float m)
    {
        masa += m;
        CantTetras++;
    }
}

