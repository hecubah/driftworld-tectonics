using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct CS_Triangle // size of 36 B
{
    Vector3 A;
    Vector3 B;
    Vector3 C;
    public CS_Triangle(Vector3 A_p, Vector3 B_p, Vector3 C_p)
    {
        A = A_p;
        B = B_p;
        C = C_p;
    }
}