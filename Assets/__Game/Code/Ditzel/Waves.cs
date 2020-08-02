using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Waves : MonoBehaviour
{
    public int Dimension = 10;
    public float UVScale = 1f;
    public Octave[] Octaves;

    private MeshFilter _meshFilter;
    private Mesh _mesh;


    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        _mesh.name = gameObject.name;

        _mesh.vertices = GenerateVerts();
        _mesh.triangles = GenerateTries();  // verts need to be set for this to work
        _mesh.uv = GenerateUvs();
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.mesh = _mesh;
    }

    #region Mesh Generation

    private Vector3[] GenerateVerts()
    {
        var verts = new Vector3[(Dimension + 1) * (Dimension + 1)];

        // equally distributed verts
        for(int x = 0; x <= Dimension; x++)
            for(int z = 0; z <= Dimension; z++)
                verts[index(x,z)] = new Vector3(x, 0 , z);

        return verts;
    }

    private int[] GenerateTries()
    {
        var tries = new int[_mesh.vertices.Length * 6];

        // two triangles make one 'tile'
        for (int x = 0; x < Dimension; x++)
        {
            for(int z = 0; z < Dimension; z++)
            {
                tries[index(x,z) * 6 + 0] = index(x, z);
                tries[index(x,z) * 6 + 1] = index(x+1, z+1);
                tries[index(x,z) * 6 + 2] = index(x+1, z);
                tries[index(x,z) * 6 + 3] = index(x, z);
                tries[index(x,z) * 6 + 4] = index(x, z+1);
                tries[index(x,z) * 6 + 5] = index(x+1, z+1);
            }
        }

        return tries;
    }

    private Vector2[] GenerateUvs()
    {
        var uvs = new Vector2[_mesh.vertices.Length];

        // flip uv's from tile to tile to ensure texture lines up neatly
        for(int x = 0; x <= Dimension; x++) 
        {
            for(int z = 0; z <= Dimension; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                uvs[index(x,z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }  

        return uvs;
    }

    #endregion


    // Update is called once per frame
    void Update()
    {
        var verts = _mesh.vertices;
        for(int x = 0; x <= Dimension; x++)
        {
            for(int z = 0; z <= Dimension; z++)
            {
                var y = 0f;
                for(int o = 0; o < Octaves.Length; o++)
                {
                    var octave = Octaves[o];
                    if(octave.alternate)
                    {
                        var perl = Mathf.PerlinNoise(
                            (x*octave.scale.x)/Dimension, 
                            (z*octave.scale.y)/Dimension);
                        perl *= Mathf.PI * 2f; //normalise for cosine?

                        y += Mathf.Cos(perl + octave.speed.magnitude * Time.time) * octave.height;
                    }
                    else
                    {
                        var perl = Mathf.PerlinNoise(
                            (x*octave.scale.x + Time.time * octave.speed.x)/Dimension, 
                            (z*octave.scale.y + Time.time * octave.speed.y)/Dimension);
                        perl -= 0.5f;   // 'normalise' around 0 (ie move from 0->1 to -0.5->0.5)

                        y += perl * octave.height;
                    }
                }

                verts[index(x,z)] = new Vector3(x, y, z);
            }
        }

        _mesh.vertices = verts;
        _mesh.RecalculateNormals();
    }

    public float GetHeight(Vector3 position)
    {
        // scale factor and position in local space
        var scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        var localPos = Vector3.Scale((position - transform.position), scale);

        // edges (closest 'point')
        var p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
        var p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
        var p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
        var p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

        // clamp if position outside the plane
        p1.x = Mathf.Clamp(p1.x, 0, Dimension);
        p1.z = Mathf.Clamp(p1.z, 0, Dimension);
        p2.x = Mathf.Clamp(p2.x, 0, Dimension);
        p2.z = Mathf.Clamp(p2.z, 0, Dimension);
        p3.x = Mathf.Clamp(p3.x, 0, Dimension);
        p3.z = Mathf.Clamp(p3.z, 0, Dimension);
        p4.x = Mathf.Clamp(p4.x, 0, Dimension);
        p4.z = Mathf.Clamp(p4.z, 0, Dimension);

        // weight height calculation based on distance to each corner of tile
        // q: why epsilon only on piont 4? 
        var max = Mathf.Max(Vector3.Distance(p1, localPos), Vector3.Distance(p2, localPos), Vector3.Distance(p3, localPos), Vector3.Distance(p4, localPos) + Mathf.Epsilon); 
        var dist = (max - Vector3.Distance(p1, localPos))
            + (max - Vector3.Distance(p2, localPos))
            + (max - Vector3.Distance(p3, localPos))
            + (max - Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        var height = _mesh.vertices[index(p1.x, p1.z)].y * (max - Vector3.Distance(p1, localPos))
            + _mesh.vertices[index(p2.x, p2.z)].y * (max - Vector3.Distance(p2, localPos))
            + _mesh.vertices[index(p3.x, p3.z)].y * (max - Vector3.Distance(p3, localPos))
            + _mesh.vertices[index(p4.x, p4.z)].y * (max - Vector3.Distance(p4, localPos));

        // scale
        return height * transform.lossyScale.y / dist;
    }

    private int index(float x, float z)
    {
        return index((int)x, (int)z);
    }

    private int index(int x, int z)
    {
        return (x * (Dimension + 1)) + z;
    }

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
    

}
