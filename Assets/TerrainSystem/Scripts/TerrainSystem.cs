using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TerrainBuilder))]
[ExecuteInEditMode]
public class TerrainSystem : MonoBehaviour
{
    #region Terrain System Settings
    [SerializeField] private float verticalScale = 4.0f;
    [SerializeField] [Range(0.0f, 5.0f)] public float cliffHeight = 0.5f;
    [Tooltip("Allows cliffs to be created below Cliff Height, if they are seperated by a height layer." +
        "These layers exist at height intervals equal to Cliff Height, and affect a height range of Cliff Height x Layering.")]
    [SerializeField] [Range(0.0f, 1.0f)] private float layering = 0.0f;
    
    [SerializeField] private int mapWidth = 64;
    [SerializeField] private int mapLength = 64;
    [SerializeField] private int blockSize = 32;

    [SerializeField] private Material groundMaterial;
    [SerializeField] private Material wallMateterial;
    [SerializeField] private float outlineThickness = 0.05f;
    #endregion

    #region Accessors
    public float VerticalScale
    {
        get { return verticalScale; }
    }
    public float CliffHeight
    {
        get { return cliffHeight; }
    }
    public float Layering
    {
        get { return layering; }
    }
    public float OutlineThickness
    {
        get { return outlineThickness; }
    }
    #endregion

    #region Private Data
    [SerializeField] [HideInInspector] private TerrainBlock[,] blocks;
    #endregion

    #region Validation
    private void OnValidate()
    {
        // Can't use RequireComponent on an interface, but still need one of these.
        // (assuming a FastImageSampler is wanted by default)
        if (!GetComponent<TerrainSampler>())
        {
            gameObject.AddComponent<FastImageSampler>();
        }
    }
    #endregion

    #region Terrain Generation
    public void FullUpdate()
    {
        TerrainSampler terrainSampler = GetComponent<TerrainSampler>();
        if (!terrainSampler) return;

        TerrainBlock[] tbs = GetComponentsInChildren<TerrainBlock>();
        foreach (TerrainBlock tb in tbs)
        {
            DestroyImmediate(tb.gameObject);
        }

        blocks = new TerrainBlock[(mapWidth + blockSize - 1) / blockSize, (mapLength + blockSize - 1) / blockSize];
        for (int j = 0; j < (mapLength + blockSize - 1) / blockSize; j++)
        {
            for (int i = 0; i < (mapWidth + blockSize - 1) / blockSize; i++)
            {
                blocks[i, j] = CreateBlock(i, j);
            }
        }
    }

    TerrainBlock CreateBlock(int i, int j)
    {
        if (mapWidth <= blockSize * i || mapLength <= blockSize * j)
        {
            Debug.Log("Block is beyond map dimensions.");
            return null;
        }

        GameObject blockObject = new GameObject("block(" + i + "," + j + ")");

        // Use transform of the terrain system to define the transform of the generated terrain
        blockObject.transform.SetParent(transform, false);
        blockObject.transform.localPosition = new Vector3(i * blockSize, 0, j * blockSize);

        // Blocks use the static flags, tags and layer of the terrain system
#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(blockObject, UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject));
#endif
        blockObject.layer = gameObject.layer;
        blockObject.tag = gameObject.tag;

        // Required components and settings
        blockObject.AddComponent<MeshFilter>();
        blockObject.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { groundMaterial, wallMateterial };
        blockObject.AddComponent<MeshCollider>();
        TerrainBlock block = blockObject.AddComponent<TerrainBlock>();

        // Setting the block data and building it
        block.Build(
            this,
            i * blockSize,
            j * blockSize,
            mapWidth - i * blockSize < blockSize ? mapWidth - i * blockSize : blockSize,
            mapLength - j * blockSize < blockSize ? mapLength - j * blockSize : blockSize
            );
        return block;
    }
#endregion
}