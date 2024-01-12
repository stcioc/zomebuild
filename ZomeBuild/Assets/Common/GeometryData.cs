using UnityEngine;
using System.Collections.Generic;

public class GeometryData {

	//TAU value - golden ratio
	public static float TAU;

	//lengths of each strut - used for creating struts objects
	public static float[] strutLen;
	//normals of the plane of each face. Is a vector that points to the center of each face
	public static Vector3[] outPoints;
	//up vectors for each face: a vector that points from the center of the face to the middle of a side
	//used for rotating correctly the struts in place
	public static Vector3[] upVectors;
	//ball diameter (distance between faces) (square to square, triangle to triangle, penta to penta) 
	public static float[] ballDiam = { 2f, 2.004638628f, 2.026221313f };
	//shared mesh
	public static Mesh mesh;

	//scaling factor for vertex coordinates - to get a ball with square-to-square radius of 1
	private static float SCALEFACTOR;

	//ball vertex coordinates expressed as pair like a+b*TAU
	//source: http://en.wikipedia.org/wiki/Rhombicosidodecahedron#Cartesian_coordinates
	private static readonly sbyte[] vertexes = {
		-1, -2, -1, -1, -4, -6,
		-1, -2, -1, -1, 4, 6,
		-1, -2, 1, 1, -4, -6,
		-1, -2, 1, 1, 4, 6,
		1, 2, -1, -1, -4, -6,
		1, 2, -1, -1, 4, 6,
		1, 2, 1, 1, -4, -6,
		1, 2, 1, 1, 4, 6,
		-4, -6, -1, -2, -1, -1,
		-4, -6, -1, -2, 1, 1,
		-4, -6, 1, 2, -1, -1,
		-4, -6, 1, 2, 1, 1,
		4, 6, -1, -2, -1, -1,
		4, 6, -1, -2, 1, 1,
		4, 6, 1, 2, -1, -1,
		4, 6, 1, 2, 1, 1,
		-1, -1, -4, -6, -1, -2,
		-1, -1, -4, -6, 1, 2,
		-1, -1, 4, 6, -1, -2,
		-1, -1, 4, 6, 1, 2,
		1, 1, -4, -6, -1, -2,
		1, 1, -4, -6, 1, 2,
		1, 1, 4, 6, -1, -2,
		1, 1, 4, 6, 1, 2,
		-2, -4, -1, -2, -3, -5,
		-2, -4, -1, -2, 3, 5,
		-2, -4, 1, 2, -3, -5,
		-2, -4, 1, 2, 3, 5,
		2, 4, -1, -2, -3, -5,
		2, 4, -1, -2, 3, 5,
		2, 4, 1, 2, -3, -5,
		2, 4, 1, 2, 3, 5,
		-3, -5, -2, -4, -1, -2,
		-3, -5, -2, -4, 1, 2,
		-3, -5, 2, 4, -1, -2,
		-3, -5, 2, 4, 1, 2,
		3, 5, -2, -4, -1, -2,
		3, 5, -2, -4, 1, 2,
		3, 5, 2, 4, -1, -2,
		3, 5, 2, 4, 1, 2,
		-1, -2, -3, -5, -2, -4,
		-1, -2, -3, -5, 2, 4,
		-1, -2, 3, 5, -2, -4,
		-1, -2, 3, 5, 2, 4,
		1, 2, -3, -5, -2, -4,
		1, 2, -3, -5, 2, 4,
		1, 2, 3, 5, -2, -4,
		1, 2, 3, 5, 2, 4,
		-3, -5, 0, 0, -3, -4,
		-3, -5, 0, 0, 3, 4,
		3, 5, 0, 0, -3, -4,
		3, 5, 0, 0, 3, 4,
		-3, -4, -3, -5, 0, 0,
		-3, -4, 3, 5, 0, 0,
		3, 4, -3, -5, 0, 0,
		3, 4, 3, 5, 0, 0,
		0, 0, -3, -4, -3, -5,
		0, 0, -3, -4, 3, 5,
		0, 0, 3, 4, -3, -5,
		0, 0, 3, 4, 3, 5
	};

	//square faces; vertexes in trigonometric order
	//opposite faces should come in pairs
	private static readonly byte[] square_faces = {
		1, 5, 7, 3,
		4, 0, 2, 6,
		
		15, 13, 12, 14,
		10, 8, 9, 11,
		
		18, 19, 23, 22,
		17, 16, 20, 21,
		
		59, 7, 31, 47,
		0, 56, 40, 24,
		
		51, 15, 39, 31,
		24, 32, 8, 48,
		
		14, 50, 30, 38,
		49, 9, 33, 25,
		
		46, 30, 6, 58,
		25, 41, 57, 1,
		
		58, 2, 26, 42,
		5, 57, 45, 29,
		
		48, 10, 34, 26,
		29, 37, 13, 51,
		
		11, 49, 27, 35,
		50, 12, 36, 28,
		
		43, 27, 3, 59,
		28, 44, 56, 4,
		
		19, 53, 35, 43,
		54, 20, 44, 36,
		
		42, 34, 53, 18,
		37, 45, 21, 54, 
		
		22, 55, 38, 46,
		52, 17, 41, 33,
		
		47, 39, 55, 23,
		32, 40, 16, 52
	};

	//triangle faces; vertexes in trigonometric order
	//opposite faces should come in pairs
	private static readonly byte[] triangle_faces = {
		51, 13, 15,
		48, 8, 10,
		
		12, 50, 14,
		9, 49, 11,
		
		3, 7, 59,
		4, 56, 0,
		
		1, 57, 5,
		6, 2, 58,
		
		31, 39, 47,
		40, 32, 24,
		
		30, 46, 38,
		33, 41, 25,
		
		26, 34, 42,
		45, 37, 29,
		
		35, 27, 43,
		36, 44, 28,
		
		18, 53, 19,
		21, 20, 54,
		
		23, 55, 22,
		16, 17, 52
	};

	//pentagonal faces; vertexes in trigonometric order
	//opposite faces should come in pairs
	private static readonly byte[] pentagon_faces = {
		5, 29, 51, 31, 7,
		0, 24, 48, 26, 2,
		
		50, 28, 4, 6, 30,
		49, 25, 1, 3, 27,
		
		19, 43, 59, 47, 23,
		44, 20, 16, 40, 56,
		
		41, 17, 21, 45, 57,
		46, 58, 42, 18, 22,
		
		15, 14, 38, 55, 39,
		32, 52, 33, 9, 8,
		
		10, 11, 35, 53, 34,
		37, 54, 36, 12, 13
	};

	//cordinates of balls as a+b*TAU
	//for each face: coordinates of ball of lenght 0, then of length 1, then of length 2
	public static sbyte[] coords = {
		0, 0, 0, 0, 2, 2, 0, 0, 0, 0, 2, 4, 0, 0, 0, 0, 4, 6,
		0, 0, 0, 0, -2, -2, 0, 0, 0, 0, -2, -4, 0, 0, 0, 0, -4, -6,
		2, 2, 0, 0, 0, 0, 2, 4, 0, 0, 0, 0, 4, 6, 0, 0, 0, 0,
		-2, -2, 0, 0, 0, 0, -2, -4, 0, 0, 0, 0, -4, -6, 0, 0, 0, 0,
		0, 0, 2, 2, 0, 0, 0, 0, 2, 4, 0, 0, 0, 0, 4, 6, 0, 0,
		0, 0, -2, -2, 0, 0, 0, 0, -2, -4, 0, 0, 0, 0, -4, -6, 0, 0,
		0, 1, 1, 1, 1, 2, 1, 1, 1, 2, 2, 3, 1, 2, 2, 3, 3, 5,
		0, -1, -1, -1, -1, -2, -1, -1, -1, -2, -2, -3, -1, -2, -2, -3, -3, -5,
		1, 2, 0, 1, 1, 1, 2, 3, 1, 1, 1, 2, 3, 5, 1, 2, 2, 3,
		-1, -2, 0, -1, -1, -1, -2, -3, -1, -1, -1, -2, -3, -5, -1, -2, -2, -3,
		1, 2, 0, 1, -1, -1, 2, 3, 1, 1, -1, -2, 3, 5, 1, 2, -2, -3,
		-1, -2, 0, -1, 1, 1, -2, -3, -1, -1, 1, 2, -3, -5, -1, -2, 2, 3,
		0, 1, 1, 1, -1, -2, 1, 1, 1, 2, -2, -3, 1, 2, 2, 3, -3, -5,
		0, -1, -1, -1, 1, 2, -1, -1, -1, -2, 2, 3, -1, -2, -2, -3, 3, 5,
		0, -1, 1, 1, -1, -2, -1, -1, 1, 2, -2, -3, -1, -2, 2, 3, -3, -5,
		0, 1, -1, -1, 1, 2, 1, 1, -1, -2, 2, 3, 1, 2, -2, -3, 3, 5,
		-1, -2, 0, 1, -1, -1, -2, -3, 1, 1, -1, -2, -3, -5, 1, 2, -2, -3,
		1, 2, 0, -1, 1, 1, 2, 3, -1, -1, 1, 2, 3, 5, -1, -2, 2, 3,
		-1, -2, 0, 1, 1, 1, -2, -3, 1, 1, 1, 2, -3, -5, 1, 2, 2, 3,
		1, 2, 0, -1, -1, -1, 2, 3, -1, -1, -1, -2, 3, 5, -1, -2, -2, -3,
		0, -1, 1, 1, 1, 2, -1, -1, 1, 2, 2, 3, -1, -2, 2, 3, 3, 5,
		0, 1, -1, -1, -1, -2, 1, 1, -1, -2, -2, -3, 1, 2, -2, -3, -3, -5,
		-1, -1, 1, 2, 0, 1, -1, -2, 2, 3, 1, 1, -2, -3, 3, 5, 1, 2,
		1, 1, -1, -2, 0, -1, 1, 2, -2, -3, -1, -1, 2, 3, -3, -5, -1, -2,
		-1, -1, 1, 2, 0, -1, -1, -2, 2, 3, -1, -1, -2, -3, 3, 5, -1, -2,
		1, 1, -1, -2, 0, 1, 1, 2, -2, -3, 1, 1, 2, 3, -3, -5, 1, 2,
		1, 1, 1, 2, 0, -1, 1, 2, 2, 3, -1, -1, 2, 3, 3, 5, -1, -2,
		-1, -1, -1, -2, 0, 1, -1, -2, -2, -3, 1, 1, -2, -3, -3, -5, 1, 2,
		1, 1, 1, 2, 0, 1, 1, 2, 2, 3, 1, 1, 2, 3, 3, 5, 1, 2,
		-1, -1, -1, -2, 0, -1, -1, -2, -2, -3, -1, -1, -2, -3, -3, -5, -1, -2,
		1, 2, 0, 0, 0, 1, 2, 3, 0, 0, 1, 1, 3, 5, 0, 0, 1, 2,
		-1, -2, 0, 0, 0, -1, -2, -3, 0, 0, -1, -1, -3, -5, 0, 0, -1, -2,
		1, 2, 0, 0, 0, -1, 2, 3, 0, 0, -1, -1, 3, 5, 0, 0, -1, -2,
		-1, -2, 0, 0, 0, 1, -2, -3, 0, 0, 1, 1, -3, -5, 0, 0, 1, 2,
		0, 0, 0, 1, 1, 2, 0, 0, 1, 1, 2, 3, 0, 0, 1, 2, 3, 5,
		0, 0, 0, -1, -1, -2, 0, 0, -1, -1, -2, -3, 0, 0, -1, -2, -3, -5,
		0, 0, 0, -1, 1, 2, 0, 0, -1, -1, 2, 3, 0, 0, -1, -2, 3, 5,
		0, 0, 0, 1, -1, -2, 0, 0, 1, 1, -2, -3, 0, 0, 1, 2, -3, -5,
		1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 2, 2, 3, 2, 3, 2, 3,
		-1, -1, -1, -1, -1, -1, -1, -2, -1, -2, -1, -2, -2, -3, -2, -3, -2, -3,
		1, 1, 1, 1, -1, -1, 1, 2, 1, 2, -1, -2, 2, 3, 2, 3, -2, -3,
		-1, -1, -1, -1, 1, 1, -1, -2, -1, -2, 1, 2, -2, -3, -2, -3, 2, 3,
		-1, -1, 1, 1, -1, -1, -1, -2, 1, 2, -1, -2, -2, -3, 2, 3, -2, -3,
		1, 1, -1, -1, 1, 1, 1, 2, -1, -2, 1, 2, 2, 3, -2, -3, 2, 3,
		-1, -1, 1, 1, 1, 1, -1, -2, 1, 2, 1, 2, -2, -3, 2, 3, 2, 3,
		1, 1, -1, -1, -1, -1, 1, 2, -1, -2, -1, -2, 2, 3, -2, -3, -2, -3,
		0, -1, 1, 2, 0, 0, -1, -1, 2, 3, 0, 0, -1, -2, 3, 5, 0, 0,
		0, 1, -1, -2, 0, 0, 1, 1, -2, -3, 0, 0, 1, 2, -3, -5, 0, 0,
		0, 1, 1, 2, 0, 0, 1, 1, 2, 3, 0, 0, 1, 2, 3, 5, 0, 0,
		0, -1, -1, -2, 0, 0, -1, -1, -2, -3, 0, 0, -1, -2, -3, -5, 0, 0,
		1, 1, 0, 0, 1, 2, 1, 2, 0, 0, 2, 3, 2, 3, 0, 0, 3, 5,
		-1, -1, 0, 0, -1, -2, -1, -2, 0, 0, -2, -3, -2, -3, 0, 0, -3, -5,
		1, 1, 0, 0, -1, -2, 1, 2, 0, 0, -2, -3, 2, 3, 0, 0, -3, -5,
		-1, -1, 0, 0, 1, 2, -1, -2, 0, 0, 2, 3, -2, -3, 0, 0, 3, 5,
		0, 0, 1, 2, 1, 1, 0, 0, 2, 3, 1, 2, 0, 0, 3, 5, 2, 3,
		0, 0, -1, -2, -1, -1, 0, 0, -2, -3, -1, -2, 0, 0, -3, -5, -2, -3,
		0, 0, -1, -2, 1, 1, 0, 0, -2, -3, 1, 2, 0, 0, -3, -5, 2, 3,
		0, 0, 1, 2, -1, -1, 0, 0, 2, 3, -1, -2, 0, 0, 3, 5, -2, -3,
		1, 2, 1, 1, 0, 0, 2, 3, 1, 2, 0, 0, 3, 5, 2, 3, 0, 0,
		-1, -2, -1, -1, 0, 0, -2, -3, -1, -2, 0, 0, -3, -5, -2, -3, 0, 0,
		-1, -2, 1, 1, 0, 0, -2, -3, 1, 2, 0, 0, -3, -5, 2, 3, 0, 0,
		1, 2, -1, -1, 0, 0, 2, 3, -1, -2, 0, 0, 3, 5, -2, -3, 0, 0
	};

	public static void Init() {

		//calculate constants
		TAU = (1 + Mathf.Sqrt(5)) / 2;
		//SCALEFACTOR = 2*TAU + 1;
		SCALEFACTOR = 4 + 6*TAU;

		//init vectors
		outPoints = new Vector3[62];
		upVectors = new Vector3[62];

		//calculate strut lengths
		float B0 = 2f + 2f * TAU;
		strutLen = new float[9];
		strutLen[0] = B0;
		strutLen[1] = B0*TAU;
		strutLen[2] = B0*(TAU+1);
		strutLen[3] = Mathf.Sqrt(3)/2*B0;
		strutLen[4] = Mathf.Sqrt(3)/2*B0*TAU;
		strutLen[5] = Mathf.Sqrt(3)/2*B0*(TAU+1);
		strutLen[6] = Mathf.Sqrt(2+TAU)/2*B0;
		strutLen[7] = Mathf.Sqrt(2+TAU)/2*B0*TAU;
		strutLen[8] = Mathf.Sqrt(2+TAU)/2*B0*(TAU+1);

		for (int i=0; i<30; i++)
			setNormal(i, square_faces[i*4], square_faces[i*4+1], square_faces[i*4+2]);

		for (int i=0; i<20; i++)
			setNormal(30+i, triangle_faces[i*3], triangle_faces[i*3+1], triangle_faces[i*3+2]);

		for (int i=0; i<12; i++)
			setNormal(50+i, pentagon_faces[i*5], pentagon_faces[i*5+1], pentagon_faces[i*5+2]);

		//build the mesh
		mesh = new Mesh();
		float[] uvCoords = {
			//0.5f, 0.191f,
			0.5f, 0,
			0.5f, 0.5f,
			0, 0.5f,
			//0, 0.191f,
			0, 0, 
			
			1f, 0.5f,
			0.75f, 0.933f,
			0.5f, 0.5f,
			
			0.4045f, 0.5f,
			0.5f, 0.794f,
			0.25f, 0.9755f,
			0, 0.794f,
			0.0965f, 0.5f
		};
		
		//build shared mesh and calculate center points
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		
		for (int i=0; i<30; i++) {
			addTriangle(vertices, triangles, square_faces[i*4], square_faces[i*4+1], square_faces[i*4+2]);
			addUV(uvs, uvCoords, 0, 1, 2);
			addTriangle(vertices, triangles, square_faces[i*4+2], square_faces[i*4+3], square_faces[i*4]);
			addUV(uvs, uvCoords, 1, 2, 3);
		}
		
		for (int i=0; i<20; i++) {
			addTriangle(vertices, triangles, triangle_faces[i*3], triangle_faces[i*3+1], triangle_faces[i*3+2]);
			addUV(uvs, uvCoords, 4, 5, 6);
		}
		
		for (int i=0; i<12; i++) {
			addTriangle(vertices, triangles, pentagon_faces[i*5], pentagon_faces[i*5+1], pentagon_faces[i*5+2]);
			addUV(uvs, uvCoords, 7, 8, 9);
			addTriangle(vertices, triangles, pentagon_faces[i*5+2], pentagon_faces[i*5+3], pentagon_faces[i*5]);
			addUV(uvs, uvCoords, 9, 10, 7);
			addTriangle(vertices, triangles, pentagon_faces[i*5+3], pentagon_faces[i*5+4], pentagon_faces[i*5]);
			addUV(uvs, uvCoords, 10, 11, 7);
		}
		
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();

	}

	private static Vector3 getMeshPoint(int point) {
		return new Vector3(
			(vertexes[point*6+0] + vertexes[point*6+1]*TAU) / SCALEFACTOR,
			(vertexes[point*6+2] + vertexes[point*6+3]*TAU) / SCALEFACTOR,
			(vertexes[point*6+4] + vertexes[point*6+5]*TAU) / SCALEFACTOR
			);
	}

	private static void addTriangle( List<Vector3> v, List<int> t, byte point1, byte point2, byte point3) {
		v.Add(getMeshPoint(point1));
		t.Add (v.Count-1);
		v.Add(getMeshPoint(point2));
		t.Add (v.Count-1);
		v.Add(getMeshPoint(point3));
		t.Add (v.Count-1);
	}
	
	private static void addUV( List<Vector2> uv, float[] uvCoords, int point1, int point2, int point3) {
		Vector2 vector = new Vector2();
		vector.Set (uvCoords[point1*2], uvCoords[point1*2 + 1]);
		uv.Add(vector);
		vector.Set (uvCoords[point2*2], uvCoords[point2*2 + 1]);
		uv.Add(vector);
		vector.Set (uvCoords[point3*2], uvCoords[point3*2 + 1]);
		uv.Add(vector);
	}

	private static void setNormal(int index, byte point1, byte point2, byte point3) {
		Vector3 v1, v2, v3, midPoint;

		v1 = getMeshPoint(point1);
		v2 = getMeshPoint(point2);
		v3 = getMeshPoint(point3);

		Plane plane = new Plane(v1, v2, v3);
		outPoints[index] = plane.normal;
		//Debug.Log("Index, " + index + "," + plane.normal.x + "," + plane.normal.y + "," + plane.normal.z);
		
		midPoint = v1 + (v2 - v1) /2;
		upVectors[index] = plane.normal * plane.distance - midPoint;
		upVectors[index].Normalize();
	}

	//for a ball with position phi p relative to current ball
	//find the face where is pointing (position) and the length of the strut (type)
	//this is used in model loading
	public static void getPositionType( PositionPhi p, out int position, out int type) {
		type = -1;
		position = -1;
		sbyte[] posArray = { (sbyte)p.x0, (sbyte)p.x1, (sbyte)p.y0, (sbyte)p.y1, (sbyte)p.z0, (sbyte)p.z1 };

		for (int i=0; i < 62; i++) {
			for (int j = 0; j < 3; j++) {
				bool found = true;
				for (int k=0; k<6; k++) {
					if (coords[i*18 + j*6 + k] != posArray[k]) {
						found = false;
						break;
					}
				}
				if (found) {
					position = i;
					if (i < 30 ) {
						type = j;
						return;
					}
					else {
						if (i<50) {
							type = 3 + j;
							return;
						}
						else {
							type = 6 + j;
							return;
						}
					}
				}
			}
		}
	}
}
