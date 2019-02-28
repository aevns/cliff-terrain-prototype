using UnityEngine;

public class FastImageSampler : TerrainSampler
{
    public Texture2D heightMapTexture;
    [SerializeField] [Range(-1.0f, 1.0f)] private float diagonalFactor = 0.25f;
    [SerializeField] [Range(0.0f, 0.5f)] private float offsetLimit = 1f/16;

    public override Vector3[,] Sample(int blockOffsetX, int blockOffsetY, int blockWidth, int blockLength)
    {
        Vector3[,] fitData = new Vector3[blockWidth + 1, blockLength + 1];
        Vector3[,] blockData = new Vector3[blockWidth + 1, blockLength + 1];
        float dhdx, dhdz, diag1, diag2;

        for (int j = 0; j <= blockLength; j++)
        {
            for (int i = 0; i <= blockWidth; i++)
            {
                int x = blockOffsetX * 2 + 2 * i, y = blockOffsetY * 2 + 2 * j;

                dhdx = (heightMapTexture.GetPixel(x + 1, y).r - heightMapTexture.GetPixel(x - 1, y).r);
                dhdz = (heightMapTexture.GetPixel(x, y + 1).r - heightMapTexture.GetPixel(x, y - 1).r);

                diag1 = (heightMapTexture.GetPixel(x + 1, y + 1).r - heightMapTexture.GetPixel(x - 1, y - 1).r);
                diag2 = (heightMapTexture.GetPixel(x + 1, y - 1).r - heightMapTexture.GetPixel(x - 1, y + 1).r);

                dhdx = dhdx * (1f - diagonalFactor) + (diag1 + diag2) * 0.5f * diagonalFactor;
                dhdz = dhdz * (1f - diagonalFactor) + (diag1 - diag2) * 0.5f * diagonalFactor;

                fitData[i, j] = new Vector3(dhdx, heightMapTexture.GetPixel(x, y).r, dhdz);
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

    float HermiteInflection(Vector3 v1, Vector3 v2, bool isInXDimension)
    {
        int dim = isInXDimension ? 0 : 2;

        float inflectionPoint = 0.5f + (v1[dim] - v2[dim]) / (v1[dim] + v2[dim] + 2 * (v1.y - v2.y)) / 6;

        float inflectionSlope = v1[dim] - 2 * inflectionPoint * (2 * v1[dim] + v2[dim] + 3 * (v1.y - v2.y));
        inflectionSlope += inflectionPoint * inflectionPoint * 3 * (v1[dim] + v2[dim] + 2 * (v1.y - v2.y));

        if (!float.IsNaN(inflectionPoint) &&
            inflectionPoint > offsetLimit &&
            inflectionPoint < 1 - offsetLimit &&
            Mathf.Abs(inflectionSlope) > Mathf.Max(Mathf.Abs(v1[dim]), Mathf.Abs(v2[dim]))
            )
        {
            return inflectionPoint - 0.5f;
        }
        return (Mathf.Abs(v1[dim]) > Mathf.Abs(v2[dim])) ? offsetLimit - 0.5f : 0.5f - offsetLimit;
    }
}