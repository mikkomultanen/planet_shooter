using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICaveSystem {
    bool insideCave(Vector2 coord);
    float caveFieldValue(Vector2 coord);
}