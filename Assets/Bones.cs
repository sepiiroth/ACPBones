using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPart : MonoBehaviour {
    public Vector3[] extremPoints = new Vector3[2];
}

public class Bones : MonoBehaviour {
    public GameObject[] bodyParts;
    public GameObject man;

    Mesh mesh;
    Vector3[] vertices;

    Vector3 barycenter = Vector3.zero;

    float[,] covarianceMatrix = new float[3, 3];

    Vector3 v0 = new Vector3(1, 0, 0);
    Vector3 vk = new Vector3();
    float lambda = 0;
    float tolerance = 0.0001f;
    int k = 2000; //nombre d'iteration

    Vector3[] extremPoints = new Vector3[2];

    LineRenderer lineRenderer;
    GameObject cylinder;
    float radius = 0.01f;

    public GameObject point;

    private GameObject gObj;
    private AvatarBuilder avatarBuilder;

    List<Transform> bones = new List<Transform>();

    void Start() {
        gObj = new GameObject("Skeleton");
        Instantiate(gObj);

        for(int i = 0; i < bodyParts.Length; i++) {
            CreateSkeleton(bodyParts[i]);
        }
        
        for(int i = 1; i < bodyParts.Length; i++) {
            Raccord(bodyParts[i]);
        }

        Animator animator = man.GetComponent<Animator>();

        HumanBone[] bonesH = new HumanBone[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            HumanBone bone = new HumanBone();
            bone.humanName = bones[i].gameObject.name;
            bone.boneName = bones[i].gameObject.name;
            bone.limit.useDefaultValues = true;

            bonesH[i] = bone;
        }

        Avatar avatar = AvatarBuilder.BuildHumanAvatar(man, bonesH);
        animator.avatar = avatar;

        //SkinnedMeshRenderer skinnedMesh = man.AddComponent<SkinnedMeshRenderer>();
        //skinnedMesh.rootBone = gObj.transform;
        //skinnedMesh.sharedMesh = man.GetComponent<MeshFilter>().mesh;

        //Transorm[] bonesArray     = bones.ToArray(); 
        //skinnedMesh.bones = bonesArray;
    }

    void CreateSkeleton(GameObject part) {
        //==-- Partie 3: Calcule du barycentre, centre les donnees et calcule la matrice de covariance --==//
        mesh = part.GetComponent<MeshFilter>().mesh; //Recupere les vertices
        part.AddComponent<BodyPart>();
        vertices = mesh.vertices;

        barycenter = Vector3.zero;
        covarianceMatrix = new float[3, 3];
        v0 = new Vector3(1, 0, 0);
        vk = new Vector3();
        lambda = 0;
        extremPoints = new Vector3[2];

        //==-- Calcul du barycentre
        for (int i = 0; i < vertices.Length; i++) {
            Vector3 vertex = part.transform.TransformPoint(mesh.vertices[i]); //Permet d'utiliser les coordonnees dans le monde et non dans le mesh
            barycenter += vertex;
        }

        barycenter /= vertices.Length; 
        //Instantiate(point, barycenter, Quaternion.identity);

        //==-- Centre les donnees
        part.transform.position += -barycenter;

        //==-- Calculer la matrice de covariance
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = part.transform.TransformPoint(mesh.vertices[i]);
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
        
        /*for(int i = 0; i < 3; i++) {
            for(int j = 0; j < 3; j++) {
                Debug.Log(covarianceMatrix[i, j]);
            } 
        }*/
        
        //==-- Partie 4: Methode de la puissance pour valeur propre dominante et vecteur associe --==//
        for(int i = 0; i < k; i++) {
            vk = MatrixMultiplyVector(covarianceMatrix, v0); //Calculer M.Vk
            lambda = Mathf.Max(Mathf.Abs(vk.x), Mathf.Abs(vk.y), Mathf.Abs(vk.z)); // Appeler lambda k la composante de M * Vk dont la valeur absolue est la plus grande possible
            vk = (1/lambda) * vk; // Calculer vk+1
            v0 = vk;
            if(!(Mathf.Abs(lambda - vk.magnitude) > tolerance)) { // Pour k suffisamment grand, lambda = lambdak donc on verifie l'ecart avec une tolerance et on arrete quand on est proche
                break;
            } 
        }
        
        vk = vk.normalized; //V normaliser pour la projection

        //Debug.Log("Lambda: " + lambda);
        //Debug.Log("Vk: " + vk);

        //==-- Partie 5: Projeter les points centres, sauvegarder les points eloignes ==--//
        Vector3[] projection = new Vector3[vertices.Length];
        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] = part.transform.TransformPoint(mesh.vertices[i]);
            projection[i] = Vector3.Dot(vertices[i], v0)*v0;
            //Instantiate(point, projection[i], Quaternion.identity); 
        }

        //==-- Calcule des deux points extremes
        for(int i = 0; i < projection.Length; i++) {
            //Debug.Log(Vector3.Angle(vk, projection[i]));
            
            if(Mathf.Floor(Vector3.Angle(vk, projection[i])) == 0) { //positif
                if(Vector3.Distance(Vector3.zero, extremPoints[0]) < Vector3.Distance(Vector3.zero, projection[i])) {
                    extremPoints[0] = projection[i];
                }
            } else { //negatif
                if(Vector3.Distance(Vector3.zero, extremPoints[1]) < Vector3.Distance(Vector3.zero, projection[i])) {
                    extremPoints[1] = projection[i];
                }
            }
        }

        //==-- Partie 6: Repositionner chaque partie ==--//
        part.transform.position -= -barycenter;
        extremPoints[0] -= -barycenter;
        extremPoints[1] -= -barycenter;
        BodyPart bdyPart = part.GetComponent<BodyPart>();
        bdyPart.extremPoints[0] = extremPoints[0];
        bdyPart.extremPoints[1] = extremPoints[1];
        //Instantiate(point, extremPoints[0], Quaternion.identity); 
        //Instantiate(point, extremPoints[1], Quaternion.identity); 

        //==-- Creation de l'axe
        // Creer un cylindre pour representer l'os
        CreateBone(extremPoints[0], extremPoints[1]);
    }

    void Raccord(GameObject gO) {
        //==-- Partie 7: Raccord de chaque axe entre eux par un segment ==--//
        BodyPart buste = bodyParts[0].GetComponent<BodyPart>();
        BodyPart actualPart = gO.GetComponent<BodyPart>();
        if(Vector3.Distance(buste.extremPoints[0], actualPart.extremPoints[0]) < Vector3.Distance(buste.extremPoints[0], actualPart.extremPoints[1])) {
            if(Vector3.Distance(buste.extremPoints[0], actualPart.extremPoints[0]) < Vector3.Distance(buste.extremPoints[1], actualPart.extremPoints[0])) {
                CreateBone(buste.extremPoints[0], actualPart.extremPoints[0]);
            } else {
                CreateBone(buste.extremPoints[1], actualPart.extremPoints[0]);
            }
        } else {
            if(Vector3.Distance(buste.extremPoints[0], actualPart.extremPoints[1]) < Vector3.Distance(buste.extremPoints[1], actualPart.extremPoints[1])) {
                CreateBone(buste.extremPoints[0], actualPart.extremPoints[1]);
            } else {
                CreateBone(buste.extremPoints[1], actualPart.extremPoints[1]);
            }
        }
    }


    void CreateBone(Vector3 pointA, Vector3 pointB) {
        Vector3 direction = (pointB - pointA).normalized;
        float length = Vector3.Distance(pointA, pointB);
        cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.rotation = Quaternion.LookRotation(direction);
        cylinder.transform.localRotation *= Quaternion.Euler(-90, 0, 0);
        cylinder.transform.position = pointA + direction * length / 2f;
        cylinder.transform.localScale = new Vector3(radius, length / 2f, radius);
        cylinder.transform.SetParent(gObj.transform);
    }

    Vector3 MatrixMultiplyVector(float[,] covMatrix, Vector3 v) {
        Vector3 res = new Vector3();
        for(int i = 0; i < 3; i++) {
            for(int j = 0; j < 3; j++) {
                res[i] += covMatrix[i, j] * v[j];
            }
        }
        return res;
    }
}
