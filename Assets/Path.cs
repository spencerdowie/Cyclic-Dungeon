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
    START,
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
    List<Node> adjNodes = new List<Node>();
    [SerializeField] Color[] colors = new Color[] { Color.white, Color.green, Color.red, Color.yellow };
    public string Name { 
        get 
        {
            if (gameObject)
                return gameObject.name;
            return "";
        }
    }
    Graph graph;

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
    public int AdjNodeCount { get => adjNodes.Count; }

    public void AddEdge(Node nodeB)
    {
        if (adjNodes.Contains(nodeB))
            return;
        adjNodes.Add(nodeB);
        adjEdges[nodeB] = new Edge(this, nodeB);
    }

    public bool IsAdjacent(Node nodeB)
    {
        return adjNodes.Contains(nodeB);
    }

    public bool ContainsAdjacent(System.Predicate<Node> match)
    {
        return adjNodes.FindIndex(match) > -1;
    }

    public Node FindAdjacent(System.Predicate<Node> match)
    {
        return adjNodes.Find(match);
    }

    public bool HasNodeInDirection(Vector3Int direction)
    {
        for(int i = 0; i < adjNodes.Count; ++i)
        {
            if (Direction(this, adjNodes[i]) == direction)
                return true;
        }
        return false;
    }

    public Node GetNodeInDirection(Vector3Int direction)
    {
        for (int i = 0; i < adjNodes.Count; ++i)
        {
            if (Direction(this, adjNodes[i]) == direction)
                return adjNodes[i];
        }
        return null;
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

    static public Vector3Int Direction(Node nodeA, Node nodeB)
    {
        Vector3Int nodeACell = nodeA.position, nodeBCell = nodeB.position;
        Vector3Int vec = nodeBCell - nodeACell;
        return vec;
    }
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

    public List<Node> Nodes { get => nodes; set => nodes = value; }
    public List<GameObject> Edges { get => edges; set => edges = value; }
    public int numNodes { get => nodes.Count; }
    public Node GetNodeByCell(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            return null;

        var index = cell.x + (cell.y * width);
        var node = nodes[index];
        return node;
    }

    public void AddEdge(Node nodeA, Node nodeB, GameObject edgeGO)
    {
        edges.Add(edgeGO);
        nodeA.AddEdge(nodeB);
        nodeB.AddEdge(nodeA);
    }
}

public class Path : MonoBehaviour
{
    Graph graph = new Graph();
    [SerializeField] GameObject edgePrefab;
    [SerializeField] Transform edgeHolder;

    private void Start()
    {
        Grid grid = GetComponent<Grid>();

        for(int i = 0; i < transform.childCount; ++i)
        {
            var nodeTransform = transform.GetChild(i);
            Node node = new Node(nodeTransform.gameObject, grid.WorldToCell(nodeTransform.position), graph);
            graph.Nodes.Add(node);
        }

        //for(int y = 0; y < graph.height -1; ++y)
        //{
        //    Vector3Int cell = new Vector3Int(0, y, 0);
        //    Debug.Log($"CellA: {cell} CellB: {cell + Vector3Int.up}");
        //    GameObject edgeGO = Instantiate(edgePrefab, graph.GetNodeByCell(cell).transform);
        //    edgeGO.transform.eulerAngles = Vector3.forward * 90f;
        //    graph.AddEdge(graph.GetNodeByCell(cell), graph.GetNodeByCell(cell + Vector3Int.up), edgeGO);
        //    edgeGO.transform.SetParent(edgeHolder);
        //}

        for (int i = 0; i < graph.numNodes; ++i)
        {
            if ((i % graph.width) == (graph.width - 1))
                continue;
            GameObject edgeGO = Instantiate(edgePrefab, graph.Nodes[i].transform);
            graph.AddEdge(graph.Nodes[i], graph.Nodes[i + 1], edgeGO);
            edgeGO.transform.SetParent(edgeHolder);
        }

        //for (int x = 0; x < graph.width - 1; ++x)
        //{
        //    Vector3Int cell = new Vector3Int(x, 0, 0);
        //    Debug.Log($"CellA: {cell} CellB: {cell + Vector3Int.right}");
        //    GameObject edgeGO = Instantiate(edgePrefab, graph.GetNodeByCell(cell).transform);
        //    graph.AddEdge(graph.GetNodeByCell(cell), graph.GetNodeByCell(cell + Vector3Int.right), edgeGO);
        //    edgeGO.transform.SetParent(edgeHolder);
        //}

        for (int i = 0; i < graph.numNodes; ++i)
        {
            if (i >= (graph.height - 1) * graph.width)
                continue;
            GameObject edgeGO = Instantiate(edgePrefab, graph.Nodes[i].transform);
            edgeGO.transform.eulerAngles = Vector3.forward * 90f;
            graph.AddEdge(graph.Nodes[i], graph.Nodes[i + graph.width], edgeGO);
            edgeGO.transform.SetParent(edgeHolder);
        }

        var initalSetup = graph.Nodes.FindAll(n => n.position.x == 0);
        initalSetup.AddRange(graph.Nodes.FindAll(n => n.position.y == 0));
        for (int i = 0; i < initalSetup.Count; ++i)
        {
            initalSetup[i].NodeType = NodeType.EMPTY;
        }

        var startCandidates = graph.Nodes.FindAll(n =>
            n.NodeType == NodeType.EMPTY && (
            n.MatchPattern(up: NodeType.UNASSIGNED) ||
            n.MatchPattern(right: NodeType.UNASSIGNED))
        );

        var startNode = startCandidates[Random.Range(0, startCandidates.Count)];
        startNode.NodeType = NodeType.START;

        var cycleStart =  startNode.FindAdjacent(n => n.NodeType == NodeType.UNASSIGNED);
        cycleStart.NodeType = NodeType.ROOM;

        var cycleMid = cycleStart.GetNodeInDirection(Node.Direction(startNode, cycleStart));
        cycleMid.NodeType = NodeType.EMPTY;


    }
}
