using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ImageToModel : MonoBehaviour
{
    public Sprite sprite;
    public bool generateBorder = true;
    public bool autoBorderDepth = true;
    public float borderDepth = 0.1f;
    [Range(0f, 1f)]
    public float threshold = 0.1f;

    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector2> uvs;

    int a;

    void Start()
    {
        GenerateModelFromTexture(sprite);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            GenerateModelFromTexture(sprite);
        }
    }

    public void GenerateModelFromTexture(Sprite sprite)
    {
        if (sprite.texture.isReadable)
        {
            Generate(sprite);
        }
        else
        {
            Texture2D originalTexture = sprite.texture;

            // Create a temporary RenderTexture with the same dimensions and format as the original texture
            RenderTexture tempRenderTexture = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);

            // Copy the contents of the original texture onto the temporary render texture
            Graphics.Blit(originalTexture, tempRenderTexture);

            // Create a new Texture2D and read the pixels from the temporary render texture into it
            Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
            readableTexture.ReadPixels(new Rect(0, 0, tempRenderTexture.width, tempRenderTexture.height), 0, 0);
            readableTexture.filterMode = sprite.texture.filterMode;
            readableTexture.Apply();

            // Release the temporary render texture
            RenderTexture.ReleaseTemporary(tempRenderTexture);


            Generate(Sprite.Create(readableTexture, sprite.rect, sprite.pivot, sprite.pixelsPerUnit));
        }
    }

    public void Generate(Sprite sprite)
    {
        Vector2 spriteStart = sprite.rect.position;
        Vector2 spriteSize = sprite.rect.size;
        if (autoBorderDepth)
        {
            borderDepth = 1f / Mathf.Max(spriteSize.x, spriteSize.y);
        }
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();

        a = 0;

        for (int x = 0; x < spriteSize.x; x++)
        {
            for (int y = 0; y < spriteSize.y; y++)
            {
                Color color = sprite.texture.GetPixel((int)spriteStart.x+x, (int)spriteStart.y + y);
                if (color.a > threshold)
                {
                    GenerateFaceSide(x, y, spriteSize, spriteStart);

                    if (generateBorder)
                    {
                        GenerateBackSide(x, y, spriteSize, spriteStart);
                        #region rightBorder
                        bool rightRange = InRange(sprite, x + 1, y);
                        if ((rightRange && sprite.texture.GetPixel((int)spriteStart.x + x + 1, (int)spriteStart.y + y).a <= threshold) || !rightRange)//right
                        {
                            GenerateRightBorder(x, y, spriteSize, spriteStart);
                        }
                        #endregion
                        #region leftBorder

                        bool leftRange = InRange(sprite, x - 1, y);
                        if ((leftRange && sprite.texture.GetPixel((int)spriteStart.x + x - 1, (int)spriteStart.y + y).a <= threshold) || !leftRange)//left
                        {
                            GenerateLeftBorder(x, y, spriteSize, spriteStart);
                        }
                        #endregion
                        #region topBorder

                        bool topRange = InRange(sprite, x, y + 1);
                        if ((topRange && sprite.texture.GetPixel((int)spriteStart.x + x, (int)spriteStart.y+y + 1).a <= threshold) || !topRange)//top
                        {
                            GenerateTopBorder(x, y, spriteSize, spriteStart);
                        }
                        #endregion
                        #region bottomBorder
                        bool bottomRange = InRange(sprite, x, y - 1);
                        if ((bottomRange && sprite.texture.GetPixel((int)spriteStart.x + x, (int)spriteStart.y + y - 1).a <= threshold) || !bottomRange)//bottom
                        {
                            GenerateBottomBorder(x, y, spriteSize, spriteStart);
                        }
                        #endregion
                    }
                }
            }
        }

        mesh = new Mesh();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.Optimize();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material.mainTexture = sprite.texture;
    }

    public void GenerateFaceSide(int x, int y, Vector2 spriteSize, Vector2 spriteStart) 
    {
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, 0));
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, 0));
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, 0));
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, 0));

        triangles.Add(a);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 3);
        triangles.Add(a + 1);

        AddUVs(x, y, spriteStart);

        a += 4;
    }

    public void GenerateBackSide(int x, int y, Vector2 spriteSize, Vector2 spriteStart) 
    {
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, borderDepth));
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, borderDepth));
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, borderDepth));
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, borderDepth));

        triangles.Add(a);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 3);

        AddUVs(x, y, spriteStart);

        a += 4;
    }

    public void GenerateRightBorder(int x, int y, Vector2 spriteSize, Vector2 spriteStart)
    {
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, 0)); //0 0
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, 0)); // 0 1
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, borderDepth)); // 0 1 d
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, borderDepth)); // 0 1 d

        triangles.Add(a);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 3);

        AddUVs(x, y, spriteStart);

        a += 4;
    }

    public void GenerateLeftBorder(int x, int y, Vector2 spriteSize, Vector2 spriteStart)
    {
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, 0)); //0 0
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, 0)); // 0 1
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, borderDepth)); // 0 1 d
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, borderDepth)); // 0 1 d

        triangles.Add(a);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 3);
        triangles.Add(a + 1);

        AddUVs(x, y, spriteStart);

        a += 4;
    }

    public void GenerateTopBorder(int x, int y, Vector2 spriteSize, Vector2 spriteStart)
    {
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, 0)); //0 0
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, 0)); // 0 1
        vertices.Add(new Vector3(x / spriteSize.x, (y + 1) / spriteSize.y, borderDepth)); // 0 1 d
        vertices.Add(new Vector3((x + 1) / spriteSize.x, (y + 1) / spriteSize.y, borderDepth)); // 0 1 d

        triangles.Add(a);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 3);
        triangles.Add(a + 1);

        AddUVs(x, y, spriteStart);

        a += 4;
    }

    public void GenerateBottomBorder(int x, int y, Vector2 spriteSize, Vector2 spriteStart)
    {
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, 0)); //0 0
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, 0)); // 0 1
        vertices.Add(new Vector3(x / spriteSize.x, y / spriteSize.y, borderDepth)); // 0 1 d
        vertices.Add(new Vector3((x + 1) / spriteSize.x, y / spriteSize.y, borderDepth)); // 0 1 d

        triangles.Add(a);
        triangles.Add(a + 1);
        triangles.Add(a + 2);
        triangles.Add(a + 2);
        triangles.Add(a + 1);
        triangles.Add(a + 3);

        AddUVs(x, y, spriteStart);
        a += 4;
    }

    public void AddUVs(int x, int y, Vector2 spriteStart)
    {
        uvs.Add(new Vector2((spriteStart.x + x) / (float)sprite.texture.width, (spriteStart.y + y) / (float)sprite.texture.height));
        uvs.Add(new Vector2((spriteStart.x + x + 1) / (float)sprite.texture.width, (spriteStart.y + y) / (float)sprite.texture.height));
        uvs.Add(new Vector2((spriteStart.x + x) / (float)sprite.texture.width, (spriteStart.y + y + 1) / (float)sprite.texture.height));
        uvs.Add(new Vector2((spriteStart.x + x + 1) / (float)sprite.texture.width, (spriteStart.y + y + 1) / (float)sprite.texture.height));
    }

    public bool InRange(Sprite sprite, int x, int y)
    {
        Vector2 posStart = sprite.rect.position;
        Vector2 spriteSize = sprite.rect.size;
        Vector2 size = posStart + spriteSize;
        return x < spriteSize.x && x >= 0 && y < spriteSize.y && y >= 0;
    }
}
