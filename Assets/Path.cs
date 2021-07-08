using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Path : MonoBehaviour
{
    Graph graph;
    [SerializeField] GameObject edgePrefab;
    [SerializeField] Transform edgeHolder;

    private void Start()
    {
        graph = new Graph(edgePrefab, edgeHolder);

        graph.InitializeNodes(GetComponent<Grid>());

        {
            //for(int y = 0; y < graph.height -1; ++y)
            //{
            //    Vector3Int cell = new Vector3Int(0, y, 0);
            //    Debug.Log($"CellA: {cell} CellB: {cell + Vector3Int.up}");
            //    GameObject edgeGO = Instantiate(edgePrefab, graph.GetNodeByCell(cell).transform);
            //    edgeGO.transform.eulerAngles = Vector3.forward * 90f;
            //    graph.AddEdge(graph.GetNodeByCell(cell), graph.GetNodeByCell(cell + Vector3Int.up), edgeGO);
            //    edgeGO.transform.SetParent(edgeHolder);
            //}

            //for (int x = 0; x < graph.width - 1; ++x)
            //{
            //    Vector3Int cell = new Vector3Int(x, 0, 0);
            //    Debug.Log($"CellA: {cell} CellB: {cell + Vector3Int.right}");
            //    GameObject edgeGO = Instantiate(edgePrefab, graph.GetNodeByCell(cell).transform);
            //    graph.AddEdge(graph.GetNodeByCell(cell), graph.GetNodeByCell(cell + Vector3Int.right), edgeGO);
            //    edgeGO.transform.SetParent(edgeHolder);
            //}

            //for (int i = 0; i < graph.numNodes; ++i)
            //{
            //    if ((i % graph.width) == (graph.width - 1))
            //        continue;
            //    GameObject edgeGO = Instantiate(edgePrefab, graph.Nodes[i].transform);
            //    graph.AddEdge(graph.Nodes[i], graph.Nodes[i + 1], edgeGO);
            //    edgeGO.transform.SetParent(edgeHolder);
            //}

            //for (int i = 0; i < graph.numNodes; ++i)
            //{
            //    if (i >= (graph.height - 1) * graph.width)
            //        continue;
            //    GameObject edgeGO = Instantiate(edgePrefab, graph.Nodes[i].transform);
            //    edgeGO.transform.eulerAngles = Vector3.forward * 90f;
            //    graph.AddEdge(graph.Nodes[i], graph.Nodes[i + graph.width], edgeGO);
            //    edgeGO.transform.SetParent(edgeHolder);
            //}
        }

        //LeftBottomLineStart();
        CornerStart();

        var startNode = PickStartingLocation();

        var cycleStart = PickCycleStart(startNode);
        AddEmptyNodes();
        StartCycle(cycleStart);
    }

    private void LeftBottomLineStart()
    {
        List<Node> initalSetup = new List<Node>(graph.FindAllNodes(n => n.position.x == 0));
        initalSetup.AddRange(graph.FindAllNodes(n => n.position.y == 0));
        for (int i = 0; i < initalSetup.Count; ++i)
        {
            initalSetup[i].NodeType = NodeType.EMPTY;
        }
    }

    private void CornerStart()
    {
        List<Node> corners = new List<Node>();
        corners.Add(graph.GetNodeByCell(Vector3Int.zero));
        corners.Add(graph.GetNodeByCell(new Vector3Int(graph.width - 1, 0, 0)));
        corners.Add(graph.GetNodeByCell(new Vector3Int(0, graph.height - 1, 0)));
        corners.Add(graph.GetNodeByCell(new Vector3Int(graph.width - 1, graph.height - 1, 0)));

        for (int i = 0; i < corners.Count; ++i)
        {
            corners[i].NodeType = NodeType.EMPTY;
        }
    }

    public Node PickStartingLocation()
    {
        var startCandidates = graph.FindAllNodes(n => n.NodeType.HasFlag(NodeType.EMPTY));

        var startNode = startCandidates[Random.Range(0, startCandidates.Length)];
        startNode.NodeType = NodeType.PATH;
        return startNode;
    }

    public Node PickCycleStart(Node startNode)
    {
        var pathEntrance = startNode.FindAdjacent(n => n.NodeType.HasFlag(NodeType.UNASSIGNED));
        pathEntrance.NodeType = NodeType.ROOM;


        int walkDir = (Node.Direction(pathEntrance, n => n == null) + 2) % 4;
        var cycleStart = pathEntrance.GetNodeInDirection(walkDir);
        graph.AddEdge(pathEntrance, cycleStart);
        cycleStart.NodeType = NodeType.CYCLE;

        return cycleStart;
    }

    public void AddEmptyNodes()
    {
        var midNode = graph.GetNodeByCell(new Vector3Int(graph.width/2, graph.height/2, 0));
        midNode.NodeType = NodeType.EMPTY;
        var adjMid = midNode.GetNodeInDirection(Random.Range(0, 4));
        adjMid.NodeType = NodeType.EMPTY;
    }

    public void StartCycle(Node cycleStart)
    {
        var nextNode = cycleStart.FindAdjacent(node => (node.NodeType.HasFlag(NodeType.UNASSIGNED) && node.HasAdjacent(n => n.NodeType.HasFlag(NodeType.EMPTY))));
        
        nextNode.NodeType = NodeType.CYCLE;

        graph.AddEdge(cycleStart, nextNode);
    }

    public void SurroundMiddle()
    {
        Graph subGraph = new Graph(null, null);

        graph.AddNode(new Node(null, Vector3Int.zero, subGraph, NodeType.EMPTY));
        graph.AddNode(new Node(null, Vector3Int.right, subGraph, NodeType.EMPTY));


        var emptyPair = graph.FindSubGraph(subGraph);
    }
}
