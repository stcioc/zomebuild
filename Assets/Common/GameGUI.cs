using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;
using SFB;
using System.IO;

public struct ShadowData
{
    //shadow/highlighted object
    public GameObject ball;
    //position of the ball
    public int position;
}

public class GameGUI : MonoBehaviour
{

    //materials
    public Material ballMaterial, shadowMaterial;
    //controls
    public GameObject controlPanel, optionsPanel, buttonLabels, buttonTri, buttonQuad, buttonPenta;
    public GameObject messagePanel, inputPanel, questionPanel;
    public Text messageText;
    public InputField inputField;
    //the axis
    public GameObject axisX, axisY, axisZ;

    //camera settings
    public float rotateSpeed = 1.0f;
    public float zoomSpeed = 1.2f;
    public float minDistance = 0f;
    public float maxDistance = 100000f;
    public float AXISSIZE = 5;

    //camera data
    //active rect where the clicks are allowed
    private Rect activeRect = new Rect(0, 0, Screen.width, Screen.height);
    //The target of the camera. The camera will always point to this object.
    private Vector3 origin = Vector3.zero;
    //up vector of the camera. Is adjusted also during the movement
    private Vector3 cameraUp = Vector3.up;
    private Vector3 mouseStart, mouseEnd;
    private float prevTouchDistance;

    //status management
    enum TOOL { BLUE_SHORT, BLUE_MED, BLUE_LONG, YELLOW_SHORT, YELLOW_MED, YELLOW_LONG, RED_SHORT, RED_MED, RED_LONG, DELETE };
    private TOOL option = TOOL.BLUE_SHORT;
    enum STATE { PROCESSING, WAIT, IN_TOUCH_ZOOM, AFTER_TOUCH_ZOOM, IN_TOUCH_ROTATE, IN_MOUSE_ROTATE, IN_MOUSE_PAN, AFTER_TOUCH_DELETE, AFTER_MOUSE_DELETE, TOUCH_DRAG, MOUSE_DRAG };
    private STATE status = STATE.PROCESSING;
    enum ADDSTATE { INVALID, VALID_STRUT, VALID_BALL };
    private ADDSTATE addStatus;

    //option panel scroll effect
    bool isOptionMoving = false, showOptionsPanel = false;
    private float optionPanelInitX;

    //clicked object (if any)
    private GameObject objClicked;

    //drag management
    private GameObject newBall, newStrut, targetBall;
    private int currPosition;
    private ShadowData[] shadows;
    private int shadowNum = 0;
    private List<ShadowData> highlighted;

    //log - undo management
    private int ballIndex;
    private List<string> logEntries;

    //world structure
    private WorldData world;

    // Use this for initialization
    void Start()
    {

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        //initialization
        GeometryData.Init();
        shadows = new ShadowData[20];
        highlighted = new List<ShadowData>();
        world = new WorldData();

        //create shadows
        for (int i = 0; i < 20; i++)
        {
            shadows[i].ball = (GameObject)Instantiate((GameObject)Resources.Load("ShadowPrefab"));
            shadows[i].ball.SetActive(false);
        }

        logEntries = new List<string>();
        loadInitModel();
        status = STATE.WAIT;

        //setup options menu / rect
        optionPanelInitX = ((RectTransform)optionsPanel.transform).anchoredPosition.x;
        activeRect = new Rect();
        setOptionMenu(false);

        //setup axis
        axisX.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.left);
        axisX.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.left);
        axisY.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.up);
        axisY.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.up);
        axisZ.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.forward);
        axisZ.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.forward);

        //setup listeners
        buttonTri.GetComponent<Toggle>().onValueChanged.AddListener(onToggleSurfaceTri);
        buttonQuad.GetComponent<Toggle>().onValueChanged.AddListener(onToggleSurfaceQuad);
        buttonPenta.GetComponent<Toggle>().onValueChanged.AddListener(onToggleSurfacePenta);
        buttonLabels.GetComponent<Toggle>().onValueChanged.AddListener(onToggleLabels);
    }

   
    public void onClickNew()
    {
        questionPanel.SetActive(true);
        status = STATE.PROCESSING;
    }

    public void onClickOkQuestion()
    {
        deleteCurrentModel();
        loadInitModel();
        logEntries.Clear();
        questionPanel.SetActive(false);

        //setup axis
        axisX.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.left);
        axisX.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.left);
        axisY.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.up);
        axisY.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.up);
        axisZ.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.forward);
        axisZ.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.forward);

        buttonTri.GetComponent<Toggle>().isOn = true;
        buttonQuad.GetComponent<Toggle>().isOn = true;
        buttonPenta.GetComponent<Toggle>().isOn = true;
        buttonLabels.GetComponent<Toggle>().isOn = true;

        status = STATE.WAIT;
    }

    public void onClickOkInput()
    {
       /*
        if (inputField.text == "")
        {
            return;
        }
        fileName = inputField.text + ".json";
        string content = world.serializeWorld();
        BrowserTextDownload(fileName, content);

        inputPanel.SetActive(false);
        status = STATE.WAIT;
       */
    }

    public void onClickCancel()
    {
        questionPanel.SetActive(false);
        status = STATE.WAIT;
    }

    public void onClickSave()
    {
        //save camera
        world.cameraPos = transform.position;
        world.cameraTarget = origin;
        world.cameraUp = cameraUp;
        string content = world.serializeWorld();

#if UNITY_WEBGL
        string fileName = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ".json";
        BrowserInterface.TextDownload(fileName, content);
#elif UNITY_STANDALONE_WIN
        string filePath = StandaloneFileBrowser.SaveFilePanel("Save File", "", "MySaveFile", "json");
        if (filePath != "") {
            try
            {
                StreamWriter sw = new StreamWriter(filePath);
                sw.Write(content);
                sw.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }            
        }
#endif
    }

    public void onClickLoad()
    {
#if UNITY_WEBGL
        BrowserInterface.TextUpload("Main Camera", "onLoadCallback",".json");
#elif UNITY_STANDALONE_WIN
         string[] paths = StandaloneFileBrowser.OpenFilePanel("Open", "", "json", false);
       if (paths.Length > 0) {
            try
            {
                StreamReader sr = new StreamReader(paths[0]);
                string contents = "|" + sr.ReadToEnd();
                sr.Close();
                onLoadCallback(contents);
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }

#endif
    }

    public void onLoadCallback(string callback)
    {
        if (callback == "ERROR")
        {
            return;
        }
        int pos = callback.IndexOf('|');
        if (pos == -1)
        {
            return;
        }

        deleteCurrentModel();
        logEntries.Clear();
        buttonTri.GetComponent<Toggle>().isOn = true;
        buttonQuad.GetComponent<Toggle>().isOn = true;
        buttonPenta.GetComponent<Toggle>().isOn = true;
        buttonLabels.GetComponent<Toggle>().isOn = true;

        string result = world.loadWorld(callback.Substring(pos + 1, callback.Length - pos - 1));
        if (result != "")
        {
            //we don't analyse the result to see what exactly happened - maybe the world wasn't loaded at all, or just partially
            //if there are no balls then the model is inconsistent and we load the "new" one
            if (world.countBalls() == 0)
            {
                TextAsset modelFile = (TextAsset)Resources.Load("empty");
                world.loadWorld(modelFile.text);
            }
        }

        ballIndex = world.getNextindex();
        //setup camera
        transform.position = world.cameraPos;
        origin = world.cameraTarget;
        cameraUp = world.cameraUp;
        transform.LookAt(origin, cameraUp);
        //setup axis
        axisX.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.left);
        axisX.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.left);
        axisY.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.up);
        axisY.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.up);
        axisZ.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.forward);
        axisZ.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.forward);
    }

    private void deleteCurrentModel()
    {
        //delete all existing balls, struts and surfaces
        GameObject[] GameObjects = (FindObjectsOfType<GameObject>() as GameObject[]);
        for (int i = 0; i < GameObjects.Length; i++)
        {
            if (GameObjects[i].name.Length > 5)
            {
                string name = GameObjects[i].name.Substring(0, 5);
                if (name == "BALL_" || name == "Strut" || name == "Surfa")
                {
                    Destroy(GameObjects[i]);
                }
            }
        }
    }

    private void loadInitModel()
    {
        //load empty model
        TextAsset modelFile = (TextAsset)Resources.Load("empty");
        world.loadWorld(modelFile.text);
        ballIndex = world.getNextindex();
        //setup camera
        transform.position = world.cameraPos;
        origin = world.cameraTarget;
        cameraUp = world.cameraUp;
        transform.LookAt(origin, cameraUp);
    }

    public void onClickUndo()
    {
        string[] parts;
        int sizeLogEntries = logEntries.Count;

        if (logEntries.Count == 0)
            return;

        parts = logEntries[sizeLogEntries - 1].Split(' ');
        logEntries.RemoveAt(sizeLogEntries - 1);

        switch (parts[0])
        {
            case "ADD_BALL":
                objClicked = world.find(parts[1]);
                world.deleteBall(objClicked);
                break;
            case "ADD_STRUT":
                objClicked = world.find(parts[1]);
                targetBall = world.find(parts[2]);
                for (int i = 0; i < objClicked.GetComponent<BallData>().connections.Count; i++)
                {

                    if (objClicked.GetComponent<BallData>().connections[i].ball == targetBall)
                    {
                        newStrut = objClicked.GetComponent<BallData>().connections[i].strut;
                        objClicked.GetComponent<BallData>().connections.RemoveAt(i);
                        break;
                    }
                }
                for (int i = 0; i < targetBall.GetComponent<BallData>().connections.Count; i++)
                {

                    if (targetBall.GetComponent<BallData>().connections[i].ball == objClicked)
                    {
                        targetBall.GetComponent<BallData>().connections.RemoveAt(i);
                        break;
                    }
                }

                Destroy(newStrut);
                break;
            case "DELETE":
                objClicked = world.find(parts[2]);
                currPosition = int.Parse(parts[3]);
                int action = int.Parse(parts[4]);
                newBall = world.createBall(int.Parse(parts[1].Substring(5)));
                newStrut = world.createStrut(action);
                world.positionBall(objClicked, newBall, action, currPosition);
                world.positionStrut(objClicked, newStrut, action, currPosition);
                newBall.GetComponent<BallData>().orientLabel(transform.position, origin, cameraUp);
                world.addBallToModel(newBall, newStrut, objClicked, currPosition, action);

                for (int i = 5; i < parts.Length; i += 3)
                {
                    objClicked = world.find(parts[i]);
                    currPosition = int.Parse(parts[i + 1]);
                    action = int.Parse(parts[i + 2]);
                    newStrut = world.createStrut(action);
                    world.positionStrut(objClicked, newStrut, action, currPosition);
                    world.addStrutToModel(newBall, newStrut, objClicked, currPosition, action);
                }

                break;
            case "DELETE_SGL":
                newBall = world.createBall(int.Parse(parts[1]));
                newBall.GetComponent<BallData>().setPositionPhi(int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                world.addSingleBallToModel(newBall);
                newBall.SetActive(true);
                break;
        }
    }

    public void onClickOptions()
    {
        showOptionsPanel = !showOptionsPanel;
        isOptionMoving = true;
    }

    private void setOptionMenu(bool isVisible)
    {
        RectTransform rect = (RectTransform)optionsPanel.transform;

        if (isVisible)
        {
            rect.anchoredPosition = new Vector2(Mathf.Abs(rect.anchoredPosition.x), rect.anchoredPosition.y);
        }
        else
        {
            rect.anchoredPosition = new Vector2(-Mathf.Abs(rect.anchoredPosition.x), rect.anchoredPosition.y);
        }
    }

    public void onClickResetCamera()
    {
        origin = Vector3.zero;
        cameraUp = Vector3.up;
        transform.position = new Vector3(0, 0, 20);
        transform.LookAt(origin, cameraUp);

        world.orientLabels(transform.position, origin, cameraUp);

        axisX.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.left);
        axisX.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.left);
        axisY.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.up);
        axisY.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.up);
        axisZ.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.forward);
        axisZ.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.forward);
    }

    public void onToggleLabels(bool b)
    {
        world.showLabels(!b);
    }

    public void onToggleSurfaceTri(bool b)
    {
        world.setSurfaceVisible(!b, 3);
    }

    public void onToggleSurfaceQuad(bool b)
    {
        world.setSurfaceVisible(!b, 4);
    }

    public void onToggleSurfacePenta(bool b)
    {
        world.setSurfaceVisible(!b, 5);
    }

    public void onChangeAction(int button)
    {
        Toggle toggle = controlPanel.transform.GetChild(button).GetComponent<Toggle>();

        if (toggle.isOn)
        {
            option = (TOOL)button;
        }
    }

    // Update is called once per frame
    void Update()
    {

        //movement parameters to apply to updateCamera
        Quaternion rotateDelta;
        Vector3 panDelta;
        float scale;
        Touch touchZero;
        Touch touchOne;

        //check option menu should move
        if (isOptionMoving)
        {
            RectTransform rect = (RectTransform)optionsPanel.transform;
            float currentX = rect.anchoredPosition.x;
            
			if (showOptionsPanel) {
				if ( Mathf.Abs(currentX + optionPanelInitX) < 0.1) {
					isOptionMoving = false;
					setOptionMenu(true);
				}
				else {
					currentX += -optionPanelInitX / 6;
					rect.anchoredPosition = new Vector2( currentX, rect.anchoredPosition.y);
				}
			}
			else {
				if ( Mathf.Abs(currentX - optionPanelInitX) < 0.1){
					isOptionMoving = false;
					setOptionMenu(false);
				}
				else {
					currentX -= -optionPanelInitX / 6;
					rect.anchoredPosition = new Vector2( currentX, rect.anchoredPosition.y);
				}
			}
			
        }

        rotateDelta = Quaternion.identity;
        panDelta = Vector3.zero;
        scale = 1f;
        if (showOptionsPanel)
        {
            float startX = ((float)Screen.width) / 800 * 70;
            activeRect.Set(startX, 0, Screen.width * 0.84f - startX, (float)Screen.height);
        }
        else
        {
            activeRect.Set(0, 0, Screen.width * 0.84f, (float)Screen.height);
        }

        switch (status)
        {

            //no mouse pressed, no process running in background
            case STATE.WAIT:
                switch (Input.touchCount)
                {
                    case 0:
                        if (Input.GetMouseButtonDown(0) && activeRect.Contains(Input.mousePosition))
                        {
                            getObjectClicked(Input.mousePosition);
                            if (objClicked != null)
                            {
                                if (option == TOOL.DELETE)
                                {
                                    addLog(world.deleteBall(objClicked));
                                    status = STATE.AFTER_MOUSE_DELETE;
                                }
                                else
                                {
                                    initDrag();
                                    showShadows(true);
                                    status = STATE.MOUSE_DRAG;
                                }
                                return;
                            }
                            status = STATE.IN_MOUSE_ROTATE;
                            setAxisVisible(true);
                            mouseStart = Input.mousePosition;
                            return;
                        }

                        if (Input.GetAxis("Mouse ScrollWheel") < 0.0f)
                        {
                            scale = zoomSpeed;
                            updateCamera(rotateDelta, panDelta, scale);
                            return;
                        }

                        if (Input.GetAxis("Mouse ScrollWheel") > 0.0f)
                        {
                            scale = 1 / zoomSpeed;
                            updateCamera(rotateDelta, panDelta, scale);
                            return;
                        }

                        if (Input.GetMouseButtonDown(1) && activeRect.Contains(Input.mousePosition))
                        {
                            status = STATE.IN_MOUSE_PAN;
                            mouseStart = Input.mousePosition;
                            setAxisVisible(true);
                        }
                        break;
                    case 1:
                        touchZero = Input.GetTouch(0);
                        if (activeRect.Contains(touchZero.position))
                        {
                            getObjectClicked(touchZero.position);
                            if (objClicked == null)
                            {
                                mouseStart = touchZero.position;
                                prevTouchDistance = 0;
                                setAxisVisible(true);
                                Debug.Log("WAIT -> IN_ROTATE");
                                status = STATE.IN_TOUCH_ROTATE;
                            }
                            else
                            {
                                if (option == TOOL.DELETE)
                                {
                                    addLog(world.deleteBall(objClicked));
                                    status = STATE.AFTER_TOUCH_DELETE;
                                }
                                else
                                {
                                    Debug.Log("WAIT -> DRAG");
                                    initDrag();
                                    showShadows(true);
                                    status = STATE.TOUCH_DRAG;
                                }
                            }
                        }
                        break;
                    case 2:
                        // Store both touches.
                        touchZero = Input.GetTouch(0);
                        touchOne = Input.GetTouch(1);
                        Vector2 touchDifference = touchZero.position - touchOne.position;

                        prevTouchDistance = touchDifference.magnitude;
                        mouseStart.x = touchZero.position.x + touchDifference.x / 2;
                        mouseStart.y = touchZero.position.y + touchDifference.y / 2;
                        setAxisVisible(true);
                        Debug.Log("WAIT -> IN_ZOOM");
                        status = STATE.IN_TOUCH_ZOOM;
                        break;
                    default:
                        Debug.Log("WAIT -> AFTER_ZOOM");
                        status = STATE.AFTER_TOUCH_ZOOM;
                        break;
                }
                break;
            case STATE.IN_TOUCH_ROTATE:
                switch (Input.touchCount)
                {
                    case 0:
                        setAxisVisible(false);
                        Debug.Log("IN_ROTATE -> WAIT");
                        status = STATE.WAIT;
                        break;
                    case 1:
                        touchZero = Input.GetTouch(0);
                        mouseEnd = touchZero.position;
                        rotateDelta = setupRotation();
                        updateCamera(rotateDelta, panDelta, scale);
                        mouseStart = mouseEnd;
                        prevTouchDistance = 0;
                        break;
                    case 2:
                        // Store both touches.
                        touchZero = Input.GetTouch(0);
                        touchOne = Input.GetTouch(1);
                        Vector2 touchDifference = touchZero.position - touchOne.position;

                        prevTouchDistance = touchDifference.magnitude;
                        mouseStart.x = touchZero.position.x + touchDifference.x / 2;
                        mouseStart.y = touchZero.position.y + touchDifference.y / 2;
                        Debug.Log("IN_ROTATE -> IN_ZOOM");
                        status = STATE.IN_TOUCH_ZOOM;
                        break;
                    default:
                        status = STATE.AFTER_TOUCH_ZOOM;
                        break;
                }
                break;
            case STATE.IN_TOUCH_ZOOM:
                switch (Input.touchCount)
                {
                    case 0:
                        setAxisVisible(false);
                        Debug.Log("IN_ZOOM -> WAIT");
                        status = STATE.WAIT;
                        break;
                    case 1:
                        status = STATE.AFTER_TOUCH_ZOOM;
                        Debug.Log("IN_ZOOM -> AFTER_ZOOM");
                        break;
                    case 2:
                        // Store both touches.
                        touchZero = Input.GetTouch(0);
                        touchOne = Input.GetTouch(1);
                        Vector2 touchDifference = touchZero.position - touchOne.position;

                        mouseEnd.x = touchZero.position.x + touchDifference.x / 2;
                        mouseEnd.y = touchZero.position.y + touchDifference.y / 2;
                        scale = prevTouchDistance / touchDifference.magnitude;
                        panDelta = setupPan();
                        updateCamera(rotateDelta, panDelta, scale);
                        prevTouchDistance = touchDifference.magnitude;
                        mouseStart = mouseEnd;
                        break;
                    default:
                        status = STATE.AFTER_TOUCH_ZOOM;
                        break;
                }
                break;
            case STATE.AFTER_TOUCH_ZOOM:
                if (Input.touchCount == 0)
                {
                    setAxisVisible(false);
                    Debug.Log("AFTER_ZOOM -> WAIT");
                    status = STATE.WAIT;
                }
                break;

            //we made the action (delete or something) now the mouse is down and we wait
            case STATE.AFTER_TOUCH_DELETE:
                if (Input.touchCount == 0)
                {
                    objClicked = null;
                    status = STATE.WAIT;
                }
                break;
            //mouse is down, calculation is done, now we do drag
            case STATE.TOUCH_DRAG:
                if (Input.touchCount == 0)
                {
                    endDrag();
                    objClicked = null;
                    status = STATE.WAIT;
                    Debug.Log("DRAG -> WAIT");
                }
                else
                    doDrag();
                break;
            case STATE.IN_MOUSE_ROTATE:
                if (Input.GetMouseButtonUp(0))
                {
                    status = STATE.WAIT;
                    setAxisVisible(false);
                    return;
                }

                mouseEnd = Input.mousePosition;
                rotateDelta = setupRotation();
                mouseStart = mouseEnd;
                updateCamera(rotateDelta, panDelta, scale);
                break;
            case STATE.IN_MOUSE_PAN:
                if (Input.GetMouseButtonUp(1))
                {
                    status = STATE.WAIT;
                    setAxisVisible(false);
                    return;
                }

                mouseEnd = Input.mousePosition;
                panDelta = setupPan();
                mouseStart = mouseEnd;
                updateCamera(rotateDelta, panDelta, scale);
                break;
            //we made the action (delete or something) now the mouse is down and we wait
            case STATE.AFTER_MOUSE_DELETE:
                if (Input.GetMouseButtonUp(0))
                {
                    objClicked = null;
                    status = STATE.WAIT;
                }
                break;
            //mouse is down, calculation is done, now we do drag
            case STATE.MOUSE_DRAG:
                if (Input.GetMouseButtonUp(0))
                {
                    endDrag();
                    objClicked = null;
                    status = STATE.WAIT;
                }
                else
                    doDrag();
                break;
        }

    }

    private void initDrag()
    {
        newBall = world.createBall(ballIndex++);
        newBall.SetActive(false);
        newStrut = world.createStrut((int)option);
        newStrut.SetActive(false);
        buildShadows();
    }

    private void addLog(string s)
    {
        if (s == "DELETE_LAST")
            return;
        logEntries.Add(s);
    }

    private void getObjectClicked(Vector3 position)
    {
        Ray ray = this.GetComponent<Camera>().ScreenPointToRay(position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10000, 1 << 8))
            objClicked = hit.transform.gameObject;
        else
            objClicked = null;
    }

    private void setAxisVisible(bool visible)
    {
        axisX.SetActive(visible);
        axisY.SetActive(visible);
        axisZ.SetActive(visible);
    }

    private void doDrag()
    {
        int i;
        float minDistance = 500000, distance;
        Vector3 screenPos, mousePos = Input.mousePosition;

        addStatus = ADDSTATE.INVALID;
        currPosition = -1;
        targetBall = null;
        mousePos.z = 0;

        if (!activeRect.Contains(mousePos))
            return;

        //check minimum distance for all shadows
        for (i = 0; i < shadowNum; i++)
        {
            screenPos = this.GetComponent<Camera>().WorldToScreenPoint(shadows[i].ball.transform.position);
            screenPos.z = 0;

            distance = Vector3.Distance(screenPos, mousePos);
            if (distance < minDistance)
            {
                currPosition = shadows[i].position;
                minDistance = distance;
                addStatus = ADDSTATE.VALID_BALL;
            }
        }

        for (i = 0; i < highlighted.Count; i++)
        {
            screenPos = this.GetComponent<Camera>().WorldToScreenPoint(highlighted[i].ball.transform.position);
            screenPos.z = 0;

            distance = Vector3.Distance(screenPos, mousePos);
            if (distance < minDistance)
            {
                currPosition = highlighted[i].position;
                minDistance = distance;
                targetBall = highlighted[i].ball;
                addStatus = ADDSTATE.VALID_STRUT;
            }
        }

        switch (addStatus)
        {
            case ADDSTATE.VALID_STRUT:
                world.positionBall(objClicked, newBall, (int)option, currPosition);
                world.positionStrut(objClicked, newStrut, (int)option, currPosition);
                newBall.SetActive(false);
                newStrut.SetActive(true);
                break;
            case ADDSTATE.VALID_BALL:
                world.positionBall(objClicked, newBall, (int)option, currPosition);
                world.positionStrut(objClicked, newStrut, (int)option, currPosition);
                newBall.SetActive(true);
                newStrut.SetActive(true);
                break;
            case ADDSTATE.INVALID:
                newBall.SetActive(false);
                newStrut.SetActive(false);
                break;
        }
    }

    private void endDrag()
    {
        switch (addStatus)
        {
            case ADDSTATE.VALID_STRUT:
                Destroy(newBall);
                addLog(world.addStrutToModel(targetBall, newStrut, objClicked, currPosition, (int)option));
                break;
            case ADDSTATE.VALID_BALL:
                newBall.GetComponent<BallData>().orientLabel(transform.position, origin, cameraUp);
                addLog(world.addBallToModel(newBall, newStrut, objClicked, currPosition, (int)option));
                break;
            case ADDSTATE.INVALID:
                Destroy(newBall);
                Destroy(newStrut);
                break;
        }
        showShadows(false);
    }

    private Vector3 setupPan()
    {
        Vector3 oldScreenPos, oldWorldPos, currScreenPos, currWorldPos, toTarget;

        toTarget = this.transform.position - origin;

        oldScreenPos = new Vector3(mouseStart.x, mouseStart.y, toTarget.magnitude);
        oldWorldPos = this.GetComponent<Camera>().ScreenToWorldPoint(oldScreenPos);
        currScreenPos = new Vector3(mouseEnd.x, mouseEnd.y, toTarget.magnitude);
        currWorldPos = this.GetComponent<Camera>().ScreenToWorldPoint(currScreenPos);

        return (oldWorldPos - currWorldPos);
    }

    private Quaternion setupRotation()
    {
        if (mouseStart.x == mouseEnd.x && mouseStart.y == mouseEnd.y)
        {
            return Quaternion.identity;
        }

        Vector3 near = new Vector3(0, 0, GetComponent<Camera>().nearClipPlane);
        Vector3 p1 = Camera.main.ScreenToWorldPoint(mouseStart + near);
        Vector3 p2 = Camera.main.ScreenToWorldPoint(mouseEnd + near);

        p1 = p1 - origin;
        p2 = p2 - origin;
        Vector3 axisOfRotation = Vector3.Cross(p2, p1);
        //float angle = Mathf.Asin( axisOfRotation.magnitude / p1.magnitude / p2.magnitude ) / Mathf.PI * 360 * rotateSpeed;
        //float angle = 10;
        float angle = Vector3.Distance(mouseStart, mouseEnd) * 360 / Screen.height;
        axisOfRotation.Normalize();
        return Quaternion.AngleAxis(angle, axisOfRotation);
    }

    void updateCamera(Quaternion rotateDelta, Vector3 panDelta, float scale)
    {
        float radius;
        Vector3 offset;

        offset = transform.position - origin;
        radius = offset.magnitude * scale;
        radius = Mathf.Max(minDistance, Mathf.Min(maxDistance, radius));

        offset = offset.normalized * radius;

        //move target to panned location
        origin = origin + panDelta;

        if (panDelta != Vector3.zero)
        {
            axisX.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.left);
            axisX.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.left);
            axisY.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.up);
            axisY.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.up);
            axisZ.GetComponent<LineRenderer>().SetPosition(0, origin - AXISSIZE * Vector3.forward);
            axisZ.GetComponent<LineRenderer>().SetPosition(1, origin + AXISSIZE * Vector3.forward);
        }

        transform.position = rotateDelta * offset + origin;
        cameraUp = rotateDelta * cameraUp;
        transform.LookAt(origin, cameraUp);

        world.orientLabels(transform.position, origin, cameraUp);
    }

    private void buildShadows()
    {
        int i, j;
        int startIndex = 0, endIndex = 0, strutLen = 0;
        Vector3 cameraVector, virtualPos;
        float dotProduct;
        bool posPossible;
        ShadowData shadowData;
        PositionPhi pos;

        cameraVector = this.transform.position - objClicked.transform.position;
        cameraVector.Normalize();

        switch (option)
        {
            case TOOL.BLUE_SHORT:
                startIndex = 0;
                endIndex = 30;
                strutLen = 0;
                break;
            case TOOL.BLUE_MED:
                startIndex = 0;
                endIndex = 30;
                strutLen = 1;
                break;
            case TOOL.BLUE_LONG:
                startIndex = 0;
                endIndex = 30;
                strutLen = 2;
                break;
            case TOOL.YELLOW_SHORT:
                startIndex = 30;
                endIndex = 50;
                strutLen = 0;
                break;
            case TOOL.YELLOW_MED:
                startIndex = 30;
                endIndex = 50;
                strutLen = 1;
                break;
            case TOOL.YELLOW_LONG:
                startIndex = 30;
                endIndex = 50;
                strutLen = 2;
                break;
            case TOOL.RED_SHORT:
                startIndex = 50;
                endIndex = 62;
                strutLen = 0;
                break;
            case TOOL.RED_MED:
                startIndex = 50;
                endIndex = 62;
                strutLen = 1;
                break;
            case TOOL.RED_LONG:
                startIndex = 50;
                endIndex = 62;
                strutLen = 2;
                break;
        }

        //get a list of all nearby objects
        Collider[] nearbyColliders = Physics.OverlapSphere(objClicked.transform.position, GeometryData.strutLen[(int)option]);

        shadowNum = 0;
        highlighted.Clear();

        //start checking all possible links of the current type from current ball
        for (i = startIndex; i < endIndex; i++)
        {

            virtualPos = GeometryData.outPoints[i];
            dotProduct = Vector3.Dot(virtualPos, cameraVector);

            //check to see if the link is not already taken
            posPossible = true;
            for (j = 0; j < objClicked.GetComponent<BallData>().connections.Count; j++)
            {
                if (objClicked.GetComponent<BallData>().connections[j].position == i)
                {
                    posPossible = false;
                    break;
                }
            }

            if (posPossible)
            {

                //check to see if there is a ball already there
                pos = objClicked.GetComponent<BallData>().getPositionFrom(i, strutLen);
                virtualPos = pos.getFloatPos();
                int existingBall = -1;

                for (j = 0; j < nearbyColliders.Length; j++)
                {
                    if (nearbyColliders[j].gameObject.tag == "Ball" &&
                        Equals(nearbyColliders[j].gameObject.GetComponent<BallData>().position, pos))
                    {
                        existingBall = j;
                        break;
                    }
                }

                if (existingBall != -1)
                {

                    //check if the strut does not intersect with anything else
                    RaycastHit hitInfo;
                    Vector3 startPos = objClicked.transform.position;
                    Physics.Linecast(startPos, virtualPos, out hitInfo);

                    if (hitInfo.collider.gameObject != nearbyColliders[existingBall].gameObject)
                    {
                        Debug.Log("New strut intersects :" + hitInfo.collider.gameObject.name);
                    }
                    else
                    {
                        shadowData.ball = nearbyColliders[existingBall].gameObject;
                        shadowData.position = i;
                        highlighted.Add(shadowData);
                    }
                }
                else
                {
                    //create shadow only if the position is in front
                    if (dotProduct > -0.01)
                    {

                        //check if the ball and strut does not intersect with anything else
                        RaycastHit hitInfo;
                        Vector3 startPos = objClicked.transform.position;
                        bool intersects;


                        intersects = Physics.Linecast(startPos, virtualPos, out hitInfo);
                        if (intersects)
                        {
                            Debug.Log("New strut/ball intersects :" + hitInfo.collider.gameObject.name);
                        }
                        else
                        {
                            intersects = Physics.CheckSphere(virtualPos, 1);
                            if (!intersects)
                            {
                                shadows[shadowNum].position = i;
                                shadows[shadowNum].ball.transform.position = virtualPos;
                                shadowNum++;
                            }
                            else
                            {
                                Debug.Log("New ball intersects something!");
                            }
                        }
                    }
                }
            }
        }
    }

    private void showShadows(bool show)
    {
        int i;

        for (i = 0; i < shadowNum; i++)
            shadows[i].ball.SetActive(show);

        for (i = 0; i < highlighted.Count; i++)
        {
            highlighted[i].ball.GetComponent<Renderer>().material = show ? shadowMaterial : ballMaterial;
        }
    }

}
