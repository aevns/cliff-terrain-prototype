using System.Collections.Generic;
using UnityEngine;

public class TerrainBuilder : MonoBehaviour
{

    #region Cell Data
    [HideInInspector] [SerializeField] private TerrainSystem terrainSystem;
    /*
    Every type of cell required to create the terrainSystem must
    be provided. Cell names are based on the occurance of walls,
    clockwise, starting from vertex (0,0), with A representing a
    rising wall, and B representing a falling one.

    These 15 cell types (and their 6 reflected and many rotated
    variations) are needed for this model. Another possible
    method, with fewer base cases and the ability to create
    layered walls (essential for quality wall smoothing) would
    be to split cells in half for all cases after 00BA, or to
    split all cells into quarters (or equivalently, create walls
    along edges for every 2nd row/column of vertices, with a
    higher resolution base terrain).
    */
    [SerializeField] private Mesh M0000;
    [SerializeField] private Mesh M000A;
    // 000A->000B
    [SerializeField] private Mesh M00AB;
    [SerializeField] private Mesh M00BA;
    [SerializeField] private Mesh M0A0B;
    [SerializeField] private Mesh M0AAB;
    // 0AAB->0ABB
    [SerializeField] private Mesh M0BBA;
    // 0BBA->0BAA
    [SerializeField] private Mesh M0ABA;
    // 0ABA->0BAB
    [SerializeField] private Mesh MAAAB;
    // AAAB->ABBB
    [SerializeField] private Mesh MAABB0;
    [SerializeField] private Mesh MAABB1;
    // AABB1->AABB2
    [SerializeField] private Mesh MABAB0;
    [SerializeField] private Mesh MABAB1;
    [SerializeField] private Mesh MABAB2;
    [SerializeField] private Mesh MABAB3;

    [HideInInspector] [SerializeField] private TerrainCell[] cells;

    #endregion

    #region Data Validation
    private void OnValidate()
    {
        terrainSystem = gameObject.GetComponent<TerrainSystem>();

        cells = new TerrainCell[21];
        cells[0] = new TerrainCell(M0000);
        cells[1] = new TerrainCell(M000A);
        cells[2] = new TerrainCell(M000A, true, 3);// 000B
        cells[3] = new TerrainCell(M00AB);
        cells[4] = new TerrainCell(M00BA);
        cells[5] = new TerrainCell(M0A0B);

        cells[6] = new TerrainCell(M0AAB);
        cells[7] = new TerrainCell(M0AAB, true, 1);// 0ABB
        cells[8] = new TerrainCell(M0BBA);
        cells[9] = new TerrainCell(M0BBA, true, 1);// 0BAA
        cells[10] = new TerrainCell(M0ABA);
        cells[11] = new TerrainCell(M0ABA, true, 1);// 0BAB

        cells[12] = new TerrainCell(MAAAB);
        cells[13] = new TerrainCell(MAAAB, true, 0);// ABBB
        cells[14] = new TerrainCell(MAABB0);
        cells[15] = new TerrainCell(MAABB1);
        cells[16] = new TerrainCell(MAABB1, true, 0);// AABB2
        cells[17] = new TerrainCell(MABAB0);
        cells[18] = new TerrainCell(MABAB1);
        cells[19] = new TerrainCell(MABAB2);
        cells[20] = new TerrainCell(MABAB3);
    }
    #endregion

    #region Geometry Data
    // Dictionaries are used to match vertices as we go (for smooth surfaces).
    // Issue: No care is taken to match vetex normals at currentBlock boundaries.
    // Normals are generated automatically after the fact (for now).
    private TerrainBlock currentBlock;

    private Dictionary<Vector3, int> groundIndices = new Dictionary<Vector3, int>();
    private List<int> groundTris = new List<int>();
    private List<int> wallTris = new List<int>();

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector2> uv2s = new List<Vector2>();
    #endregion

    #region Mesh Construction
    public void Build(TerrainBlock terrainBlock)
    {
        // Ideally this would be handled by the terrain block, but cell construction
        // needs to be done here (as there could be multiple types of TerrainBuilder
        // in the future), making that unreasonable. Instead, this handles both how
        // the mesh should be generated, and the mesh generation itself.

        currentBlock = terrainBlock;
        // List sizes are just estimates, to mitigate the cost of resizing.
        if (vertices == null || vertices.Count == 0)
        {
            groundIndices = new Dictionary<Vector3, int>();
            groundTris = new List<int>(currentBlock.BlockWidth * currentBlock.BlockLength * 8);
            wallTris = new List<int>(currentBlock.BlockWidth * currentBlock.BlockLength * 2);

            vertices = new List<Vector3>(currentBlock.BlockWidth * currentBlock.BlockLength * 2);
            uvs = new List<Vector2>(currentBlock.BlockWidth * currentBlock.BlockLength * 2);
            uv2s = new List<Vector2>(currentBlock.BlockWidth * currentBlock.BlockLength * 2);
        }
        else
        {
            groundIndices.Clear();
            groundTris.Clear();
            wallTris.Clear();
            vertices.Clear();
            uvs.Clear();
            uv2s.Clear();
        }

        for (int j = 0; j < currentBlock.BlockLength; j++)
        {
            for (int i = 0; i < currentBlock.BlockWidth; i++)
            {
                BuildCell(i, j);
            }
        }

        Mesh mesh = new Mesh { subMeshCount = 2 };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uv2s);
        mesh.SetTriangles(groundTris, 0);
        mesh.SetTriangles(wallTris, 1);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        currentBlock.GetComponent<MeshFilter>().sharedMesh = mesh;
        currentBlock.GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    #endregion

    #region Cell Construction
    public void BuildCell(int i, int j)
    {
        Vector3 offset = new Vector3(i, 0, j);
        Vector3[] cellVertices = currentBlock.GetCellVertices(i, j);
        // Vertical scaling should be done here in mesh generation
        // instead of the sampling stage, as sampling should be
        // independant of all mesh generation parameters, allowing
        // them to be changed without resampling.
        for (int k = 0; k < 4; k++)
        {
            cellVertices[k].y *= terrainSystem.VerticalScale;
        }


        // Determining the type of cell to generate, as well as its rotation.
        // selection is based on some common properties, but unfortunately,
        // it isn't exactly straightforward.
        float[] walls = new float[4];
        int wallN = 0, bN = 0, modulus = -1, modulusB = -1;
        bool sameEven, sameOdd;

        walls[3] = IsWall(cellVertices[3], cellVertices[0]) ? (cellVertices[0].y - cellVertices[3].y) : 0;
        for (int k = 0; k < 4; k++)
        {
            walls[k] = IsWall(cellVertices[k], cellVertices[(k + 1) % 4]) ? (cellVertices[(k + 1) % 4].y - cellVertices[k].y) : 0;
            wallN += walls[k] == 0 ? 0 : 1;
            bN += walls[k] < 0 ? 1 : 0;
            if (walls[k] == 0 && walls[(k + 3) % 4] != 0)
                modulus = k;
            if (walls[k] >= 0 && walls[(k + 3) % 4] < 0)
                modulusB = k;
        }
        sameEven = walls[0] * walls[2] > 0 || walls[0] == walls[2];
        sameOdd = walls[1] * walls[3] > 0 || walls[1] == walls[3];

        switch (wallN)
        {
            case 0:
                //0-5
                ConstructCell(offset, cellVertices, 0, 0);
                break;
            case 1:
                if (bN == 0)
                    ConstructCell(offset, cellVertices, modulus, 1);
                else
                    ConstructCell(offset, cellVertices, modulus, 2);
                break;
            case 2:
                if (sameEven || sameOdd)
                    ConstructCell(offset, cellVertices, modulus, 5);
                else if (modulusB == modulus)
                    ConstructCell(offset, cellVertices, modulus, 3);
                else
                    ConstructCell(offset, cellVertices, modulus, 4);
                break;
            case 3:
                //6-11
                if (sameEven || sameOdd)
                {
                    if (bN == 1)
                        ConstructCell(offset, cellVertices, modulus, 10);
                    else
                        ConstructCell(offset, cellVertices, modulus, 11);
                }
                else if (bN == 1 && modulusB == modulus)
                    ConstructCell(offset, cellVertices, modulus, 6);
                else if (modulusB == modulus)
                    ConstructCell(offset, cellVertices, modulus, 7);
                else if (bN == 1)
                    ConstructCell(offset, cellVertices, modulus, 9);
                else
                    ConstructCell(offset, cellVertices, modulus, 8);
                break;
            default:
                //17-20:
                if (sameEven && sameOdd)
                {
                    if (!IsWall(cellVertices[modulusB], cellVertices[(modulusB + 2) % 4]))
                        ConstructCell(offset, cellVertices, modulusB, 17);
                    else if (!IsWall(cellVertices[(modulusB + 1) % 4], cellVertices[(modulusB + 3) % 4]))
                        ConstructCell(offset, cellVertices, modulusB, 20);
                    else if (cellVertices[(modulusB) % 4].y < cellVertices[(modulusB + 2) % 4].y)
                        ConstructCell(offset, cellVertices, modulusB, 18); // difference between 18 and 19 is minor, has same edges; just using 18 for now.
                    else
                        ConstructCell(offset, cellVertices, (modulusB + 2) % 4, 19);
                }
                //12-13
                else if (bN == 1 && (sameEven || sameOdd))
                    ConstructCell(offset, cellVertices, modulusB, 12);
                else if (bN == 3 && (sameEven || sameOdd))
                    ConstructCell(offset, cellVertices, modulusB, 13);
                //14-16
                else
                {
                    if (!IsWall(cellVertices[(modulusB + 1) % 4], cellVertices[(modulusB + 3) % 4]))
                        ConstructCell(offset, cellVertices, modulusB, 14);
                    else if (cellVertices[(modulusB + 1) % 4].y > cellVertices[(modulusB + 3) % 4].y)
                        ConstructCell(offset, cellVertices, modulusB, 16);
                    else
                        ConstructCell(offset, cellVertices, modulusB, 15);
                }
                break;
        }

    }

    private bool IsWall(Vector3 v1, Vector3 v2)
    {
        float h1 = v1.y, h2 = v2.y;
        // if the height difference exceeds max cliff height,
        // or if they're seperated by a full layer region,
        // then a wall will be present.
        return (
            (Mathf.Abs(h2 - h1) > terrainSystem.CliffHeight) ||
            (h2 % terrainSystem.CliffHeight < terrainSystem.CliffHeight * terrainSystem.Layering &&
            h1 % terrainSystem.CliffHeight < terrainSystem.CliffHeight * terrainSystem.Layering &&
            h2 - h1 != h2 % terrainSystem.CliffHeight - h1 % terrainSystem.CliffHeight)
            );
    }

    void ConstructCell(Vector3 offset, Vector3[] blockPoints, int modulus, int cellNumber)
    {
        TerrainCell cell = cells[cellNumber];
        Vector3[] triVerts = new Vector3[3];
        int index;

        for (int i = 0; i < cell.triCount; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                index = cell.tris[i + j];
                triVerts[j] = cell.vertices[modulus, index];
                triVerts[j].y = 0;

                for (int k = 0; k < 4; k++)
                {
                    triVerts[j].y += blockPoints[(k + modulus) % 4].y * cell.heightWeights[index][k];
                }
                triVerts[j].x += blockPoints[0].x * cell.wallWeights[index][(3 + 4 - modulus) % 4] + blockPoints[1].x * cell.wallWeights[index][(1 + 4 - modulus) % 4];
                triVerts[j].z += blockPoints[0].z * cell.wallWeights[index][(4 - modulus) % 4] + blockPoints[3].z * cell.wallWeights[index][(2 + 4 - modulus) % 4];
                triVerts[j] += offset;
            }
            MakeTri(triVerts);
        }
    }
    #endregion

    #region Geometry Construction
    void MakeTri(params Vector3[] points)
    {
        Vector3 uVector = Vector3.Cross(new Plane(points[0], points[1], points[2]).normal, Vector3.up);
        bool isGround = uVector.sqrMagnitude < .75f;

        int index;
        if (isGround)
        {
            for (int k = 0; k < 3; k++)
            {
                if (!groundIndices.TryGetValue(points[k], out index))
                {
                    vertices.Add(points[k]);
                    uvs.Add(new Vector2(points[k].x + currentBlock.XPosition, points[k].z + currentBlock.ZPosition));
                    uv2s.Add(new Vector2(points[k].x + currentBlock.XPosition, points[k].z + currentBlock.ZPosition));
                    index = vertices.Count - 1;
                    groundIndices.Add(points[k], index);
                }
                groundTris.Add(index);
            }
            return;
        }
        else
        {
            // Used to determine UV2 coords for wall 'outlines'.
            // Can't be done perfectly for quads on a per-tri basis,
            // so a vert is assumed to be top or bottom depending on
            // its position relative to the midpoint of the tri.
            float meanY = (points[0].y + points[1].y + points[2].y) / 3;
            float diffY = Mathf.Max(
                Mathf.Abs(points[1].y - points[0].y),
                Mathf.Max(
                    Mathf.Abs(points[2].y - points[1].y),
                    Mathf.Abs(points[0].y - points[2].y)
                    )
                );

            for (int k = 0; k < 3; k++)
            {
                vertices.Add(points[k]);
                uvs.Add(new Vector2(Vector3.Dot(uVector, new Vector3(points[k].x + currentBlock.XPosition, 0, points[k].z + currentBlock.ZPosition)), points[k].y));
                uv2s.Add(new Vector2(2f * terrainSystem.OutlineThickness / diffY, points[k].y <= meanY ? 0 : 1));
                index = vertices.Count - 1;
                wallTris.Add(index);
            }
        }
    }
    #endregion
}

#region Terrain Cell Definition
// Struct containing cell mesh information, in a more convenient format than pure mesh data.
// Mesh data currently needs to follow strict criteria.
// The first 4 vertex groups define height weighting, indexed clockwise starting at vertex (0,0).
// The next 4 vertex groups define offset weighting for walls.
struct TerrainCell
{
    public int triCount;
    public int vertCount;

    public int[] tris;
    public Vector3[,] vertices;

    public Vector4[] heightWeights;
    public Vector4[] wallWeights;

    public TerrainCell(Mesh mesh, bool reflected = false, int mod = 0)
    {
        triCount = mesh.triangles.Length;
        vertCount = mesh.vertices.Length;

        // 4 copies of vertex data is stored, one for each 90 degree rotation,
        // instead of applying the rotation every time we want to generate a cell.
        vertices = new Vector3[4, vertCount];

        // The ith corner of a cell affects the height of a vertex according to
        // heightWeights[i], and the offset of a wall affects it in the same way.
        // Corners and walls use indexes starting at (0,0) and going clockwise.
        heightWeights = new Vector4[vertCount];
        wallWeights = new Vector4[vertCount];

        // When reflected, the vertex order in a triangle must be reversed:
        if (reflected)
        {
            tris = new int[triCount];
            for (int v = 0; v < triCount; v += 3)
            {
                tris[v] = mesh.triangles[v + 2];
                tris[v + 1] = mesh.triangles[v + 1];
                tris[v + 2] = mesh.triangles[v];
            }
        }
        else
            tris = mesh.triangles;

        for (int i = 0; i < vertCount; i++)
        {
            vertices[0, i] = Transformed(mesh.vertices[i], mod, reflected);
            vertices[1, i] = Transformed(mesh.vertices[i], mod + 1, reflected);
            vertices[2, i] = Transformed(mesh.vertices[i], mod + 2, reflected);
            vertices[3, i] = Transformed(mesh.vertices[i], mod + 3, reflected);

            int[] bindices = new int[4];
            bindices[0] = mesh.boneWeights[i].boneIndex0;
            bindices[1] = mesh.boneWeights[i].boneIndex1;
            bindices[2] = mesh.boneWeights[i].boneIndex2;
            bindices[3] = mesh.boneWeights[i].boneIndex3;

            float[] bweights = new float[4];
            bweights[0] = mesh.boneWeights[i].weight0;
            bweights[1] = mesh.boneWeights[i].weight1;
            bweights[2] = mesh.boneWeights[i].weight2;
            bweights[3] = mesh.boneWeights[i].weight3;

            Vector4 hs = Vector4.zero;
            Vector4 ws = Vector4.zero;
            float nobone = 0;

            // Vertex weights are currently calculated using bone weights;
            // it would likely be best to replace this system with one based
            // on blend shapes (bones are currently used to 'store' Blender's
            // vertex groups, but with the caveat of unwanted normalization).
            for (int b = 0; b < 4; b++)
            {
                if (reflected)
                {
                    if (bindices[b] < 4)
                    {
                        if (bindices[b] % 2 == 1)
                            hs[(4 - bindices[b] + mod) % 4] += bweights[b];
                        else
                            hs[(bindices[b] + mod) % 4] += bweights[b];
                    }
                    else if (bindices[b] < 8)
                        ws[(7 - bindices[b] + mod) % 4] += bweights[b];
                    else
                        nobone += bweights[b];
                }
                else
                {
                    if (bindices[b] < 4)
                        hs[bindices[b]] += bweights[b];
                    else if (bindices[b] < 8)
                        ws[bindices[b] % 4] += bweights[b];
                    else
                        nobone += bweights[b];
                }
            }

            float hsum = hs[0] + hs[1] + hs[2] + hs[3];
            float wsum = ws[0] + ws[1] + ws[2] + ws[3] + nobone;

            heightWeights[i] = hs / hsum;
            wallWeights[i] = ws / wsum;
        }
    }

    // Tool to rotate and reflect vertex data.
    private Vector3 Transformed(Vector3 point, float mod, bool reflected = false)
    {
        Vector3 temp = point;
        if (reflected)
        {
            temp.x = point.z;
            temp.z = point.x;
        }
        return Quaternion.Euler(0, 90 * mod, 0) * (temp - new Vector3(0.5f, 0f, 0.5f)) + new Vector3(0.5f, 0f, 0.5f);
    }
}
#endregion