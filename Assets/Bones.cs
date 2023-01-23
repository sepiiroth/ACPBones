using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bones : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector3 barycenter;
    float t;
    public GameObject tqt;
    float[,] covarianceMatrix;

    void Start() {
        //Recupere les vertices
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

        // Calculer le barycentre
        barycenter = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++) {
            Vector3 vertex = transform.TransformPoint(mesh.vertices[i]); //Permet de recuperer les coordonnees de l'objet sinon barycentre au centre de la scene
            barycenter += vertex;
        }
        barycenter /= vertices.Length; 

        //Centre les données en 0, 0, 0
        this.transform.position += -barycenter;

        // Calculer la matrice de covariance
        covarianceMatrix = new float[3, 3];
        for (int i = 0; i < vertices.Length; i++) {
            covarianceMatrix[0, 0] += vertices[i].x * vertices[i].x;
            covarianceMatrix[1, 1] += vertices[i].y * vertices[i].y;
            covarianceMatrix[2, 2] += vertices[i].z * vertices[i].z;
            covarianceMatrix[0, 1] += vertices[i].x * vertices[i].y;
            covarianceMatrix[0, 2] += vertices[i].x * vertices[i].z;
            covarianceMatrix[1, 2] += vertices[i].y * vertices[i].z;
        }
        covarianceMatrix[1, 0] = covarianceMatrix[0, 1];
        covarianceMatrix[2, 0] = covarianceMatrix[0, 2];
        covarianceMatrix[2, 1] = covarianceMatrix[1, 2];
    }
}
