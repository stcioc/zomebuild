using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MiniJSON;


public class WorldData {

	//list of balls
	private Dictionary<string, GameObject> balls;
	//camera info
	public Vector3 cameraPos, cameraTarget, cameraUp;
	//list of surfaces
	private List<SurfaceData> surfaces;
	//materials for surfaces
	private Material matTri, matQuad, matPenta;
	//surfaces visibile by default
	public bool visibleTri, visibleQuad, visiblePenta;
	//labels visible by default
	public bool visibleLabels;


	//creates a new worls and adds a first ball
	public void newWorld() {
		balls = new Dictionary<string, GameObject>();
		surfaces = new List<SurfaceData>();
		GameObject newBall = createBall(0);
		balls.Add(newBall.name, newBall);
	}

	//load the world from json file
	public string loadWorld(string jsonString) {
		Dictionary<string,object> dict, elem;
		
		balls = new Dictionary<string, GameObject>();
		surfaces = new List<SurfaceData>();
		cameraPos = new Vector3();
		cameraTarget = new Vector3();
		cameraUp = new Vector3();
		matTri = Resources.Load<Material>("Surface_triangle");
		matQuad = Resources.Load<Material>("Surface_quad");
		matPenta = Resources.Load<Material>("Surface_penta");
		visibleTri = false;
		visiblePenta = false;
		visibleQuad = false;
		visibleLabels = false;

		try {
			dict = Json.Deserialize(jsonString) as Dictionary<string,object>;
		}
		catch (System.Exception e) {
			return "There was an error loading the data model! The message from exception is:\n" + e.Message;
		}

		try {
			elem = (Dictionary<string, object>)dict["cameraPos"];
			cameraPos.Set ( (float)(double)elem["x"], (float)(double)elem["y"], (float)(double)elem["z"]);

			elem = (Dictionary<string, object>)dict["cameraTarget"];
			cameraTarget.Set ( (float)(double)elem["x"], (float)(double)elem["y"], (float)(double)elem["z"]);

			elem = (Dictionary<string, object>)dict["cameraUp"];
			cameraUp.Set ( (float)(double)elem["x"], (float)(double)elem["y"], (float)(double)elem["z"]);
		}
		catch (System.Exception) {
			cameraPos.Set (0, 0, 20f);
			cameraTarget = Vector3.zero;
			cameraUp = Vector3.up;
		}

		try {
			//load balls
			List<object> ballList = (List<object>)dict["balls"];
			if (ballList.Count == 0)
				return "File format error: there are no balls!";

			for (int i=0; i<ballList.Count; i++) {
				elem = (Dictionary<string, object>)ballList[i];
				
				GameObject newBall = createBall(0);
				newBall.GetComponent<BallData>().loadFromJSON(elem);
				newBall.GetComponent<BallData>().connections = new List<Connection>();
				balls.Add(newBall.name, newBall);
			}

			//load struts
			List<object> strutList = (List<object>)dict["struts"];

			for (int i=0; i<strutList.Count; i++) {
				elem = (Dictionary<string, object>)strutList[i];

				int type, position;

				int indexFrom = (int)(long)elem["from"];
				int indexTo = (int)(long)elem["to"];
				GameObject ballFrom = balls["BALL_" + indexFrom];
				GameObject ballTo = balls["BALL_" + indexTo];

				PositionPhi p = ballTo.GetComponent<BallData>().position;
				p.subtract( ballFrom.GetComponent<BallData>().position);
				GeometryData.getPositionType( p, out position, out type );
				if (type == -1) {
					return "File format error: no valid connection could be built between balls " + indexFrom + " and " + indexTo;
				}

				GameObject newStrut = createStrut(type);
				positionStrut(ballFrom, newStrut, type, position);
				addStrutToModel(ballTo, newStrut, ballFrom, position, type);
			}

			//clean up and call garbage collector
			dict.Clear();
			dict = null;
			System.GC.Collect();
		}
		catch (System.Exception e) {
			return "File format error; there was an error loading the data model! The message from exception is:\n" + e.Message;
		}
		return "";
	}

	//serialize the world in a string to be written in a json file
	public string serializeWorld() {
		bool isFirst;

		System.Text.StringBuilder sb = new System.Text.StringBuilder ();
		System.IFormatProvider iFormatProvider = new System.Globalization.CultureInfo("");

		sb.Append ("{\n");
		sb.Append ("\"creator\": \"Zomebuild version 1.0\",\n");
		sb.Append ("\"cameraPos\": { ");
		sb.Append ( string.Format (iFormatProvider, "\"x\":{0:#0.00000}, \"y\":{1:#0.00000}, \"z\":{2:#0.00000}", cameraPos.x, cameraPos.y, cameraPos.z));
		sb.Append (" },\n");
		sb.Append ("\"cameraTarget\": { ");
		sb.Append ( string.Format (iFormatProvider, "\"x\":{0:#0.00000}, \"y\":{1:#0.00000}, \"z\":{2:#0.00000}", cameraTarget.x, cameraTarget.y, cameraTarget.z));
		sb.Append (" },\n");
		sb.Append ("\"cameraUp\": { ");
		sb.Append ( string.Format (iFormatProvider, "\"x\":{0:#0.00000}, \"y\":{1:#0.00000}, \"z\":{2:#0.00000}", cameraUp.x, cameraUp.y, cameraUp.z));
		sb.Append (" },\n");
		sb.Append ("\"balls\": [\n");

		isFirst = true;
		foreach(string item in balls.Keys) {
			if (isFirst)
				isFirst = false;
			else
				sb.Append (",\n");
			sb.Append (balls[item].GetComponent<BallData>().getBallDescriptor());
		}
		sb.Append ("\n],\n\"struts\": [\n");

		isFirst = true;
		foreach(string item in balls.Keys) {
			string descriptor = balls[item].GetComponent<BallData>().getConnectionsDescriptor();

			if (descriptor != "") {
				if (isFirst)
					isFirst = false;
				else
					sb.Append (",\n");
				sb.Append (descriptor);
			}
		}
		sb.Append ("\n]\n}");

		return sb.ToString();
	}

	//gets the fisrt available index - xurrent maximum index + 1
	public int getNextindex() {
		int maxIndex = -1;
	
		foreach(string item in balls.Keys) {
			int index = System.Convert.ToInt32(item.Substring(5));
			if (index > maxIndex)
				maxIndex = index;
		}
		return maxIndex + 1;
	}

	public GameObject find(string name) {
		return balls[name];
	}

	public int countBalls()
    {
		return balls.Count;
    }

	//creates a new ball with given index
	public GameObject createBall(int index) {
		GameObject newBall;
		string newName = string.Format("BALL_{0}", index);

		newBall = (GameObject)Object.Instantiate (Resources.Load ("BallPrefab"));
		newBall.GetComponent<MeshFilter>().mesh = GeometryData.mesh;
		newBall.name = newName;
		newBall.GetComponent<BallData>().connections = new List<Connection>();
		newBall.GetComponent<BallData>().ballId = index;

		GameObject labelObj = (GameObject)Object.Instantiate (Resources.Load ("LabelPrefab"));
		labelObj.name = "LABEL_" + newName;
		labelObj.transform.parent = newBall.transform;
		labelObj.SetActive(visibleLabels);
		labelObj.GetComponent<TextMesh>().text = "";
		newBall.GetComponent<BallData>().label = labelObj;
		return newBall;
	}

	//creates a strut object based on type
	public GameObject createStrut(int type) {
		GameObject newStrut;
		string[] prefabNames = { "Strut_Blue", "Strut_Yellow", "Strut_Red" };

		newStrut = (GameObject)Object.Instantiate(Resources.Load (prefabNames[type/3]));
		newStrut.transform.localScale = new Vector3(newStrut.transform.localScale.x, newStrut.transform.localScale.y, 
		                                            GeometryData.strutLen[type] - GeometryData.ballDiam[type/3]);
		return newStrut;
	}

	public void positionBall(GameObject ballFrom, GameObject newBall, int type, int position) {
		newBall.GetComponent<BallData>().setPositionDelta(ballFrom.GetComponent<BallData>(), position, type%3);
	}
	
	public void positionStrut(GameObject ballFrom, GameObject newStrut, int type, int position) {
		newStrut.transform.position = GeometryData.outPoints[position] * GeometryData.strutLen[type]/2 + ballFrom.transform.position;
		newStrut.transform.LookAt(ballFrom.transform.position, GeometryData.upVectors[position]);
	}

	public string addBallToModel(GameObject newBall, GameObject newStrut, GameObject objClicked, int position, int action) {
		Connection conn;

		conn.ball = newBall;
		conn.position = position;
		conn.linkType = action;
		conn.strut = newStrut;
		objClicked.GetComponent<BallData>().connections.Add (conn);
		
		conn.ball = objClicked;
		conn.position = ( (position % 2) == 0)? (position+1): (position-1);
		newBall.GetComponent<BallData>().connections.Add (conn);
		balls.Add (newBall.name, newBall);

		newBall.GetComponent<BallData>().label.GetComponent<TextMesh>().text = newBall.GetComponent<BallData>().getLabelString();
		
		return string.Format("ADD_BALL {0}", newBall.name);
	}

	public void addSingleBallToModel(GameObject newBall) {
		balls.Add (newBall.name, newBall);
	}

	public string addStrutToModel(GameObject targetBall, GameObject newStrut, GameObject objClicked, int position, int action) {
		Connection conn;
		
		conn.ball = targetBall;
		conn.position = position;
		conn.linkType = action;
		conn.strut = newStrut;
		objClicked.GetComponent<BallData>().connections.Add (conn);
		
		conn.ball = objClicked;
		conn.position = ( (position % 2) == 0)? (position+1): (position-1);
		targetBall.GetComponent<BallData>().connections.Add (conn);

		removeDupSurfaces(targetBall, objClicked);
		findSurfaces(targetBall, objClicked, action);

		return string.Format("ADD_STRUT {0} {1}", objClicked.name, targetBall.name);
	}

	public void setSurfaceVisible(bool visible, int type) {
		for (int i=0; i< surfaces.Count; i++) {
			if (surfaces[i].type == type) {
				surfaces[i].surfaceObj.SetActive(visible);
			}
		}

		switch(type) {
		case 3:
			visibleTri = visible;
			break;
		case 4:
			visibleQuad = visible;
			break;
		case 5:
			visiblePenta = visible;
			break;
		}
	}

	public string deleteBall(GameObject objClicked) {
		if (balls.Count == 1)
			return "DELETE_LAST";

		string log = string.Format ("DELETE {0}", objClicked.name);
		
		for (int i=0; i< objClicked.GetComponent<BallData>().connections.Count; i++) {
			
			//remove connection from other side
			GameObject objLinked = objClicked.GetComponent<BallData>().connections[i].ball;
			for (int j=0; j<objLinked.GetComponent<BallData>().connections.Count; j++) {
				if (objLinked.GetComponent<BallData>().connections[j].ball == objClicked) {
					objLinked.GetComponent<BallData>().connections.RemoveAt(j);
					break;
				}
			}
			
			//remove rod
			Object.Destroy( objClicked.GetComponent<BallData>().connections[i].strut );
			
			//log
			int position = objClicked.GetComponent<BallData>().connections[i].position;
			log += string.Format (" {0} {1} {2}", objLinked.name,
			                      (position%2 == 0)? (position+1):(position-1),
			                      objClicked.GetComponent<BallData>().connections[i].linkType);
		}
		
		if (objClicked.GetComponent<BallData>().connections.Count == 0) {
			log = objClicked.GetComponent<BallData>().getDeleteSingleString();
		}

		//search for surfaces
		for (int i=0; i<surfaces.Count; i++) {
			if (surfaces[i].contains(objClicked)) {
				Object.Destroy ( surfaces[i].surfaceObj );
				surfaces.RemoveAt(i);
				i--;
			}
		}
		
		balls.Remove(objClicked.name);
		Object.Destroy ( objClicked );
		return log;
	}

	private void removeDupSurfaces(GameObject linkA, GameObject linkB) {
		int index = 0;
		while (index < surfaces.Count) {
			if (surfaces[index].type == 4 && surfaces[index].containsBoth(linkA, linkB)) {
				//Debug.Log ("Removing surface: ");
				//surfaces[index].Dump();
				surfaces[index].delete();
				surfaces.RemoveAt(index);
			}
			else
				index++;
		}
	}

	private void findSurfaces(GameObject linkA, GameObject linkB, int linkType) {
		//collection of candidate surfaces
		List<SurfaceData> candidateSurfaces = new List<SurfaceData>();
		bool foundUnkonwn;
		GameObject[] otherCandidates;

		candidateSurfaces.Add ( new SurfaceData());
		candidateSurfaces[0].initSurface(linkA, linkB);

//		Debug.Log ("find surfaces");
		//loop and add balls to surfaces until there are no more unknown surfaces
		// all closed or invalid
		do {
			foundUnkonwn = false;
			int toProcess = candidateSurfaces.Count;

			//Debug.Log ("Start cycle");
			for (int i=0; i< toProcess; i++) {

				//candidateSurfaces[i].Dump();

				if (candidateSurfaces[i].type == SurfaceData.UNKNOWN_STRUCT) {
					foundUnkonwn = true;
					candidateSurfaces[i].tryAddNext(linkType, out otherCandidates);
					if (otherCandidates != null) {
						//we found other candidates, so we should replicate current surface and add
						for (int j=0; j<otherCandidates.Length; j++) {
							SurfaceData newSurf = new SurfaceData();
							newSurf.copy(candidateSurfaces[i]);
							newSurf.replaceLast(otherCandidates[j]);
							candidateSurfaces.Add (newSurf);
						}

					}
				}
			}
		} while (foundUnkonwn);

//		//for the remaining surfaces, we should check if higher order surfaces
//		// do no overlap with lower-order surfaces
//		//in this case we should add only the lower order
		for (int i=0; i<candidateSurfaces.Count; i++) {
			if (candidateSurfaces[i].type == 4) {

				//compare with 
				for (int j=0; j< candidateSurfaces.Count; j++) {
					if (candidateSurfaces[j].type == 3) {
						if (candidateSurfaces[i].includeSurface(candidateSurfaces[j])) {
							candidateSurfaces[i].type = SurfaceData.INVALID_STRUCT;
							break;
						}
					}
				}
			}
		}


		//add the remaining surfaces to model
		for (int i=0; i<candidateSurfaces.Count; i++) {
			switch (candidateSurfaces[i].type) {
			case 3:
				//candidateSurfaces[i].Dump();
				candidateSurfaces[i].createSurfaceObj(matTri, visibleTri);
				surfaces.Add(candidateSurfaces[i]);
				break;
			case 4:
				//candidateSurfaces[i].Dump();
				candidateSurfaces[i].createSurfaceObj(matQuad, visibleQuad);
				surfaces.Add(candidateSurfaces[i]);
				break;
			case 5:
				//candidateSurfaces[i].Dump();
				candidateSurfaces[i].createSurfaceObj(matPenta, visiblePenta);
				surfaces.Add(candidateSurfaces[i]);
				break;
			default:
				break;
			}
		}
	}

	public void showLabels (bool visible) {
		foreach (KeyValuePair<string, GameObject> ball in balls) {
			ball.Value.GetComponent<BallData>().label.SetActive(visible);
		}
		visibleLabels = visible;
	}

	public void orientLabels( Vector3 cameraPos, Vector3 cameraTarget, Vector3 cameraUp) {
		Vector3 target = cameraPos - (cameraPos - cameraTarget).normalized * 1000;
		Vector3 deltaPos = cameraUp.normalized * 1.5f;

		foreach (KeyValuePair<string, GameObject> ball in balls) {
			ball.Value.GetComponent<BallData>().label.transform.localPosition = deltaPos;
			ball.Value.GetComponent<BallData>().label.transform.LookAt(target, cameraUp);
		}
	}

}
