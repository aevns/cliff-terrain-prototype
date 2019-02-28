using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainBlock : MonoBehaviour
{
    #region Block Settings
    [HideInInspector] [SerializeField] private TerrainSystem terrainSystem;
    [HideInInspector] [SerializeField] public int XPosition { get; private set; }
    [HideInInspector] [SerializeField] public int ZPosition { get; private set; }
    [HideInInspector] [SerializeField] public int BlockWidth { get; private set; }
    [HideInInspector] [SerializeField] public int BlockLength { get; private set; }
    #endregion

    #region Serialized Block Data
    // Data is stored as:
    // {x->x+1 intercept offset, height, y->y+1 intercept offset}
    [HideInInspector] [SerializeField] private Vector3[,] cellData;
    #endregion

    #region Methods
    public void Build(
        TerrainSystem terrainSystem,
        int xPosition,
        int zPosition,
        int blockWidth,
        int blockLength
        )
    {
        this.terrainSystem = terrainSystem;
        this.XPosition = xPosition;
        this.ZPosition = zPosition;
        this.BlockWidth = blockWidth;
        this.BlockLength = blockLength;

        Sample();
        GenerateMesh();
    }

    public Vector3[] GetCellVertices(int i, int j)
    {
        return new Vector3[4]{
            cellData[i,j],
            cellData[i,j+1],
            cellData[i+1,j+1],
            cellData[i+1,j]
        };
    }

    private void Sample()
    {
        cellData = terrainSystem.GetComponent<TerrainSampler>().Sample(XPosition, ZPosition, BlockWidth, BlockLength);
    }

    private void GenerateMesh()
    {
        terrainSystem.GetComponent<TerrainBuilder>().Build(this);

    }
    #endregion
}