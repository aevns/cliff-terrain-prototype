using UnityEngine;

public abstract class TerrainSampler : MonoBehaviour {

    abstract public Vector3[,] Sample(int width, int length, int xOffset, int yOffset);

}