﻿using System.Collections.Generic;
using UnityEngine;
using MCTS.Core;
using MCTS.Core.Games;

namespace MCTS.Visualisation.Hashing
{
    /// <summary>
    /// Main controller for the hashing visualisation <para/>
    /// In this visualisation, each board state is given its own position in world space and <see cref="LineRenderer"/> objects are used to create links between the nodes
    /// </summary>
    public class MainController : MonoBehaviour
    {
        /// <summary>
        /// A dictionary that maps unique positions in world space to their corresponding node gameobjects
        /// </summary>
        public Dictionary<Vector3, GameObject> nodePositionMap = new Dictionary<Vector3, GameObject>();

        /// <summary>
        /// A dictionary that maps unique nodes to their corresponding node gameobjects
        /// </summary>
        public Dictionary<Node, GameObject> nodeObjectMap = new Dictionary<Node, GameObject>();

        /// <summary>
        /// A list of all hash nodes in the current scene
        /// </summary>
        public List<HashNode> AllNodes = new List<HashNode>();

        /// <summary>
        /// The MCTS used to provide node data
        /// </summary>
        public TreeSearch<Node> mcts;

        /// <summary>
        /// The timestamp that the last step was performed <para/>
        /// Used to determine when to do the next step
        /// </summary>
        private float lastSpawn;

        /// <summary>
        /// Flag which controls whether the playing animation is active <para/>
        /// When the playing animation is active, nodes are created periodically
        /// </summary>
        bool playing = false;

        /// <summary>
        /// The delay in seconds between nodes being created when <see cref="playing"/> is active
        /// </summary>
        private const float SPAWN_DELAY = 0.025f;

        /// <summary>
        /// The amount of spacing between each node
        /// </summary>
        private const float NODE_SPACING = 7f;

        /// <summary>
        /// Enable the running of the application in the background when initially ran
        /// </summary>
        public void Start()
        {
            Application.runInBackground = true;
        }

        /// <summary>
        /// Called every frame <para/>
        /// Used to control the creation of new nodes when <see cref="playing"/> is true <para/>
        /// Also used to listen for user input
        /// </summary>
        public void Update()
        {
            if (mcts == null)
            {
                return;
            }

            //Allow the user to perform a step with the return key or Y button instead of pressing the step button
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("YButton"))
            {
                PerformStep(false);
            }

            //Allow the user to toggle the play/pause options with the p key or B button instead of pressing the respective buttons
            if (Input.GetKeyDown(KeyCode.P) || Input.GetButtonDown("BButton"))
            {
                TogglePlayPause();
            }


            if (playing && Time.time - lastSpawn > SPAWN_DELAY)
            {
                PerformStep(false);
                lastSpawn = Time.time;
            }
        }

        /// <summary>
        /// Called when the user enters the play/pause input <para/>
        /// Toggles play/pause depending on which one is currently active
        /// </summary>
        public void TogglePlayPause()
        {
            if (playing)
            {
                PauseButtonPressed();
            }
            else
            {
                PlayButtonPressed();
            }
        }

        /// <summary>
        /// Called when the play button is pressed <para/>
        /// Starts the animation of ndoes being creates and sets the pause button to be active
        /// </summary>
        public void PlayButtonPressed()
        {
            playing = true;
            UIController.PlayButtonPressed();
        }

        /// <summary>
        /// Called when the pause button is pressed <para/>
        /// Pauses the animation of nodes beng created and sets the play button to active again
        /// </summary>
        public void PauseButtonPressed()
        {
            playing = false;
            UIController.PauseButtonPressed();
        }

        /// <summary>
        /// Called when the start button is pressed <para/>
        /// Initialises the <see cref="mcts"/> tree search object and instantiates the root node <para/>
        /// Also creates as many starting nodes as the user specified
        /// </summary>
        public void StartButtonPressed()
        {
            mcts = new TreeSearch<Node>(new TTTBoard());

            //Calculate the position of the root node and add an object for it to the scene
            Vector3 rootNodePosition = BoardToPosition((TTTBoard)mcts.Root.GameBoard);
            GameObject rootNode = Instantiate(Resources.Load("Ball"), rootNodePosition, Quaternion.identity) as GameObject;
            rootNode.GetComponent<HashNode>().AddNode(null, mcts.Root);

            //Add the root node to the position and object map
            nodePositionMap.Add(rootNodePosition, rootNode);
            nodeObjectMap.Add(mcts.Root, rootNode);

            for (int i = 0; i < UIController.GetStartingNodeInput(); i++)
            {
                PerformStep(true);
            }

            //Swap out the current menu panels
            UIController.SetMenuPanelActive(false);
            UIController.SetNavigationPanelActive(true);
        }

        /// <summary>
        /// Called when the step button is pressed <para/>
        /// Calls the <see cref="PerformStep(bool)"/> method as long as <see cref="playing"/> is false
        /// </summary>
        public void StepButtonPressed()
        {
            if (playing)
            {
                return;
            }

            PerformStep(false);
        }

        /// <summary>
        /// Performs a step of MCTS and creates a gameobject for the new <see cref="Node"/>'s board state if required <para/>
        /// Also creates any relevant links between related nodes
        /// </summary>
        /// <param name="fromMenu">Flag used to indicate whether this step is being performed from the menu or not. Used to prevent the spawning animation if a node is not being added when the visualisation has already been started</param>
        public void PerformStep(bool fromMenu)
        {
            //Perform an iteration of MCTS
            mcts.Step();

            //Get a reference to the newest node
            Node newestNode = mcts.AllNodes[mcts.AllNodes.Count - 1];

            //Hash the board contents of the newest node to obtain a positon
            Vector3 newNodePosition = BoardToPosition((TTTBoard)newestNode.GameBoard);

            //Decrease the visibility of every node in the scene
            foreach (HashNode n in AllNodes)
            {
                n.SetVisibility(n.Visibility - 0.01f);
            }

            //If the current board state already exists, then don't create a new node, but create a line to the existing node
            if (nodePositionMap.ContainsKey(newNodePosition))
            {
                //Map the newest node to the existing node object if it does not already exist in the dicitonary
                if (!nodeObjectMap.ContainsKey(newestNode))
                {
                    nodeObjectMap.Add(newestNode, nodePositionMap[newNodePosition]);
                }
            }
            else
            {
                //Instantiate the new node object at the hashed position
                GameObject newNodeObject = Instantiate(Resources.Load("Ball"), fromMenu ? newNodePosition : nodeObjectMap[newestNode.Parent].transform.position, Quaternion.identity) as GameObject;

                //Map the newest node to the new node object
                nodeObjectMap.Add(newestNode, newNodeObject);

                //Map the hashed position of the newest node to the new node object
                nodePositionMap.Add(newNodePosition, newNodeObject);

                AllNodes.Add(newNodeObject.GetComponent<HashNode>());
            }

            //Initialise the newest hash node and add a mcts Node to ite
            nodeObjectMap[newestNode].GetComponent<HashNode>().Initialise(newNodePosition);
            nodeObjectMap[newestNode].GetComponent<HashNode>().AddNode(nodeObjectMap[newestNode.Parent], newestNode);

            UIController.SetTotalNodeText(nodeObjectMap.Count);
        }

        /// <summary>
        /// Hashes a <see cref="Board"/> object to a unique position in world space
        /// </summary>
        /// <param name="board">The board to hash</param>
        /// <returns>A <see cref="Vector3"/>, unique to the board state of hte board being hashed </returns>
        public Vector3 BoardToPosition(TTTBoard board)
        {
            float xPos = 0;
            float yPos = 0;
            float zPos = 0;

            for (int y = 0; y < 3; y++)
            {
                xPos += Mathf.Pow(3, y) * board.GetCell(0, y);
                yPos += Mathf.Pow(3, y) * board.GetCell(1, y);
                zPos += Mathf.Pow(3, y) * board.GetCell(2, y);
            }

            return new Vector3(xPos, yPos, zPos) * NODE_SPACING;
        }

    }
}