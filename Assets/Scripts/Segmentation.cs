using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Segmentation : MonoBehaviour {
    public GameObject body;
    public float epsilon = 0.1f;
  public int k = 5;
  public int maxIterations = 20;

  private Vector3[] vertices;
  private int[] headIndices;

  void Start () {
    Mesh mesh = body.GetComponent<MeshFilter>().mesh;
    vertices = mesh.vertices;

    headIndices = KMeansClustering(vertices, k, maxIterations, epsilon);

    GameObject head = new GameObject("Head");
    Mesh headMesh = CreateHeadMesh(vertices, headIndices);
    head.AddComponent<MeshFilter>().mesh = headMesh;
    head.AddComponent<MeshRenderer>();
    Instantiate(head, Vector3.zero, Quaternion.identity);
  }

  int[] KMeansClustering(Vector3[] points, int k, int maxIterations, float epsilon) {
    Vector3[] centroids = new Vector3[k];
    for (int i = 0; i < k; i++) {
      centroids[i] = points[Random.Range(0, points.Length)];
    }

    int[] clusterIndices = new int[points.Length];
    for (int iteration = 0; iteration < maxIterations; iteration++) {
      for (int i = 0; i < points.Length; i++) {
        float minDistance = float.MaxValue;
        int closestCentroid = 0;
        for (int j = 0; j < k; j++) {
          float distance = Vector3.Distance(points[i], centroids[j]);
          if (distance < minDistance) {
            minDistance = distance;
            closestCentroid = j;
          }
        }
        clusterIndices[i] = closestCentroid;
      }

      Vector3[] newCentroids = new Vector3[k];
      int[] clusterSizes = new int[k];
      for (int i = 0; i < points.Length; i++) {
        int clusterIndex = clusterIndices[i];
        newCentroids[clusterIndex] += points[i];
        clusterSizes[clusterIndex]++;
      }
      for (int i = 0; i < k; i++) {
        newCentroids[i] /= clusterSizes[i];
      }

      bool converged = true;
      for (int i = 0; i < k; i++) {
        if (Vector3.Distance(centroids[i], newCentroids[i]) > epsilon) {
          converged = false;
          break;
        }
      }
      if (converged) {
        break;
      }
      centroids = newCentroids;
    }

    int[] headClusterIndices = new int[points.Length];
    int headClusterIndex = 0;
    float maxAverageY = float.MinValue;
    for (int i = 0; i < k; i++) {
      Vector3 clusterCentroid = centroids[i];
      int clusterSize = 0;
      float clusterSumY = 0f;
      for (int j = 0; j < points.Length; j++) {
        if (clusterIndices[j] == i) {
          clusterSize++;
          clusterSumY += points[j].y;
        }
      }
      float clusterAverageY = clusterSumY / clusterSize;
      if (clusterAverageY > maxAverageY) {
        maxAverageY = clusterAverageY;
        headClusterIndex = i;
      }
    }
    int headIndex = 0;
    for (int i = 0; i < points.Length; i++) {
      if (clusterIndices[i] == headClusterIndex) {
        headClusterIndices[headIndex] = i;
        headIndex++;
      }
    }
    System.Array.Resize(ref headClusterIndices, headIndex);
    return headClusterIndices;
  }

    private Mesh CreateHeadMesh(Vector3[] vertices, int[] headIndices) {
        Vector3[] headVertices = new Vector3[headIndices.Length];
        for (int i = 0; i < headIndices.Length; i++) {
            headVertices[i] = vertices[headIndices[i]];
        }

        Mesh headMesh = new Mesh();
        headMesh.vertices = headVertices;

        List<int> triangles = new List<int>();
        for (int i = 0; i < headIndices.Length - 2; i++) {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        headMesh.triangles = triangles.ToArray();
        headMesh.RecalculateNormals();
        return headMesh;
    }
}

