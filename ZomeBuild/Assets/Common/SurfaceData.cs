using UnityEngine;
using System.Collections;

public class SurfaceData {

	//type of surface = number of nodes, or INVALID
	public int type;
	//nodes
	public GameObject[] nodes;
	//surface object
	public GameObject surfaceObj;

	public const int INVALID_STRUCT = -1;
	public const int UNKNOWN_STRUCT = 0;
	private const int MAX_SIZES = 5;

	public SurfaceData() {
		type = UNKNOWN_STRUCT;
		nodes = new GameObject[MAX_SIZES];
		surfaceObj = null;
		for (int i=0; i< MAX_SIZES; i++) {
			nodes[i] = null;
		}
	}

	//copy surface data from another
	public void copy(SurfaceData surf) {
		type = surf.type;
		surfaceObj = surf.surfaceObj;
		for (int i=0; i< MAX_SIZES; i++)
			nodes[i] = surf.nodes[i];
	}

	//start filling the surface with two nodes
	public void initSurface(GameObject a, GameObject b) {
		nodes[0] = a;
		nodes[1] = b;
	}

	//replace the last node with a given one
	public void replaceLast(GameObject node) {
		nodes[getSize()] = node;
	}

	private int getSize() {
		for (int i=2; i<MAX_SIZES; i++) {
			if (nodes[i] == null )
				return i-1;
		}
		
		//if we got gere means that all nodels are filled, no last null -> last is the 5th
		return MAX_SIZES-1;
	}

	public bool contains(GameObject obj) {
		for (int i=0; i<MAX_SIZES; i++) {
			if (nodes[i] == null ) return false;
			if (nodes[i] == obj ) return true;
		}
		return false;
	}

	public bool containsBoth(GameObject a, GameObject b) {
		bool containsA = false, containsB = false;

		if (type < 4)
			return false;

		for (int i=0; i<type; i++) {
			if (nodes[i] == a ) containsA = true;
			if (nodes[i] == b ) containsB = true;
		}
		return (containsA && containsB);
	}

	public bool includeSurface(SurfaceData small) {
		//check that a higher-order surface (4, 5)
		//include a lower-order surface (3, 4, 5)
		//only 3 combinations possible: 4-3, 5-3, 5-4
		//the first two nodes will always be identical
		switch (type * small.type) {
		case 12:
			return (small.nodes[2] == nodes[2]) || (small.nodes[2] == nodes[3]);
		case 15:
			return (small.nodes[2] == nodes[2]) || (small.nodes[2] == nodes[4]);
		case 20:
			return
				(small.nodes[2]==nodes[2] && small.nodes[3]==nodes[3]) || 
				(small.nodes[2]==nodes[2] && small.nodes[3]==nodes[4]) || 
				(small.nodes[2]==nodes[3] && small.nodes[3]==nodes[4]);
		}

		//this should never happen
		Debug.Log ("Include Surface comparison - compare " + type + " with " + small.type + " - we should never get here!"); 
		return false;
	}

	public void delete() {
		if (surfaceObj == null)
			return;
		Object.Destroy(surfaceObj);
	}

	public void Dump() {
		string descriptor = "Surface: " + type.ToString();
		for (int i=0; i< MAX_SIZES; i++) {
			if (nodes[i] != null) {
				descriptor += " " + nodes[i].name + " " + nodes[i].transform.position.ToString();
			}
		}
		Debug.Log (descriptor);
	}

	//try finding another strut with type linkType
	//hanging from the last ball of the surface
	//if no one is fond or invalid, structure is invalidated
	//if one is found, is appended
	public void tryAddNext(int linkType, out GameObject[] otherCandidates) {
		BallData lastBall = null;
		int sides = 1;
		ArrayList candidates = new ArrayList();
		Vector3 vect1, vect2, cross1, cross2;
		float dot1, dot2;
		Plane structPlane;

		sides = getSize();
		lastBall = nodes[sides].GetComponent<BallData>();
		otherCandidates = null;

		//loop through the connections of last ball
		for (int i=0; i< lastBall.connections.Count; i++) {
//			if (lastBall.connections[i].ball != nodes[sides-1] && lastBall.connections[i].linkType == linkType) {
			if (lastBall.connections[i].ball != nodes[sides-1]) {
				GameObject newNode = lastBall.connections[i].ball;
//				Debug.Log ("Trying " + newNode.name);

				//is it the first node? great, closed surface - finished
				if (newNode == nodes[0]) {

					//if closed triangle - closed surface, finish
					if (sides == 2) {
						type = sides + 1;
						return;
					}

					//check colinearity
					vect1 = nodes[sides-1].transform.position - nodes[sides].transform.position;
					vect2 = nodes[sides].transform.position - newNode.transform.position;
					dot1 = Vector3.Dot ( vect1.normalized, vect2.normalized);
					if ( Mathf.Abs (dot1 - 1) < 0.001 ) {
						continue;
					}

					//we should check before closing if the structure is convergent
					vect1 = nodes[1].transform.position - nodes[0].transform.position;
					vect2 = nodes[1].transform.position - nodes[2].transform.position;
					cross1 = Vector3.Cross(vect1, vect2) + nodes[1].transform.position;
					vect1 = nodes[0].transform.position - nodes[sides].transform.position;
					vect2 = nodes[0].transform.position - nodes[1].transform.position;
					cross2 = Vector3.Cross(vect1, vect2) + newNode.transform.position;
					structPlane = new Plane();
					structPlane.Set3Points(
						nodes[0].transform.position, nodes[1].transform.position, nodes[2].transform.position );
					if( !structPlane.SameSide( cross1, cross2 ) ) {
						//Debug.Log ("Structure unwinding");
						continue;
					}

					type = sides + 1;
					return;
				}

				//found a candidate which is the 5h but not closing the structure? not useful
				if (sides == 4) {
//					Debug.Log("already 4 sides and not closing");
					continue;
				}

				//creates the second side? just add it to candidates - unless is on a straight line from first segment
				if (sides == 1) {

					//check colinearity
					vect1 = nodes[0].transform.position - nodes[1].transform.position;
					vect2 = nodes[1].transform.position - newNode.transform.position;
					dot1 = Vector3.Dot ( vect1.normalized, vect2.normalized);
					if ( Mathf.Abs (dot1 - 1) > 0.001 ) {
//						Debug.Log("Second side-added candidate");
						candidates.Add( newNode );
					}
					continue;
				}

				//is it maybe a node that we already have? so is a semi-closed struct (shaped like a 9)
				//then not useful
				for (int j=1; j<=sides; j++) {
					if (newNode == nodes[j]) {
						newNode = null;
						break;
					}
				}
				if (newNode == null) {
//					Debug.Log("Semi-closed structure");
					continue;
				}

				//ok, so it creates another side

//				vect1 = nodes[0].transform.position - nodes[1].transform.position;
//				vect2 = nodes[1].transform.position - nodes[2].transform.position;
//				dot1 = Vector3.Dot ( vect1.normalized, vect2.normalized);
				vect1 = nodes[sides-1].transform.position - nodes[sides].transform.position;
				vect2 = nodes[sides].transform.position - newNode.transform.position;
				dot2 = Vector3.Dot ( vect1.normalized, vect2.normalized);

				//is it collinear with the last segment?
				if ( Mathf.Abs (dot2 - 1) < 0.001 ) {
//					Debug.Log ("Collinear with last segment");
					continue;
				}
				//is it on the same angle as the first two sides ?
//				if ( Mathf.Abs (dot1 - dot2) > 0.001 ) {
//					continue;
//				}

				//we need to check if this new point is coplanar with the others
				//we build a plant from the first 3 points and check the distance from this one to the plane
				structPlane = new Plane();
				structPlane.Set3Points(
					nodes[0].transform.position, nodes[1].transform.position, nodes[2].transform.position );
				float distToPlane = structPlane.GetDistanceToPoint(newNode.transform.position);
				if (Mathf.Abs (distToPlane) > 0.001) {
					//not coplanar, not valid candidate
//					Debug.Log ("not coplanar");
					continue;
				}

//				//for a plane that goes through the last point, facing the first point
//				//the newpoint in in front (facing the first point) or in the back ?
//				//if in the back, it means that the structure is unwinding -> no ok (e.g. -\_ )
//				vect1 = nodes[sides].transform.position - nodes[0].transform.position;
//				structPlane.SetNormalAndPosition(vect1.normalized, nodes[sides].transform.position);
//				if( !structPlane.SameSide( nodes[0].transform.position, newNode.transform.position)) {
////					Debug.Log ("Other side of the plane");
//					continue;
//				}
				vect1 = nodes[1].transform.position - nodes[0].transform.position;
				vect2 = nodes[1].transform.position - nodes[2].transform.position;
				cross1 = Vector3.Cross(vect1, vect2) + nodes[1].transform.position;
				vect1 = nodes[sides].transform.position - nodes[sides-1].transform.position;
				vect2 = nodes[sides].transform.position - newNode.transform.position;
				cross2 = Vector3.Cross(vect1, vect2) + nodes[sides].transform.position;
				if( !structPlane.SameSide( cross1, cross2 ) ) {
					//Debug.Log ("Structure unwinding");
					continue;
				}


				//valid candidate
				candidates.Add( lastBall.connections[i].ball );
//				Debug.Log ("valid canditdate");
			}
		}

		//if no candidate found, mark structure as invalid -> open struct
		if (candidates.Count == 0) {
			type = INVALID_STRUCT;
			otherCandidates = null;
			return;
		}

		//append the candidate
		nodes[sides + 1] = (GameObject)candidates[0];

		//if only one candidate - return
		if (candidates.Count == 1) {
			return;
		}

		otherCandidates = new GameObject[candidates.Count - 1];
		for (int i=0; i< (candidates.Count - 1); i++)
			otherCandidates[i] = (GameObject)candidates[i+1];
	}

	public void createSurfaceObj(Material mat, bool visible) {
		surfaceObj = new GameObject("Surface_" + type.ToString());
		MeshFilter meshFilter = (MeshFilter)surfaceObj.AddComponent(typeof(MeshFilter));
		MeshRenderer renderer = surfaceObj.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

		int[] indices3 = { 0, 1, 2, 0, 1, 2 };
		int[] indices4 = { 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3 };
		int[] indices5 = { 0, 1, 2, 0, 2, 4, 2, 3, 4, 0, 1, 2, 0, 2, 4, 2, 3, 4 };
		int[] triangles3 = { 0, 1, 2, 3, 5, 4 };
		int[] triangles4 = { 0, 1, 2, 3, 4, 5, 6, 8, 7, 9, 11, 10 };
		int[] triangles5 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 10, 12, 14, 13, 15, 17, 16 };

//		string descriptor = "";
//		for (int i=0; i<type; i++)
//			descriptor = descriptor + nodes[i].GetComponent<BallData>().name + " ";
//		Debug.Log ("Create surface: " + type.ToString() + " " + descriptor);

		Mesh mesh = new Mesh();
		meshFilter.mesh = mesh;
		mesh.Clear();
		Vector3[] vertices = null;
		int[] triangles = null;

		switch (type) {
		case 3:
			vertices = new Vector3[ indices3.Length ];
			for (int i=0; i<indices3.Length; i++)
				vertices[i] = new Vector3(nodes[indices3[i]].transform.position.x, nodes[indices3[i]].transform.position.y, nodes[indices3[i]].transform.position.z);
			triangles = (int[])triangles3.Clone();
			break;
		case 4:
			vertices = new Vector3[ indices4.Length ];
			for (int i=0; i<indices4.Length; i++)
				vertices[i] = new Vector3(nodes[indices4[i]].transform.position.x, nodes[indices4[i]].transform.position.y, nodes[indices4[i]].transform.position.z);
			triangles = (int[])triangles4.Clone();
			break;
		case 5:
			vertices = new Vector3[ indices5.Length ];
			for (int i=0; i<indices5.Length; i++)
				vertices[i] = new Vector3(nodes[indices5[i]].transform.position.x, nodes[indices5[i]].transform.position.y, nodes[indices5[i]].transform.position.z);
			triangles = (int[])triangles5.Clone();
			break;
		}

		Vector2[] uv = new Vector2[ vertices.Length ];
		for (int i=0; i < vertices.Length; i++)
			uv[i] = new Vector2( vertices[i].x, vertices[i].z);

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uv;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();

		surfaceObj.SetActive(visible);
		renderer.material = mat;
	}
}
 