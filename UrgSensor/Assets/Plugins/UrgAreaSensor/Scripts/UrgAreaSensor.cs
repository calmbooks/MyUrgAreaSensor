using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class UrgAreaSensor : MonoBehaviour {

	public delegate void  UrgAreaSensorDelegate( List<Vector2> _points, int _areaWidth, int _areaHeight );
	public event UrgAreaSensorDelegate ReceiveHandlers;

	string ip_address = "192.168.0.10"; 
	int port_number = 10940;

    public Material lineMaterial;
    // public Material lineTestMaterial;

	private Vector3[] directions;

	UrgDeviceEthernet urg;

	public float scale = 0.1f;

	List<long> distances = new List<long>();

    int AREA_WIDTH = 200;
    int AREA_HEIGHT = 100;

    int AREA_OFFSET_X = 0;
    int AREA_OFFSET_Y = 50;

    bool isUpdate = false;

    public Material viewMat;    

    float[] view_points_x = new float[500];
    float[] view_points_y = new float[500];

	List<Vector2> points = new List<Vector2>();
	List<Vector2> normalizePoints = new List<Vector2>();

    int mabikiCount = 0;

    void Awake() {
        Application.targetFrameRate = 100;
    } 

	void Start () {

        viewMat.SetInt("_AREA_WIDTH", AREA_WIDTH);
        viewMat.SetInt("_AREA_HEIGHT", AREA_HEIGHT);
        viewMat.SetInt("_AREA_OFFSET_X", AREA_OFFSET_X);
        viewMat.SetInt("_AREA_OFFSET_Y", AREA_OFFSET_Y);

        // viewMat.SetFloatArray("_view_points_x", new float[] { 0.25f, 0.75f, 10.0f, 10.0f });
        // viewMat.SetFloatArray("_view_points_y", new float[] { 0.55f, 0.15f, 10.0f, 10.0f });
        // viewMat.SetInt("_pointCount", 500);

        Invoke("StartDelay0", 1.0f);
	}

	void StartDelay0 () {
		urg = this.gameObject.AddComponent<UrgDeviceEthernet>();
		urg.StartTCP(ip_address, port_number);
		urg.Write(SCIP_library.SCIP_Writer.MD(0, 1080, 1, 0, 0));

        Invoke("StartDelay1", 1.0f);
    }

	void StartDelay1 () {

        isUpdate = true;
    }

	
	void Update () {
        
        if( !isUpdate ) return;
		
		float d = Mathf.PI * 2 / 1440;
		float offset = d * 540;

        if( urg.distances.Count != 1081 ) return;

        distances.Clear();
        distances.AddRange(urg.distances);

		// cache directions
		if(urg.distances.Count > 0 ) {
            directions = new Vector3[distances.Count];
            for(int i = 0; i < directions.Length; i++){
                float a = d * i + offset;
                directions[i] = new Vector3(-Mathf.Cos(a), -Mathf.Sin(a), 0);
            }
		}

        if( urg.distances.Count != 1081 ) {
            Debug.Log("no 1081" + urg.distances.Count);
            return;
        }

        points.Clear();
        normalizePoints.Clear();

        int pindex = 0;

        int _tmpTotal = 0;
        float _tmpCountX = 0;
        float _tmpCountY = 0;

        for(int i = 0; i < distances.Count; i++) {

            Vector3 ddd = distances[i] * directions[i] * scale;

            float x = ddd.x;
            float y = ddd.y;

            bool inXY = GetInArea(x, y);

            if( inXY ) {
                _tmpTotal += 1;
                _tmpCountX += x;
                _tmpCountY += y;
            }
            else if( _tmpTotal > 0 ) { // inXY が続かなかったらpoint算出


                float _x = _tmpCountX / _tmpTotal;
                float _y = _tmpCountY / _tmpTotal;

                points.Add(new Vector2(_x, _y));

                float _nx = _x - ( -( AREA_WIDTH * 0.5f ) + AREA_OFFSET_X );
                float _ny = _y - ( -( AREA_HEIGHT * 0.5f ) + AREA_OFFSET_Y );

                normalizePoints.Add(new Vector2(_nx, _ny));

                pindex += 1;

                _tmpTotal = 0;
                _tmpCountX = 0;
                _tmpCountY = 0;

                if( 499 <= pindex ) {
                    break;
                }
            }
        }

        SetViewMat();

        if( mabikiCount == 3 ) {
            ReceiveHandlers(normalizePoints, AREA_WIDTH, AREA_HEIGHT);
            mabikiCount = 0;
        }
        else {
            mabikiCount += 1;
        }
    }

    bool GetInArea( float x, float y ) {

        bool inX = -( AREA_WIDTH * 0.5f ) + AREA_OFFSET_X <= x && x <= ( AREA_WIDTH * 0.5f ) + AREA_OFFSET_X;
        bool inY = -( AREA_HEIGHT * 0.5f ) + AREA_OFFSET_Y <= y && y <= ( AREA_HEIGHT * 0.5f ) + AREA_OFFSET_Y;

        return inX && inY;
    }

    void SetViewMat() {

        ResetPointArray();

        for( int i = 0, imax = points.Count; i < imax; ++i ) {

            view_points_x[i] = points[i].x;
            view_points_y[i] = points[i].y;
        }

        viewMat.SetFloatArray("_points_x", view_points_x);
        viewMat.SetFloatArray("_points_y", view_points_y);
        viewMat.SetInt("_pointCount", 500);
	}

    void ResetPointArray() {

        for( int i = 0, imax = view_points_x.Length; i < imax; ++i ) {
            view_points_x[i] = 5000.0f;
            view_points_y[i] = 5000.0f;
        }
    } 

    public static float distance2d( Vector2 _vec1, Vector2 _vec2 ) {

        float dx = _vec2.x - _vec1.x;
        float dy = _vec2.y - _vec1.y;

        float dis = Mathf.Sqrt((dx * dx) + (dy * dy)); 

        return dis;
    }

    public static Vector2 mix2d( Vector2 _vec1, Vector2 _vec2, float r ) {

        float x = ( _vec2.x - _vec1.x ) * r + _vec1.x;
        float y = ( _vec2.y - _vec1.y ) * r + _vec1.y;

        return new Vector2(x, y);
    }    

    void OnRenderObject() {

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

        GL.Vertex3(0f, 0f, 0f);

		float d = Mathf.PI * 2 / 1440;        
		float offset = d * 540;        

        for(int i = 0; i < distances.Count; i++){
            
            float a = d * i + offset;
            Vector3 dir = new Vector3(-Mathf.Cos(a), -Mathf.Sin(a), 0);

            long dist = distances[i];
            // Debug.DrawRay(Vector3.zero, dist * dir * scale, distanceColor);
            Vector3 ddd = dist * dir * scale;
            GL.Vertex3(ddd.x, ddd.y, ddd.z);
            GL.Vertex3(0f, 0f, 0f);
        }

        GL.End();
        GL.PopMatrix ();



        /*
        lineTestMaterial.SetPass(0);
        GL.PushMatrix ();
        GL.MultMatrix (transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        GL.Vertex3(0f, 0f, 0f);
        GL.Vertex3(90f, 90f, 0f);
        GL.End();
        GL.PopMatrix ();
        */
    }
}
