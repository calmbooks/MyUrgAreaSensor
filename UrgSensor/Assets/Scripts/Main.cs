using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

	string ip_address = "192.168.0.10"; 
	int port_number = 10940;

    public Material lineMaterial;
    public Material lineTestMaterial;

	private Vector3[] directions;

	UrgDeviceEthernet urg;

	public float scale = 0.1f;

	List<long> distances;

    int AREA_WIDTH = 200;
    int AREA_HEIGHT = 100;

    int AREA_OFFSET_X = 0;
    int AREA_OFFSET_Y = 100;

    bool isUpdate = false;

    public Material viewMat;    

    float[] points_x = new float[500];
    float[] points_y = new float[500];

    void Awake() {
        Application.targetFrameRate = 100;
    } 

	void Start () {

		distances = new List<long>();


        viewMat.SetInt("_AREA_WIDTH", AREA_WIDTH);
        viewMat.SetInt("_AREA_HEIGHT", AREA_HEIGHT);
        viewMat.SetInt("_AREA_OFFSET_X", AREA_OFFSET_X);
        viewMat.SetInt("_AREA_OFFSET_Y", AREA_OFFSET_Y);

        // viewMat.SetFloatArray("_points_x", new float[] { 0.25f, 0.75f, 10.0f, 10.0f });
        // viewMat.SetFloatArray("_points_y", new float[] { 0.55f, 0.15f, 10.0f, 10.0f });
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


        ResetPointArray();

        int pindex = 0;

        Debug.Log(-( AREA_WIDTH * 0.5 ) + AREA_OFFSET_X);
        Debug.Log((AREA_WIDTH * 0.5 ) + AREA_OFFSET_X);



        for(int i = 0; i < distances.Count; i++) {
            Vector3 ddd = distances[i] * directions[i] * scale;


            float x = ddd.x;
            float y = ddd.y;


            // Debug.Log(ddd);
            // points_x[0] = x;
            // points_y[0] = y;

            bool inXY = GetInArea(x, y);

            if( inXY ) {
                Debug.Log("kita");

                if( pindex <= 0 ) {
                    points_x[pindex] = x;
                    points_y[pindex] = y;

                    pindex += 1;

                    continue;
                }

                if( 499 <= pindex ) {
                    continue;
                }

                Vector2 cvec = new Vector2(x,y);
                bool ismerge = false;

                // 近くにポイントがある場合はマージ（mix0.5）
                for( int pi = 0, pimax = pindex; pi < pindex; ++pi ) {

                    float bx = points_x[pi];
                    float by = points_y[pi];

                    Vector2 bvec = new Vector2(bx,by);
                    float distance = distance2d(bvec, cvec);

                    if( distance < 40 ) {
                        Vector2 mix = mix2d(bvec, cvec, 0.5f);
                        points_x[pi] = mix.x;
                        points_y[pi] = mix.y;
                        ismerge = true;
                        break;
                    }
                }

                // 前のpointがエリア内の場合は省く
                if( !ismerge ) { 
                    float bx = points_x[pindex-1];
                    float by = points_y[pindex-1];
                    Vector2 bvec = new Vector2(bx,by);

                    bool inXYb = GetInArea(bx, by);

                    if( inXYb ) {
                        // Vector2 mix = mix2d(bvec, cvec, 0.5f);
                        // points_x[pindex-1] = mix.x;
                        // points_y[pindex-1] = mix.y;
                        ismerge = true;
                    }
                }

                // マージがない場合は新規追加
                if( !ismerge ) {
                    points_x[pindex] = x;
                    points_y[pindex] = y;
                }

                pindex += 1;
            }
        }

        viewMat.SetFloatArray("_points_x", points_x);
        viewMat.SetFloatArray("_points_y", points_y);
        viewMat.SetInt("_pointCount", 500);
	}

    bool GetInArea( float x, float y ) {

        bool inX = -( AREA_WIDTH * 0.5 ) + AREA_OFFSET_X <= x && x <= ( AREA_WIDTH * 0.5 ) + AREA_OFFSET_X;
        bool inY = -( AREA_HEIGHT * 0.5 ) + AREA_OFFSET_Y <= y && y <= ( AREA_HEIGHT * 0.5 ) + AREA_OFFSET_Y;

        return inX && inY;
    }

    void ResetPointArray() {

        for( int i = 0, imax = points_x.Length; i < imax; ++i ) {
            points_x[i] = 5000.0f;
            points_y[i] = 5000.0f;
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



        lineTestMaterial.SetPass(0);
        GL.PushMatrix ();
        GL.MultMatrix (transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        GL.Vertex3(0f, 0f, 0f);
        GL.Vertex3(90f, 90f, 0f);
        GL.End();
        GL.PopMatrix ();
    }
}
