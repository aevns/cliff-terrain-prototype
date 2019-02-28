//include side projects with CV
//send to sascha.kava[...]@gmail.com

using UnityEngine;

public class SlowImageSampler : TerrainSampler
{
    public Texture2D heightMapTexture;
    [Tooltip("WARNING: texture scale follows the same rules as material texture scale, e.g. for a full 256x256 block map, it should be 1/256 x 1/256.")]
    public Vector2 scale = Vector2.zero;
    public Vector2 offset = Vector2.zero;

    private Vector2 textureSize;

    public override Vector3[,] Sample(int blockWidth, int blockLength, int blockOffsetX, int blockOffsetY)
    {
        textureSize = new Vector2(heightMapTexture.width, heightMapTexture.height);

        if (textureSize.x/blockWidth > 32 || textureSize.y / blockLength > 32)
        {
            Debug.Log("WARNING: Texture scale too high. Texture scale follows the same rules as material texture scale, e.g. for full 256x256 block map, it should be 1/256 x 1/256.");
            return null;
        }

        return BuildData(blockWidth, blockLength, blockOffsetX, blockOffsetY);
    }

    public Vector3[,] BuildData(int blockWidth, int blockLength, int blockOffsetX, int blockOffsetY)
    {
        Vector3[,] blockData = new Vector3[blockWidth + 1, blockLength + 1];
        Vector3[,] fitData = new Vector3[blockWidth + 1, blockLength + 1];
        Vector3[,] fitWeights = new Vector3[blockWidth + 1, blockLength + 1];

        Vector2 point0, point1;
        int q0, q1, r0, r1;
        bool has_top_left, has_top_right, has_bottom_left, has_bottom_right;

        for (int j = -1; j <= blockLength; j++)
        {
            for (int i = -1; i <= blockWidth; i++)
            {
                point0 = VertexPosition(new Vector2(blockOffsetX + i, blockOffsetY + j));
                point1 = VertexPosition(new Vector2(blockOffsetX + i + 1, blockOffsetY + j + 1));

                q0 = Mathf.RoundToInt(GetPixel(point0).x);
                q1 = Mathf.RoundToInt(GetPixel(point1).x);
                r0 = Mathf.RoundToInt(GetPixel(point0).y);
                r1 = Mathf.RoundToInt(GetPixel(point1).y);

                has_top_left = (i >= 0 && j >= 0);
                has_top_right = (i + 1 <= blockWidth && j >= 0);
                has_bottom_left = (i >= 0 && j + 1 <= blockLength);
                has_bottom_right = (i + 1 <= blockWidth && j + 1 <= blockLength);

                for (int q = q0; q < q1; q++)
                {
                    for (int r = r0; r < r1; r++)
                    {
                        Vector2 pos = PixelPosition(new Vector2(q, r));
                        float h = heightMapTexture.GetPixel(q, r).r;
                        Vector2 relPos;
                        Vector3 interpFactors;

                        if (has_top_left)
                        {
                            relPos = GetVertex(pos) - GetVertex(point0);
                            interpFactors = HermiteBoxSample2(relPos);
                            fitData[i, j] += interpFactors * h;
                            fitWeights[i, j] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_top_right)
                        {
                            relPos = GetVertex(pos) - GetVertex(new Vector2(point1.x, point0.y));
                            interpFactors = HermiteBoxSample2(relPos);
                            fitData[i + 1, j] += interpFactors * h;
                            fitWeights[i + 1, j] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_bottom_left)
                        {
                            relPos = GetVertex(pos) - GetVertex(new Vector2(point0.x, point1.y));
                            interpFactors = HermiteBoxSample2(relPos);
                            fitData[i, j + 1] += interpFactors * h;
                            fitWeights[i, j + 1] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_bottom_right)
                        {
                            relPos = GetVertex(pos) - GetVertex(point1);
                            interpFactors = HermiteBoxSample2(relPos);
                            fitData[i + 1, j + 1] += interpFactors * h;
                            fitWeights[i + 1, j + 1] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                    }
                }
            }
        }
        for (int j = 0; j <= blockLength; j++)
        {
            for (int i = 0; i <= blockWidth; i++)
            {
                fitData[i, j] = fitData[i, j].DivideBy(fitWeights[i, j]);
            }
        }
        for (int j = 0; j <= blockLength; j++)
        {
            for (int i = 0; i <= blockWidth; i++)
            {
                blockData[i, j] = new Vector3(
                    i < blockWidth ? HermiteInflection(fitData[i, j], fitData[i + 1, j], true) : 0.5f,
                    fitData[i, j].y,
                    j < blockLength ? HermiteInflection(fitData[i, j], fitData[i, j + 1], false) : 0.5f
                    );
            }
        }
        return blockData;
    }

    //EXPERIMENTAL:
    public Vector3[,] BuildDataPSample(int blockWidth, int blockLength, int blockOffsetX, int blockOffsetY)
    {
        Vector3[,] blockData = new Vector3[blockWidth + 1, blockLength + 1];
        Vector3[,] fitData = new Vector3[blockWidth + 1, blockLength + 1];
        Vector3[,] fitWeights = new Vector3[blockWidth + 1, blockLength + 1];

        Vector2 point0, point1;
        int q0, q1, r0, r1;
        bool has_top_left, has_top_right, has_bottom_left, has_bottom_right;

        for (int j = -1; j <= blockLength; j++)
        {
            for (int i = -1; i <= blockWidth; i++)
            {
                point0 = VertexPosition(new Vector2(blockOffsetX + i, blockOffsetY + j));
                point1 = VertexPosition(new Vector2(blockOffsetX + i + 1, blockOffsetY + j + 1));

                q0 = Mathf.RoundToInt(GetPixel(point0).x);
                q1 = Mathf.RoundToInt(GetPixel(point1).x);
                r0 = Mathf.RoundToInt(GetPixel(point0).y);
                r1 = Mathf.RoundToInt(GetPixel(point1).y);

                has_top_left = (i >= 0 && j >= 0);
                has_top_right = (i + 1 <= blockWidth && j >= 0);
                has_bottom_left = (i >= 0 && j + 1 <= blockLength);
                has_bottom_right = (i + 1 <= blockWidth && j + 1 <= blockLength);

                for (int q = q0; q < q1; q++)
                {
                    for (int r = r0; r < r1; r++)
                    {
                        Vector2 pos = PixelPosition(new Vector2(q, r));
                        float h = heightMapTexture.GetPixel(q, r).r;
                        Vector2 relPos;
                        Vector3 interpFactors;

                        if (has_top_left)
                        {
                            relPos = GetVertex(pos) - GetVertex(point0);
                            interpFactors = HermiteBoxSample(relPos);
                            fitData[i, j] += interpFactors * h;
                            fitWeights[i, j] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_top_right)
                        {
                            relPos = GetVertex(pos) - GetVertex(new Vector2(point1.x, point0.y));
                            interpFactors = HermiteBoxSample(relPos);
                            fitData[i + 1, j] += interpFactors * h;
                            fitWeights[i + 1, j] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_bottom_left)
                        {
                            relPos = GetVertex(pos) - GetVertex(new Vector2(point0.x, point1.y));
                            interpFactors = HermiteBoxSample(relPos);
                            fitData[i, j + 1] += interpFactors * h;
                            fitWeights[i, j + 1] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                        if (has_bottom_right)
                        {
                            relPos = GetVertex(pos) - GetVertex(point1);
                            interpFactors = HermiteBoxSample(relPos);
                            fitData[i + 1, j + 1] += interpFactors * h;
                            fitWeights[i + 1, j + 1] += Vector3.Scale(interpFactors, new Vector3(relPos.x, 1, relPos.y));
                        }
                    }
                }
                if (has_top_left)
                {
                    fitData[i, j].y = heightMapTexture.GetPixel(q0, r0).r;
                    fitWeights[i, j].y = 1;
                }
            }
        }
        for (int j = 0; j <= blockLength; j++)
        {
            for (int i = 0; i <= blockWidth; i++)
            {
                fitData[i, j] = fitData[i, j].DivideBy(fitWeights[i, j]);
            }
        }
        for (int j = 0; j <= blockLength; j++)
        {
            for (int i = 0; i <= blockWidth; i++)
            {
                blockData[i, j] = new Vector3(
                    i < blockWidth ? HermiteInflection(fitData[i, j], fitData[i + 1, j], true) : 0.5f,
                    fitData[i, j].y,
                    j < blockLength ? HermiteInflection(fitData[i, j], fitData[i, j + 1], false) : 0.5f
                    );
            }
        }
        return blockData;
    }

    static Vector3 HermiteBoxSample(Vector2 relPos)
    {
        if (Mathf.Abs(relPos.x) > 1f || Mathf.Abs(relPos.y) > 1f)
            return Vector3.zero;

        float temp = (Mathf.Abs(relPos.x) - 1) * (Mathf.Abs(relPos.y) - 1);

        return new Vector3(
            relPos.x * (2 * Mathf.Abs(relPos.y) + 1),
            (2 * Mathf.Abs(relPos.x) + 1) * (2 * Mathf.Abs(relPos.y) + 1),
            relPos.y * (2 * Mathf.Abs(relPos.x) + 1)
            ) * temp * temp;
    }

    static Vector3 HermiteBoxSample2(Vector2 relPos)
    {
        if (Mathf.Abs(relPos.x) > 1f || Mathf.Abs(relPos.y) > 1f)
            return Vector3.zero;

        float xh = Mathf.Abs(relPos.x) < 0.5f ?
            2 + relPos.x * relPos.x * (24 * Mathf.Abs(relPos.x) - 20) :
            4 + Mathf.Abs(relPos.x) * (Mathf.Abs(relPos.x) * (20 - 8 * Mathf.Abs(relPos.x)) - 16);
        float yh = Mathf.Abs(relPos.y) < 0.5f ?
            2 + relPos.y * relPos.y * (24 * Mathf.Abs(relPos.y) - 20) :
            4 + Mathf.Abs(relPos.y) * (Mathf.Abs(relPos.y) * (20 - 8 * Mathf.Abs(relPos.y)) - 16);

        return new Vector3(
            yh * relPos.x * (Mathf.Abs(relPos.x) * (15 * Mathf.Abs(relPos.x) - 30) + 15),
            xh * yh,
            xh * relPos.y * (Mathf.Abs(relPos.y) * (15 * Mathf.Abs(relPos.y) - 30) + 15)
            );
    }

    static float HermiteInflection(Vector3 v1, Vector3 v2, bool isInXDimension, float limit = 1f / 12)
    {
        int dim = isInXDimension ? 0 : 2;

        float inflectionPoint = 0.5f + (v1[dim] - v2[dim]) / (v1[dim] + v2[dim] + 2 * (v1.y - v2.y)) / 6;

        float inflectionSlope = v1[dim] - 2 * inflectionPoint * (2 * v1[dim] + v2[dim] + 3 * (v1.y - v2.y));
        inflectionSlope += inflectionPoint * inflectionPoint * 3 * (v1[dim] + v2[dim] + 2 * (v1.y - v2.y));

        if (!float.IsNaN(inflectionPoint) &&
            inflectionPoint > limit &&
            inflectionPoint < 1 - limit &&
            Mathf.Abs(inflectionSlope) > Mathf.Max(Mathf.Abs(v1[dim]), Mathf.Abs(v2[dim]))
            )
        {
            return inflectionPoint - 0.5f;
        }
        return (Mathf.Abs(v1[dim]) > Mathf.Abs(v2[dim])) ? limit - 0.5f : 0.5f - limit;
    }

    Vector2 PixelPosition(Vector2 index)
    {
        return (Vector2.one * 0.5f + index) / textureSize;
    }

    Vector2 GetPixel(Vector2 pos)
    {
        return (pos * textureSize);
    }

    Vector2 VertexPosition(Vector2 index)
    {
        return (index) * scale + offset;
    }

    Vector2 GetVertex(Vector2 pos)
    {
        return ((pos - offset) / scale);
    }
}