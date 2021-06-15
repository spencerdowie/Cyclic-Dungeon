using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum NodeType
{
    OOB = -5,
    ANY = -2,
    UNASSIGNED = -1,
    EMPTY = 0,
    PATH,
    CYCLE,
    END,
    ROOM
}

public class Node
{
    GameObject gameObject;
    readonly public Vector3Int position;
    Image image;
    NodeType nodeType = NodeType.UNASSIGNED;
    Dictionary<Node, Edge> adjEdges = new Dictionary<Node, Edge>();
    Node[] adjNodes = new Node[4];
    [SerializeField] Color[] colors = new Color[] { Color.white, Color.green, Color.cyan, Color.red, Color.yellow };
    public string Name { 
        get 
        {
            if (gameObject)
                return gameObject.name;
            return "";
        }
    }
    Graph graph;
    private bool initialized = false;

    public Node(GameObject gameObject, Vector3Int position, Graph graph)
    {
        this.gameObject = gameObject;
        image = gameObject.GetComponent<Image>();
        this.position = position;
        gameObject.name = position.ToString();
        NodeType = NodeType.UNASSIGNED;
        this.graph = graph;
    }

    public Transform transform { get => gameObject.transform; }
    public NodeType NodeType
    {
        get => nodeType;
        set
        {
            nodeType = value;
            if (value == NodeType.UNASSIGNED)
            {
                image.color = Color.gray;
            }
            else if((int)value < colors.Length)
            {
                image.color = colors[(int)value];
            }
            else
            {
                Debug.LogError("Node Colour Out of Bounds.");
            }
        }
    }
    public int AdjNodeCount { get => adjNodes.Length; }

    public void InitAdjacentNodes()
    {
        if (initialized)
            return;

        adjNodes[0] = graph.GetNodeByCell(position + Vector3Int.up);
        adjNodes[1] = graph.GetNodeByCell(position + Vector3Int.right);
        adjNodes[2] = graph.GetNodeByCell(position + Vector3Int.down);
        adjNodes[3] = graph.GetNodeByCell(position + Vector3Int.left);

        initialized = true;
    }

    public void AddEdge(Node nodeB)
    {
        if (adjEdges.ContainsKey(nodeB))
            return;
        adjEdges[nodeB] = new Edge(this, nodeB);
    }

    public bool IsAdjacent(Node nodeB)
    {
        return adjEdges.ContainsKey(nodeB);
    }

    public bool ContainsAdjacent(System.Predicate<Node> match)
    {
        return System.Array.FindIndex(adjNodes, match) > -1;
    }

    public Node FindAdjacent(System.Predicate<Node> match)
    {
        for(int i = 0; i < AdjNodeCount; ++i)
        {
            if (adjNodes[i] != null && match(adjNodes[i]))
                return adjNodes[i];
        }
        return null;
    }

    public bool HasNodeInDirection(int direction)
    {
        if (direction < 0 || direction > 3)
            Debug.LogError("Direction out of bounds");
        return adjEdges.ContainsKey(adjNodes[direction]);
    }

    public Node GetNodeInDirection(int direction)
    {
        if (adjNodes[direction] == null)
            return null;
        return adjNodes[direction];
    }

    public bool MatchPattern(NodeType up = NodeType.ANY, NodeType right = NodeType.ANY, NodeType down = NodeType.ANY, NodeType left = NodeType.ANY)
    {
        var upNode = graph.GetNodeByCell(position + Vector3Int.up);
        bool upMatch = up == NodeType.ANY || (upNode != null ? upNode.NodeType == up : up == NodeType.OOB);

        var rightNode = graph.GetNodeByCell(position + Vector3Int.right);
        bool rightMatch = right == NodeType.ANY || (rightNode != null ? rightNode.NodeType == right : right == NodeType.OOB);

        var downNode = graph.GetNodeByCell(position + Vector3Int.down);
        bool downMatch = down == NodeType.ANY || (downNode != null ? downNode.NodeType == down : down == NodeType.OOB);

        var leftNode = graph.GetNodeByCell(position + Vector3Int.left);
        bool leftMatch = left == NodeType.ANY || (leftNode != null ? leftNode.NodeType == left : left == NodeType.OOB);

        return upMatch && rightMatch && downMatch && leftMatch;
    }

    static public int Direction(Node nodeA, Node nodeB)
    {
        return System.Array.IndexOf(nodeA.adjNodes, nodeB);
    }

    static public int Direction(Node nodeA, System.Predicate<Node> match)
    {
        for(int i = 0; i < nodeA.AdjNodeCount; ++i)
        {
            if (match(nodeA.adjNodes[i]))
                return i;
        }
        return -1;
    }

    static public int up = 0, right = 1, down = 2, left = 3;
}

public class Edge
{
    readonly public Node[] nodes = new Node[2];

    public Edge(Node nodeA, Node nodeB)
    {
        nodes[0] = nodeA;
        nodes[1] = nodeB;
    }
}


public class Graph
{
    List<Node> nodes = new List<Node>();
    List<GameObject> edges = new List<GameObject>();
    readonly public int width = 5, height = 5;
    private GameObject edgePrefab;
    private Transform edgeHolder;

    public List<Node> Nodes { get => nodes; set => nodes = value; }
    public List<GameObject> Edges { get => edges; set => edges = value; }
    public int numNodes { get => nodes.Count; }

    public Graph(GameObject edgePrefab, Transform edgeHolder)
    {
        this.edgePrefab = edgePrefab;
        this.edgeHolder = edgeHolder;
    }

    public Node GetNodeByCell(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            return null;

        var index = cell.x + (cell.y * width);
        if (index < 0 || index >= nodes.Count)
            return null;
        var node = nodes[index];
        return node;
    }

    public void AddEdge(Node nodeA, Node nodeB)
    {
        GameObject edgeGO = GameObject.Instantiate(edgePrefab, nodeA.transform);
        edges.Add(edgeGO);
        nodeA.AddEdge(nodeB);
        nodeB.AddEdge(nodeA);
        edgeGO.transform.eulerAngles = Vector3.forward * (Node.Direction(nodeA, nodeB) * -90f);
        edgeGO.transform.SetParent(edgeHolder);
    }
}

public class Path : MonoBehaviour
{
    Graph graph;
    [SerializeField] GameObject edgePrefab;
    [SerializeField] Transform edgeHolder;

    private void Start()
    {
        graph = new Graph(edgePrefab, edgeHolder);

        InitializeNodes();

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

        StartCycle(startNode);
    }

    public void InitializeNodes()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            Grid grid = GetComponent<Grid>();
            var nodeTransform = transform.GetChild(i);
            Node node = new Node(nodeTransform.gameObject, grid.WorldToCell(nodeTransform.position), graph);
            graph.Nodes.Add(node);
        }

        for(int i = 0; i < graph.numNodes; ++i)
        {
            graph.Nodes[i].InitAdjacentNodes(); ;
        }
    }

    private void LeftBottomLineStart()
    {
        var initalSetup = graph.Nodes.FindAll(n => n.position.x == 0);
        initalSetup.AddRange(graph.Nodes.FindAll(n => n.position.y == 0));
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
        var startCandidates = graph.Nodes.FindAll(n =>
                    n.NodeType == NodeType.EMPTY && (
                    n.MatchPattern(up: NodeType.UNASSIGNED) ||
                    n.MatchPattern(right: NodeType.UNASSIGNED))
                );

        var startNode = startCandidates[Random.Range(0, startCandidates.Count)];
        startNode.NodeType = NodeType.PATH;
        return startNode;
    }

    public void StartCycle(Node startNode)
    {
        var cycleStart = startNode.FindAdjacent(n => n.NodeType == NodeType.UNASSIGNED);
        cycleStart.NodeType = NodeType.ROOM;


        int walkDir = (Node.Direction(cycleStart, n => n == null) + 2) % 4;
        var cycleMid = cycleStart.GetNodeInDirection(walkDir);
        graph.AddEdge(cycleStart, cycleMid);
        cycleMid.NodeType = NodeType.CYCLE;

        cycleMid.GetNodeInDirection(walkDir).NodeType = NodeType.EMPTY;
    }
}
