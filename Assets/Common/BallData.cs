using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//models a ball connection to other balls
public struct Connection {
	//starting position
	public int position;
	//link to which ball
	public GameObject ball;
	//link type: 
	public int linkType;
	//strut object
	public GameObject strut;
}

//models a Vector3 described as multiples of 1 and tau
public struct PositionPhi {
	public int x0, x1, y0, y1, z0, z1;
	public Vector3 getFloatPos() {
		return new Vector3( x0 + x1 * GeometryData.TAU, y0 + y1 * GeometryData.TAU, z0 + z1 * GeometryData.TAU);
	}
	public void subtract( PositionPhi p) {
		x0 -= p.x0;
		x1 -= p.x1;
		y0 -= p.y0;
		y1 -= p.y1;
		z0 -= p.z0;
		z1 -= p.z1;
	}
}

//a ball in the model
public class BallData : MonoBehaviour {

	//id of the ball - unique in the model (incremental)
	public int ballId;
	//position in phi coordinates
	public PositionPhi position;
	//list of connections to other balls
	public List<Connection> connections;
	//the label of the ball
	public GameObject label;

	public void setPositionDelta(BallData  ball, int index, int length) {
		position = ball.getPositionFrom(index, length);
		transform.position = position.getFloatPos();
	}

	public PositionPhi getPositionFrom(int index, int length) {
		PositionPhi pos;
		pos.x0 = position.x0 + GeometryData.coords[index*18 + length*6 + 0];
		pos.x1 = position.x1 + GeometryData.coords[index*18 + length*6 + 1];
		pos.y0 = position.y0 + GeometryData.coords[index*18 + length*6 + 2];
		pos.y1 = position.y1 + GeometryData.coords[index*18 + length*6 + 3];
		pos.z0 = position.z0 + GeometryData.coords[index*18 + length*6 + 4];
		pos.z1 = position.z1 + GeometryData.coords[index*18 + length*6 + 5];
		return pos;
	}

	public string getBallDescriptor() {
		return "{" +
			string.Format ("\"id\":{0}, \"x0\":{1}, \"x1\":{2}, \"y0\":{3}, \"y1\":{4}, \"z0\":{5}, \"z1\":{6}",
			               ballId, position.x0, position.x1, position.y0, position.y1, position.z0, position.z1) +
				"}";
	}

	public string getConnectionsDescriptor() {
		string descriptor = "", connDescript;

		for (int i=0; i<connections.Count; i++) {
			if ( ballId < connections[i].ball.GetComponent<BallData>().ballId) {
				connDescript = "{" +
					string.Format ("\"from\":{0}, \"to\":{1}", ballId, connections[i].ball.GetComponent<BallData>().ballId) +
						"}";
				if (descriptor == "")
					descriptor = connDescript;
				else
					descriptor = descriptor + ",\n" + connDescript;
			}
		}
		return descriptor;
	}

	public string getLabelString() {
		return string.Format ("{0}: {1},{2},{3},{4},{5},{6}", ballId, position.x0, position.x1, position.y0, position.y1, position.z0, position.z1);
	}

	public string getDeleteSingleString() {
		return string.Format ("DELETE_SGL {0} {1} {2} {3} {4} {5} {6}", ballId, position.x0, position.x1, position.y0, position.y1, position.z0, position.z1);
	}

	public void setPositionPhi(int x0, int x1, int y0, int y1, int z0, int z1) {
		position.x0 = x0;
		position.x1 = x1;
		position.y0 = y0;
		position.y1 = y1;
		position.z0 = z0;
		position.z1 = z1;
		transform.position = position.getFloatPos();
		label.GetComponent<TextMesh>().text = getLabelString();
	}

	public void orientLabel( Vector3 cameraPos, Vector3 cameraTarget, Vector3 cameraUp) {
		Vector3 target = cameraPos - (cameraPos - cameraTarget).normalized * 1000;
		Vector3 deltaPos = cameraUp.normalized * 1.5f;
		
		label.transform.localPosition = deltaPos;
		label.transform.LookAt(target, cameraUp);
	}

	public void loadFromJSON(Dictionary<string, object> dict) {
		ballId = (int)(long)dict["id"];
		position = new PositionPhi();
		position.x0 = (int)(long)dict["x0"];
		position.x1 = (int)(long)dict["x1"];
		position.y0 = (int)(long)dict["y0"];
		position.y1 = (int)(long)dict["y1"];
		position.z0 = (int)(long)dict["z0"];
		position.z1 = (int)(long)dict["z1"];

		transform.position = position.getFloatPos();
		name = "BALL_" + ballId;
		label.name = "LABEL_BALL_" + ballId;
		label.GetComponent<TextMesh>().text = getLabelString();

		connections = new List<Connection>();
	}

}
