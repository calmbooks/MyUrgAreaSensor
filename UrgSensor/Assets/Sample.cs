using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour {

    public UrgAreaSensor urgAreaSensor;

    public List<Vector2> samplePoints;
    public int sampleAreaWidth;
    public int sampleAreaHeight;

	void Start () {
        urgAreaSensor.ReceiveHandlers += onPointsReceive;	
	}
	
	void Update () {	
	}

	void onPointsReceive ( List<Vector2> _points, int _areaWidth, int _areaHeight  ) {
        samplePoints = _points;
        sampleAreaWidth = _areaWidth;
        sampleAreaHeight = _areaHeight;
    }
}
