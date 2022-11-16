using System;
using System.Collections;
using System.Collections.Generic;
using CodingConnected.TraCI.NET;
using CodingConnected.TraCI.NET.Types;
using UnityEngine;

using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CodingConnected.TraCI.NET.Commands;
using Color = UnityEngine.Color;
using Object = System.Object;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Xml;
using PathCreation;
using PathCreation.Examples;
using System.Threading.Tasks;
using Unity.Jobs;
using System.Runtime.InteropServices;

public class SumoSimulation : MonoBehaviour
{
    public static SumoSimulation inst;
    public UISystem uISystem;

    private const float vehHeight = 0.85f;
    private const float laneWidth = 3.3f;

    private float frameTimeG = 0.10f;
    private float frameTimeS = 10f;
    public bool maxFps = true;
    private Stopwatch sw;
    
    public List<GameObject> carlist;
    public GameObject NPCVehicle;
    public TraCIClient client;
    private List<string> tlightids;
    public Dictionary<string, List<traLights>> tlightMap;
    private bool connected = false;

    private SumoVehicle selectedCar;
    private SumoCStation selectedcStation;
    public GameObject selectObject;

    public string sumoSimPath;
    private bool isSimRunning = false;
    private bool isRunningStep = false;
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject cStaPrefab;
    public TextAsset defAdditional;
    public SumoOptions simOptions;

    Transform nodeObject;
    Transform edgeObject;
    Transform cstatObject;
    Transform vehObject;

    public Dictionary<String, Transform> nodeMap = new Dictionary<string, Transform>();
    public Dictionary<String, SumoEdge> edgeMap = new Dictionary<string, SumoEdge>();
    public Dictionary<string,SumoCStation> RoadIDtoSumoCSMap = new Dictionary<string,SumoCStation>();
    public Dictionary<string,SumoCStation> CSIDtoSumoCSMap = new Dictionary<string,SumoCStation>();

    // Start is called before the first frame update
    void Start()
    {
        //singleton
        if (inst != null) Debug.LogWarning("Something is wrong, SumoSimulation.inst was not null");
        inst = this;

        //get simulation path
        sumoSimPath = MainMenu.selectedSimulationPath ?? sumoSimPath;
        if (!sumoSimPath.EndsWith("\\")) sumoSimPath = sumoSimPath + "\\";

        //parent objects
        nodeObject = new GameObject().transform;
        nodeObject.name = "Nodes";
        edgeObject = new GameObject().transform;
        edgeObject.name = "Edges";
        vehObject = new GameObject().transform;
        vehObject.name = "Vehicles";
        cstatObject = new GameObject().transform;
        cstatObject.name = "Charging Stations";
        selectObject.SetActive(false);

        //preparation
        LoadSumoOptions();
        StartCoroutine( PrepareMap());
    }

    private IEnumerator PrepareMap()
    {
        sw = Stopwatch.StartNew();
        uISystem.SetLoading();
        float totalTasks = 6f;
        uISystem.LoadingProgress(1f / totalTasks, "Using Netconvert");
        yield return StartCoroutine(NetToPlain());
        uISystem.LoadingProgress(2f / totalTasks, "Adding Nodes");
        yield return StartCoroutine(AddNodes());
        uISystem.LoadingProgress(3f / totalTasks, "Adding Edges");
        yield return StartCoroutine(AddEdges());
        uISystem.LoadingProgress(4f / totalTasks, "Adding Charging Stations");
        yield return StartCoroutine(AddAdditonal());
        uISystem.LoadingProgress(5f / totalTasks, "Finishing Map Generation");
        yield return StartCoroutine(UpdateAllEdges());
        uISystem.LoadingProgress(6f / totalTasks, "Done");
        yield return new WaitForSeconds(0.2f);
        uISystem.FinishLoading();
        sw.Stop();
    }

    private void LoadSumoOptions()
    {
        string optionPath = sumoSimPath + "map.options.json";
        if (File.Exists(optionPath))
        {
            simOptions = SumoOptions.Load(optionPath);
        }
        else
        {
            simOptions = new SumoOptions();
            SumoOptions.Save(simOptions, optionPath);
        }
    }

    public void SaveSumoOptions()
    {
        string optionPath = sumoSimPath + "map.options.json";
        SumoOptions.Save(simOptions, optionPath);
    }

    IEnumerator NetToPlain()
    {
        Process proc = new Process();
        try
        {
            if (File.Exists(sumoSimPath + "map.con.xml")) File.Delete(sumoSimPath + "map.con.xml");
            Debug.Log("Using Netconvert");
            string sumoNetPath = InQuotes(sumoSimPath + "map.net.xml");
            string closeOnExit = "/k ";
            if (!simOptions.waitForUserToClose || simOptions.hideNetconvert) closeOnExit = "/c ";
            proc.StartInfo.FileName = @"cmd.exe";
            proc.StartInfo.Arguments = $@"{closeOnExit}netconvert -s {sumoNetPath} --plain-output-prefix map -o map.net.xml";
            proc.StartInfo.WorkingDirectory = sumoSimPath;
            if (simOptions.hideNetconvert)
            {
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            proc.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Netconvert Failed: " + e.ToString());
        }
        while (!proc.HasExited) {
            yield return null;
        }
        proc.WaitForExit();
        if (File.Exists(sumoSimPath + "map.con.xml")) Debug.Log("Netconvert was successful");
        else Debug.LogWarning("Netconvert was not successful");
    }

    public void OpenSumoEditor()
    {
        string sumoNetPath = InQuotes(sumoSimPath + "map.net.xml");
        string sumoAddPath = InQuotes(sumoSimPath + "map.add.xml");
        Process.Start("netedit", $" -s {sumoNetPath} -a {sumoAddPath}");
    }

    IEnumerator AddNodes()
    {
        XmlDocument net = new XmlDocument();
        net.Load(sumoSimPath+"map.nod.xml");
        XmlAttribute xatt, yatt, id;
        Vector3 locationVector;
        nodeMap.Clear();
        XmlNode firstnode = net.SelectSingleNode("nodes/node");
        SetCamera(firstnode);
        foreach (XmlNode node in net.SelectNodes("nodes/node"))
        {
            //prevent freeze screen
            if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
            {
                yield return null;
                sw.Restart();
            }
            xatt = node.Attributes["x"];
            yatt = node.Attributes["y"];
            id = node.Attributes["id"];
            if (xatt != null && yatt != null)
            {
                locationVector = new Vector3(float.Parse(xatt.Value), 0, float.Parse(yatt.Value));
                GameObject n = Instantiate(nodePrefab, locationVector, Quaternion.identity,nodeObject);
                if (id != null)
                {
                    n.name = id.Value;
                    nodeMap.Add(id.Value,n.transform);
                }
            }
            else
            {
                if (id!=null) Debug.LogWarning("Node was not added:" + id.Value);
                else Debug.LogWarning("Node was not added: null node");
            }
        }
    }

    private void SetCamera(XmlNode node)
    {
        XmlAttribute xatt = node.Attributes["x"];
        XmlAttribute yatt = node.Attributes["y"];
        if (xatt != null && yatt != null)
        {
            Camera.main.transform.position = new Vector3(float.Parse(xatt.Value), 150, float.Parse(yatt.Value));
        }
    }

    IEnumerator AddEdges()
    {
        XmlDocument netedge = new XmlDocument();
        netedge.Load(sumoSimPath + "map.edg.xml");
        XmlAttribute from, to, id, numLanes, shape, spreadType;
        edgeMap.Clear();
        XmlNodeList[] xmlNodeLists = { netedge.SelectNodes("edges/edge") /*, netconnections.SelectNodes("connections/connection")*/ };
        foreach (XmlNodeList xlist in xmlNodeLists)
        {
            foreach (XmlNode node in xlist)
            {
                //prevent freeze screen
                if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
                {
                    yield return null;
                    sw.Restart();
                }
                from = node.Attributes["from"];
                to = node.Attributes["to"];
                id = node.Attributes["id"];
                numLanes = node.Attributes["numLanes"];
                shape = node.Attributes["shape"];
                spreadType = node.Attributes["spreadType"];

                GameObject n = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, edgeObject);
                PathCreator road = n.GetComponent<PathCreator>();
                if (id != null)
                {
                    n.name = id.Value;
                }
                if (shape != null)
                {
                    //get nodes from nodemap
                    Transform fromObj, toObj;
                    nodeMap.TryGetValue(from.Value, out fromObj);
                    nodeMap.TryGetValue(to.Value, out toObj);
                    string[] shapeStringPairs = shape.Value.Split(' ');
                    List<Vector3> points = new List<Vector3>();
                    for (int i = 0; i < shapeStringPairs.Length; i++)
                    {
                        float x = float.Parse(shapeStringPairs[i].Split(',')[0]);
                        float y = float.Parse(shapeStringPairs[i].Split(',')[1]);

                        points.Add(new Vector3(x, 0, y));
                    }
                    BezierPath path = new BezierPath(points, false, PathSpace.xz);
                    road.bezierPath = path;
                    //set sumoedge values
                    SumoEdge sumoedge = road.GetComponent<SumoEdge>();
                    if (sumoedge != null)
                    {
                        string stypestr = (spreadType != null) ? spreadType.Value : "right";
                        sumoedge.SetValues(id.Value, fromObj, toObj, numLanes.Value, stypestr, points, road);
                        edgeMap.Add(id.Value, sumoedge);
                    }
                }
                else if (from != null && to != null)
                {
                    //get nodes from nodemap
                    Transform fromObj, toObj;
                    nodeMap.TryGetValue(from.Value, out fromObj);
                    nodeMap.TryGetValue(to.Value, out toObj);
                    if (toObj != null && fromObj != null)
                    {
                        Transform[] points = { fromObj, toObj };
                        BezierPath path = new BezierPath(points, false, PathSpace.xz);
                        road.bezierPath = path;
                        road.bezierPath.IsClosed = true;
                        road.bezierPath.IsClosed = false;
                    }
                    //set sumoedge values
                    SumoEdge sumoedge = road.GetComponent<SumoEdge>();
                    if (sumoedge != null)
                    {
                        string typestr = (spreadType != null) ? spreadType.Value : "none";
                        sumoedge.SetValues(id.Value, fromObj, toObj, numLanes.Value, typestr, null, road);
                        edgeMap.Add(id.Value, sumoedge);
                    }
                }
                else
                {
                    if (id != null) Debug.LogWarning("Edge was not added:" + id.Value);
                    else Debug.LogWarning("Edge was not added: null edge");
                }
            }
        }
    }

    IEnumerator UpdateAllEdges()
    {
        foreach (var p in edgeMap)
        {
            //prevent freeze screen
            if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
            {
                yield return null;
                sw.Restart();
            }
            p.Value.roadSpline.bezierPath.IsClosed = true;
            p.Value.roadSpline.bezierPath.IsClosed = false;
            RoadMeshCreator roadmesh = p.Value.roadMesh;
            roadmesh.SetWidth(laneWidth * p.Value.numLanes, p.Value.IsOneSided());
            p.Value.roadSpline.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
            //p.Value.roadSpline.bezierPath.sp
            p.Value.roadMesh.Updated();
        }
        foreach (var p in RoadIDtoSumoCSMap)
        {
            //prevent freeze screen
            if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
            {
                yield return null;
                sw.Restart();
            }
            p.Value.sumoEdge.roadSpline.bezierPath.IsClosed = true;
            p.Value.sumoEdge.roadSpline.bezierPath.IsClosed = false;
            RoadMeshCreator roadmesh = p.Value.roadMesh;
            roadmesh.SetWidth(laneWidth * p.Value.sumoEdge.numLanes, p.Value.sumoEdge.IsOneSided());
            p.Value.roadSpline.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
            p.Value.roadMesh.Updated();
        }

    }

    IEnumerator AddAdditonal()
    {
        XmlDocument add = new XmlDocument();
        if (!File.Exists(sumoSimPath + "map.add.xml")) {
            Debug.Log("Additional File not found for : " + sumoSimPath);
            yield break;
        }
        else
        {
            add.Load(sumoSimPath + "map.add.xml");
            RoadIDtoSumoCSMap.Clear();
            CSIDtoSumoCSMap.Clear();
            XmlAttribute cstationId, laneId, startPos, endPos;
            List<string> roadsFound = new List<string>();
            foreach (XmlNode node in add.SelectNodes("additional/chargingStation"))
            {
                //prevent freeze screen
                if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
                {
                    yield return null;
                    sw.Restart();
                }
                //get attributes
                cstationId = node.Attributes["id"];
                laneId = node.Attributes["lane"];
                startPos = node.Attributes["startPos"];
                endPos = node.Attributes["endPos"];

                string roadId = laneId.Value.Remove(laneId.Value.LastIndexOf("_"));
                SumoEdge se;
                edgeMap.TryGetValue(roadId, out se);

                if (cstationId != null && !RoadIDtoSumoCSMap.ContainsKey(roadId))
                {
                    if (se != null && se.roadSpline != null)
                    {
                        GameObject cstation = Instantiate(cStaPrefab, Vector3.zero, Quaternion.identity, cstatObject);
                        SumoCStation cStat = cstation.GetComponent<SumoCStation>();
                        cStat.SetValues(se, cstation.GetComponent<PathCreator>(), cstation.GetComponent<RoadMeshCreator>());
                        cStat.roadSpline.bezierPath = se.roadSpline.bezierPath;
                        cStat.roadSpline.bezierPath.IsClosed = true;
                        cStat.roadSpline.bezierPath.IsClosed = false;
                        RoadIDtoSumoCSMap.Add(roadId,cStat);
                        CSIDtoSumoCSMap.Add(cstationId.Value, cStat);
                        cstation.name = roadId;
                        cStat.AddStationID(cstationId.Value);
                    }
                }
                else if (RoadIDtoSumoCSMap.ContainsKey(roadId))
                {
                    SumoCStation cStat;
                    RoadIDtoSumoCSMap.TryGetValue(roadId, out cStat);
                    if (cStat!=null)
                    {
                        CSIDtoSumoCSMap.Add(cstationId.Value, cStat);
                        cStat.AddStationID(cstationId.Value);
                    }
                    else
                    {
                        Debug.LogWarning("Something went wrong while adding Cstations");
                    }
                }
                else
                {
                    Debug.LogWarning("Something went wrong while adding Cstations");
                }
            }
        }
    }

    void RemoveNodeChildren()
    {
        foreach (Transform child in nodeObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    
    void RemoveEdgeChildren()
    {
        foreach (Transform child in edgeObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (Transform child in cstatObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public void RefreshMap()
    {
        StartCoroutine(RefreshMapRoutine());
    }

    IEnumerator RefreshMapRoutine()
    {
        Debug.Log("Starting Refresh");
        sw = Stopwatch.StartNew();
        float totalTasks = 8f;

        uISystem.SetLoading();
        if (simOptions.netConvertRefresh)
        {
            uISystem.LoadingProgress(1f / totalTasks, "Clearing Simulation");
            RemoveNodeChildren();
            RemoveEdgeChildren();
            yield return new WaitForSeconds(0.1f);
            uISystem.LoadingProgress(1f / totalTasks, "Using Netconvert");
            yield return StartCoroutine(NetToPlain());
            uISystem.LoadingProgress(2f / totalTasks, "Adding Nodes");
            yield return StartCoroutine(AddNodes());
            uISystem.LoadingProgress(3f / totalTasks, "Adding Edges");
            yield return StartCoroutine(AddEdges());
            uISystem.LoadingProgress(4f / totalTasks, "Adding Charging Stations");
            yield return StartCoroutine(AddAdditonal());
            uISystem.LoadingProgress(5f / totalTasks, "Finishing Map Generation");
            yield return StartCoroutine(UpdateAllEdges());
        }
        if (simOptions.randomTripsRefresh)
        {
            uISystem.LoadingProgress(6f / totalTasks, "Using RandomTrips");
            yield return StartCoroutine(RandomTrips());
            uISystem.LoadingProgress(7f / totalTasks, "Finishing Route Generation");
            yield return StartCoroutine(AddStopsToRoutes());
        }
        if (simOptions.csSetValues) SetCStationValues();
        uISystem.LoadingProgress(7f / totalTasks, "Done");
        yield return new WaitForSeconds(0.2f);
        uISystem.FinishLoading();
        sw.Stop();
        Debug.Log("Finished Refresh");
    }

    IEnumerator RandomTrips()
    {

        Process proc = new Process();
        try
        {
            //cmd: python "%SUMO_HOME%/tools/randomTrips.py" -v -n map.net.xml -a map.add.xml -r map.rou.xml --trip-attributes="type=\"ev\"" -o map.trips.xml --validate
            //set sumocfg routes
            CorrectSumocfgRoutename();
            //add vType ev to add if it doesnt have it
            XmlDocument net = new XmlDocument();
            net.Load(sumoSimPath + "map.add.xml");
            XmlNode vTypeNode = net.SelectSingleNode("additional/vType[@id='ev']");
            if (vTypeNode == null)
            {
                //add the vType node
                XmlDocument def = new XmlDocument();
                Debug.Log(defAdditional.ToString());
                def.LoadXml(defAdditional.ToString());
                XmlNode vTypeElem = def.SelectSingleNode("additional/vType[@id='ev']");
                if (vTypeElem == null) Debug.LogError("Default vType not found");
                //else Debug.Log(vTypeElem.Name);
                net.DocumentElement.AppendChild(net.ImportNode(vTypeElem, true));
                net.Save(sumoSimPath + "map.add.xml");
                if (net.SelectSingleNode("additional/vType[@id='ev']") == null) throw new Exception("vtype was not inserted");
            }
            //execute randomtrips python
            Debug.Log("Using RandomTrips");
            string sumoNetPath = InQuotes(sumoSimPath + "map.net.xml");
            string sumoAddPath = InQuotes(sumoSimPath + "map.add.xml");
            string closeOnExit = "/k ";
            if (!simOptions.waitForUserToClose || simOptions.hideRandomTrips) closeOnExit = "/c ";
            proc.StartInfo.FileName = @"cmd.exe";
            proc.StartInfo.Arguments = $@"{closeOnExit}python ""%SUMO_HOME%/tools/randomTrips.py"" -v -n {sumoNetPath} -a {sumoAddPath} -o map.trips.xml --trip-attributes=""type=\""ev\"""" --validate --fringe-factor {simOptions.rTripsFringe} --period {simOptions.rTripsPeriod}";
            if (simOptions.hideRandomTrips)
            {
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            proc.StartInfo.WorkingDirectory = sumoSimPath;
            proc.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("RandomTrips Failed: " + e.ToString());
        }
        while (!proc.HasExited)
        {
            yield return null;
        }
        proc.WaitForExit();
        //remove vtype from rout file
        XmlDocument netr = new XmlDocument();
        netr.Load(sumoSimPath + "map.trips.xml");
        XmlNode oldChild = netr.SelectSingleNode("routes/vType");
        if (oldChild != null)
        {
            oldChild.ParentNode.RemoveChild(oldChild);
            netr.Save(sumoSimPath + "map.trips.xml");
        }
    }

    private void CorrectSumocfgRoutename()
    {
        XmlDocument cfg = new XmlDocument();
        cfg.Load(sumoSimPath + "map.sumocfg");
        XmlElement cfgrout = cfg.SelectSingleNode("configuration/input/route-files") as XmlElement;
        cfgrout.SetAttribute("value", "map.trips.xml");
        cfg.Save(sumoSimPath + "map.sumocfg");
    }

    IEnumerator AddStopsToRoutes()
    {
        if (!simOptions.csStopsEnabled || RoadIDtoSumoCSMap.Count <= 0) { yield break; }
        sw = Stopwatch.StartNew();
        XmlDocument doc = new XmlDocument();
        doc.Load(sumoSimPath + "map.trips.xml");
        XmlNodeList routes = doc.SelectNodes("routes/trip");
        IEnumerator<string> csEnum = UniqueRandomCstation();

        foreach (XmlElement rnode in routes)
        {
            //prevent freeze screen
            if (maxFps && sw.ElapsedMilliseconds > frameTimeG)
            {
                yield return null;
                sw.Restart();
            }
            //conditions for adding a stop
            float prob = UnityEngine.Random.Range(0f, 1f);
            if (prob <= simOptions.csCarsProbability)
            {
                //add stop if possible
                try
                {
                    //List<string> edges = new List<string>(rnode.SelectSingleNode("route").Attributes["edges"].Value.Split(' '));
                    csEnum.MoveNext();
                    string randStation = csEnum.Current;
                    SumoCStation se;
                    CSIDtoSumoCSMap.TryGetValue(randStation, out se);
                    //add via route
                    rnode.SetAttribute("via", se.sumoEdge.id);
                    //add stop node
                    XmlElement stopNode = doc.CreateElement("stop");
                    //select random cStation
                    stopNode.SetAttribute("chargingStation", randStation);
                    stopNode.SetAttribute("friendlyPos", "true");
                    //stop at cStation for duration
                    stopNode.SetAttribute("duration", UnityEngine.Random.Range(simOptions.csStopDurationMin, simOptions.csStopDurationMax).ToString());
                    //append to xml
                    rnode.AppendChild(stopNode);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error adding stops to route: "+e);
                }
            }
        }
        doc.Save(sumoSimPath + "map.trips.xml");
    }

    public IEnumerator<string> UniqueRandomCstation()
    {
        System.Random rand = new System.Random();
        LinkedList<string> allvalues = new LinkedList<string>(from v in CSIDtoSumoCSMap.Keys orderby rand.Next() select v);
        LinkedList<string> values = new LinkedList<string>(allvalues);
        while (true)
        {
            if (values.Count == 0) values = new LinkedList<string>(allvalues);
            yield return values.Last.Value;
            values.RemoveLast();
        }
    }

    void SetCStationValues()
    {
        XmlDocument add = new XmlDocument();
        if (!simOptions.csStopsEnabled || !File.Exists(sumoSimPath + "map.add.xml"))
        {
            return;
        }
        else
        {
            add.Load(sumoSimPath + "map.add.xml");
            XmlNodeList nodes = add.SelectNodes("additional/chargingStation");
            foreach (XmlElement node in nodes)
            {
                node.SetAttribute("power", simOptions.csPowerValue.ToString("F2"));
                node.SetAttribute("efficiency", simOptions.csEfficiency.ToString("F2"));
            }
            add.Save(sumoSimPath + "map.add.xml");
        }
    }

    public void BeginSumo()
    {
        if (connected == true) {
            TerminateSumo();
        }

        string sumoCfgPath = InQuotes(sumoSimPath + "map.sumocfg");
        //start sumo process
        Process sumoProcess = new Process();
        if (simOptions.hideSumoWindow)
        {
            sumoProcess.StartInfo.UseShellExecute = true;
            sumoProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            sumoProcess.StartInfo.FileName = @"sumo";
        }
        else
        {
            sumoProcess.StartInfo.FileName = @"sumo-gui";
        }
        string lateralVar = $"--lateral-resolution {simOptions.lateralResolution}";
        if (!simOptions.useSublaneModel) lateralVar = "--lanechange.duration 1";
        sumoProcess.StartInfo.WorkingDirectory = sumoSimPath;
        sumoProcess.StartInfo.Arguments = $" -c {sumoCfgPath} --start --remote-port 4001 --step-length {simOptions.simSpeed * 0.02} {lateralVar} --quit-on-end --ignore-route-errors true";
        sumoProcess.Start();
        //sumoProcess = Process.Start("sumo-gui", $" -c {sumoCfgPath} --start --remote-port 4001 --step-length {simOptions.simSpeed*0.02} --lateral-resolution {simOptions.lateralResolution} --quit-on-end --chargingstations-output \"map.cgs.xml\"");

        if (sumoProcess == null)
        {
            Debug.LogError("Sumo not started: " + sumoProcess.ToString());
            return;
        }

        client = new TraCIClient();
        connected = client.Connect("127.0.0.1", 4001); //connects to SUMO simulation
        if (!connected || client.TrafficLight.GetIdList() == null)
        {
            Debug.LogWarning("No connection");
        }
        else
        {
            tlightids = client.TrafficLight.GetIdList().Content; //all traffic light IDs in the simulation
            SetSimRunning(true);
            uISystem.uiselection.SetUpdating(true);
            createTLS();
        }
    }

    public void PlayButtonAction()
    {
        if (!connected)
        {
            BeginSumo();
        }
        else
        {
            SetSimRunning(!IsSimRunning());
        }
    }

    private void OnApplicationQuit()
    {
        if (connected) TerminateSumo();
    }

    public void TerminateSumo()
    {
        client.Control.Close();//terminates the connection upon ending of the scene
        isRunningStep = false;
        isSimRunning = false;
        connected = false;
        uISystem.uiselection.SetUpdating(false);
        Deselect();
        //remove all the cars
        foreach (GameObject car in carlist) Destroy(car);
        carlist.Clear();
    }

    public bool IsSimRunning()
    {
        return isSimRunning;
    }
    public bool IsConnected()
    {
        return connected;
    }

    public void SetSimRunning(bool active)
    {
        isSimRunning = active;
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        if (connected && isSimRunning)
        {
            SimStep();
        }
        UpdateSelected();
    }

    private void OnDisable()
    {
        if (connected) TerminateSumo();
    }

    private void UpdateSelected()
    {
        if (selectedCar!=null)
        {
            //update selected car
            selectObject.transform.position = selectedCar.transform.position;
        }
        if (selectedcStation !=null)
        {
            //update selected cstation
        }
    }

    public void Select(SumoVehicle car) {
        if (car.Equals(selectedCar)) return;
        Deselect();
        selectedCar = car;
        selectObject.transform.position = selectedCar.transform.position;
        selectObject.SetActive(true);
        uISystem.uiselection.SetSelected(car);
    }
    public void Select(SumoCStation cStation) {
        if (cStation.Equals(selectedcStation)) return;
        Deselect();
        selectedcStation = cStation;
        uISystem.uiselection.SetSelected(cStation);
    }
    public void Deselect() {
        if (selectedcStation != null)
        {
            selectedcStation.Unselected();
            selectedcStation = null;
        }
        if (selectedCar !=null)
        {
            selectedCar = null;
        }
        selectObject.SetActive(false);
        uISystem.uiselection.SetSelected(null);
    }

    private void SimStep()
    {
        if (isRunningStep) return;
        AddNewVehicles();//add new vehicles
        RemoveOldVehicles();
        StartCoroutine(MoveVehicles());
        TLS(); //Traffic light system

        //sumo simstep
        client.Control.SimStep();
    }

    private void TLS()
    {
        //change trafficlights
        try
        {
            var currentphase = client.TrafficLight.GetCurrentPhase(tlightids[0]);
            //checks traffic light's phase to see if it has changed
            if (currentphase != null && client.TrafficLight.GetCurrentPhase(tlightids[0]).Content != currentphase.Content)
            {
                changeTrafficLights();
            }
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    private IEnumerator MoveVehicles()
    {
        isRunningStep = true;
        Stopwatch sw = Stopwatch.StartNew();
        List<GameObject> vehicleErrors = new List<GameObject>();
        //move current vehicles
        foreach (GameObject carid in carlist)
        {
            if (maxFps && sw.ElapsedMilliseconds > frameTimeS)
            {
                yield return null;
                if (!connected) yield break;
                sw.Restart();
            }
            try
            {
                var carpos = client.Vehicle.GetPosition(carid.name).Content; //gets position of NPC vehicle
                carid.transform.position = new Vector3((float)carpos.X, vehHeight, (float)carpos.Y);

                var newangle = client.Vehicle.GetAngle(carid.name).Content; //gets angle of NPC vehicle
                carid.transform.rotation = Quaternion.Euler(0f, (float)newangle, 0f);
            }
            catch (Exception)
            {
                Debug.Log($"Failed to move car with id: {carid}");
                vehicleErrors.Add(carid);
            }

        }
        sw.Stop();
        //remove error vehicles
        foreach (GameObject errorcar in vehicleErrors)
        {
            RemoveVehicle(errorcar);
        }
        isRunningStep = false;
    }

    private void RemoveOldVehicles()
    {
        var vehiclesleft = client.Simulation.GetArrivedIDList("0").Content; //vehicles that have left the simulation
        //remove old and error vehicles
        for (int j = 0; j < vehiclesleft.Count; j++)
        {
            GameObject toremove = vehObject.Find($"{vehiclesleft[j]}")?.gameObject;
            if (toremove != null)
            {
                RemoveVehicle(toremove);
            }

        }
    }

    private void AddNewVehicles()
    {
        var newvehicles = client.Simulation.GetDepartedIDList("0").Content; //new vehicles this step
        for (int i = 0; i < newvehicles.Count; i++)
        {
            var newcarposition = client.Vehicle.GetPosition(newvehicles[i]).Content; //gets position of new vehicle

            GameObject newcar = GameObject.Instantiate(NPCVehicle, vehObject); //creates the vehicle GameObject
            newcar.transform.position = new Vector3((float)newcarposition.X, vehHeight, (float)newcarposition.Y);//maps its initial position
            var newangle = client.Vehicle.GetAngle(newvehicles[i]).Content;
            newcar.transform.rotation = Quaternion.Euler(0f, (float)newangle, 0f);//maps initial angle

            //newcar.name = $"{newvehicles[i]}";//object name the same as SUMO simulation version
            SumoVehicle sv = newcar.GetComponent<SumoVehicle>();
            if (sv !=null)
            {
                sv.SetID(newvehicles[i]);
            }
            carlist.Add(newcar);

        }
    }

    private void RemoveVehicle(GameObject toremove)
    {
        if (selectedCar!=null&& toremove.Equals(selectedCar.gameObject)) Deselect();
        carlist.Remove(toremove);
        Destroy(toremove);
    }



    //Changes traffic lights to their next phase
    void changeTrafficLights()
    {
        for (int i = 0; i < tlightids.Count; i++)
        {
            //for each traffic light value of a junctions name
            for (int k = 0; k < tlightMap[tlightids[i]].Count; k++)
            {
				
                var newstate = client.TrafficLight.GetState(tlightids[i]).Content;
                var lightchange = tlightMap[tlightids[i]][k]; //retrieves traffic light object from list
                
                var chartochange = newstate[lightchange.index].ToString();//traffic lights new state based on its index
                if (lightchange.isdual == false)
                {
                    lightchange.changeState(chartochange.ToLower());//single traffic light change
                }
                else
                {
                    lightchange.changeStateDual(chartochange.ToLower());//dual traffic light change
                }

            }
        }

    }
    
    
    // Creates the TLS for of all junctions in the SUMO simulation
    void createTLS()
    {
        if (tlightids == null) return;
        tlightMap = new Dictionary<string, List<traLights>>(); //the dictionary to hold each junctions traffic lights
        for (int ids = 0; ids < tlightids.Count; ids++)	
        {
            List<traLights> traLightslist = new List<traLights>();
            int numconnections = 0;  //The index that represents the traffic light's state value
            var newjunction = GameObject.Find(tlightids[ids]); //the traffic light junction
            for (int i = 0; i < newjunction.transform.childCount; i++)
            {
                bool isdouble = false;
                var trafficlight = newjunction.transform.GetChild(i);//the next traffic light in the junction
                //Checks if the traffic light has more than 3 lights
                if (trafficlight.childCount > 3)
                {
                    isdouble = true;
                }
                Light[] newlights = trafficlight.GetComponentsInChildren<Light>();//list of light objects belonging to
                                                                                  //the traffic light
               //Creation of the traffic light object, with its junction name, list of lights, index in the junction
               //and if it is a single or dual traffic light
                traLights newtraLights = new traLights(newjunction.name, newlights, numconnections, isdouble);
                traLightslist.Add(newtraLights);
                var linkcount = client.TrafficLight.GetControlledLinks(newjunction.name).Content.NumberOfSignals;
                var laneconnections = client.TrafficLight.GetControlledLinks(newjunction.name).Content.Links;
                if (numconnections+1 < linkcount - 1)
                {
                    numconnections++;//index increases
                    //increases index value until the next lane is reached
                    while ((laneconnections[numconnections][0] == laneconnections[numconnections - 1][0] || isdouble) &&
                           numconnections < linkcount - 1)
                    {
						//if the next lane is reached but the traffic light is a dual lane, continue until the
						//lane after is reached
                        if (laneconnections[numconnections][0] != laneconnections[numconnections - 1][0] && isdouble)
                        {
                            isdouble = false;
                        }
                        numconnections++;
                    }
                }
            }
            tlightMap.Add(newjunction.name, traLightslist);
        }
        changeTrafficLights(); //displays the initial state of all traffic lights
    }

    static string InQuotes(string inp)
    {
        return "\""+inp+ "\"";
    }

    public void StartSimAnalysis()
    {
        StartCoroutine(SimulationAnalysis());
    }

    public IEnumerator SimulationAnalysis()
    {
        sw = Stopwatch.StartNew();
        uISystem.SetLoading();
        float totalTasks = 3f;
        uISystem.LoadingProgress(2f / totalTasks, "Gathering SUMO Data");
        uISystem.SetLoadingBarSpeed(0.001f);
        yield return StartCoroutine(RunSumoData());
        uISystem.ResetLoadingBarSpeed();
        //uISystem.LoadingProgress(1f / totalTasks, "Gathering SUMO Data");
        //yield return StartCoroutine(RunSumoData());
        uISystem.LoadingProgress(3f / totalTasks, "Done");
        yield return new WaitForSeconds(0.2f);
        uISystem.FinishLoading();
        uISystem.ToData();
        sw.Stop();
    }

    public IEnumerator RunSumoData()
    {
        string cgsfilename = "map.cgs.xml";
        if (File.Exists(sumoSimPath + cgsfilename)) File.Delete(sumoSimPath + cgsfilename);
        Process sumoProcess = new Process();
        bool quitonend = !simOptions.waitForUserToClose;
        try
        {
            Debug.Log("Using Sumo");
            string sumoCfgPath = InQuotes(sumoSimPath + "map.sumocfg");
            //start sumo process
            if (simOptions.hideSumoWindow)
            {
                sumoProcess.StartInfo.UseShellExecute = true;
                sumoProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                sumoProcess.StartInfo.FileName = @"sumo";
            }
            else
            {
                sumoProcess.StartInfo.FileName = @"sumo-gui";
            }
            string lateralVar = $"--lateral-resolution { simOptions.lateralResolution}";
            if (!simOptions.useSublaneModel) lateralVar = "--lanechange.duration 1";
            sumoProcess.StartInfo.WorkingDirectory = sumoSimPath;
            sumoProcess.StartInfo.Arguments = $" -c {sumoCfgPath} --start --step-length {1} {lateralVar} --quit-on-end {quitonend} --chargingstations-output \"map.cgs.xml\"  --log map.log --statistic-output map.stat.log --aggregate-warnings 1";
            sumoProcess.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("SUMO Failed: " + e.ToString());
        }
        while (!sumoProcess.HasExited)
        {
            yield return null;
        }
        sumoProcess.WaitForExit();
        //update time
        simOptions.dataTime = DateTime.Now.ToString();
        SaveSumoOptions();
        uISystem.ResetData();
        if (File.Exists(sumoSimPath + cgsfilename)) Debug.Log("SUMO data was successful");
        else Debug.LogWarning("SUMO data was not successful");
    }
}
