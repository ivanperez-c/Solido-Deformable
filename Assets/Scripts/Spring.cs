using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring
{
    public Node nodeA, nodeB;

    public float Length0;
    public float Length;
    public float masa;

    public Spring(Node nodeA, Node nodeB)
    {
        this.nodeA = nodeA;
        this.nodeB = nodeB;

        Length = (nodeA.pos - nodeB.pos).magnitude;
        Length0 = (nodeA.pos - nodeB.pos).magnitude;
    }

    public void UpdateLength()
    {
        Length = (nodeA.pos - nodeB.pos).magnitude;
    }

    public void ComputeForces(float fuerzaTraccion, float k, float damping)
    {
        var u = nodeA.pos - nodeB.pos;
        u.Normalize();
        float volumen = masa / k;
        Vector3 force = (-(volumen / (Length0 * Length0)) * k * (Length - Length0) * ((nodeA.pos - nodeB.pos) / Length));
        force += force * fuerzaTraccion;
        force += -damping * (nodeA.vel - nodeB.vel);
        force += -damping * Vector3.Dot(u, (nodeA.vel - nodeB.vel)) * u;
        nodeA.force += force;
        nodeB.force -= force;
    }

    public void MasaTetraedro(float w)
    {
        masa = w;
    }
}