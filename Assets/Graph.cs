using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Flags]
public enum NodeType
{
    OOB = -5,
    UNASSIGNED = 0,
    EMPTY   = 1 << 0,
    PATH    = 1 << 1,
    CYCLE   = 1 << 2,
    END     = 1 << 3,
    ROOM    = 1 << 4,

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

    public Node(GameObject gameObject, Vector3Int position, Graph graph, NodeType nodeType = NodeType.UNASSIGNED)
    {
        if (gameObject != null)
        {
            this.gameObject = gameObject;
            gameObject.name = position.ToString();
            image = gameObject.GetComponent<Image>();
        }
        this.position = position;
        this.graph = graph;
        NodeType = nodeType;
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

    //Add null == OOB
    public bool HasAdjacent(System.Predicate<Node> match)
    {
        for (int i = 0; i < AdjNodeCount; ++i)
        {
            if (adjNodes[i] != null && match(adjNodes[i]))
                return true;
        }
        return false;
    }

    //Add null == OOB
    public Node FindAdjacent(System.Predicate<Node> match)
    {
        for(int i = 0; i < AdjNodeCount; ++i)
        {
            if (adjNodes[i] != null && match(adjNodes[i]))
                return adjNodes[i];
        }
        return null;
    }

    //Add null == OOB
    public Node[] FindAllAdjacent(System.Predicate<Node> match)
    {
        List<Node> adjList = new List<Node>();
        for (int i = 0; i < AdjNodeCount; ++i)
        {
            if (adjNodes[i] != null && match(adjNodes[i]))
                adjList.Add(adjNodes[i]);
        }
        return adjList.ToArray(); ;
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

    public List<Node> Nodes {set => nodes = value; }
    public List<GameObject> Edges { get => edges; set => edges = value; }
    public int numNodes { get => nodes.Count; }

    public Graph(GameObject edgePrefab, Transform edgeHolder)
    {
        this.edgePrefab = edgePrefab;
        this.edgeHolder = edgeHolder;
    }

    public void InitializeNodes(Grid grid)
    {
        for (int i = 0; i < grid.transform.childCount; ++i)
        {
            var nodeTransform = grid.transform.GetChild(i);
            Node node = new Node(nodeTransform.gameObject, grid.WorldToCell(nodeTransform.position), this);
            nodes.Add(node);
        }

        for (int i = 0; i < nodes.Count; ++i)
        {
            nodes[i].InitAdjacentNodes(); ;
        }
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

    public void AddNode(Node node)
    {

    }

    public void AddEdge(Node nodeA, Node nodeB)
    {
        if (edgePrefab != null || edgeHolder != null)
        {
            GameObject edgeGO = GameObject.Instantiate(edgePrefab, nodeA.transform);
            edges.Add(edgeGO);
            edgeGO.transform.eulerAngles = Vector3.forward * (Node.Direction(nodeA, nodeB) * -90f);
            edgeGO.transform.SetParent(edgeHolder);
        }
        nodeA.AddEdge(nodeB);
        nodeB.AddEdge(nodeA);
    }

    public Node[] FindAllNodes(System.Predicate<Node> predicate)
    {
        List<Node> foundNodes = new List<Node>();

        for(int i = 0; i < nodes.Count; ++i)
        {
            if (predicate(nodes[i]))
                foundNodes.Add(nodes[i]);
        }

        return foundNodes.ToArray();
    }

    //Need to constrain that all nodes are contiguous
    public Node[] FindSubGraph(Graph graph)
    {

        if(graph.numNodes <= 0)
        {
            Debug.LogError("Graph empty.");
            return null;
        }

        Node startNode = graph.GetNodeByCell(Vector3Int.zero);
        if (startNode == null)
            startNode = graph.nodes[0];

        if (graph.numNodes <= 1)
        {
            Debug.LogError("Graph too small.");
            return null;
        }
        else if(!startNode.HasAdjacent(n => n != null))
        {
            Debug.LogError("Graph is non-contiguous.");
            return null;
        }

        var matchCandidates = FindAllNodes(n => n == startNode);
        

        return new Node[] { startNode};
    }
}
