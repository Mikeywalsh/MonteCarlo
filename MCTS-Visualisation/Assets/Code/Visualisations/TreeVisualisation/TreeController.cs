﻿using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using MCTS.Core;
using MCTS.Core.Games;

namespace MCTS.Visualisation.Tree
{
    /// <summary>
    /// The main script for the application <para/>
    /// Handles the running of the MCTS and the positioning of each <see cref="NodeObject"/> <para/>
    /// Also handles the changeover between the menu UI and navigation UI
    /// </summary>
    public class TreeController : MonoBehaviour
    {
        /// <summary>
        /// Flag indicating how the board is displayed to the user <para/>
        /// True if displaying a board model <para/>
        /// False if displaying a rich text board display
        /// </summary>
        public bool displayBoardModel;

        /// <summary>
        /// The board model display being used for this visualisation <para/>
        /// Null if the <see cref="displayBoardModel"/> flag is set to false
        /// </summary>
        public BoardModelController boardModelController;

        /// <summary>
        /// Singleton reference to the active TreeController
        /// </summary>
        public static TreeController Controller;

        /// <summary>
        /// The MCTS instance used to create the game tree <para/>
        /// Should be ran on another thread to avoid freezing of the application
        /// </summary>
        private TreeSearch<NodeObject> mcts;

        /// <summary>
        /// The root node object, which is the starting point for the MCTS
        /// </summary>
        private NodeObject rootNodeObject;

        /// <summary>
        /// The type of visualisation being performed
        /// </summary>
        private VisualisationType visualisationType;

        /// <summary>
        /// The time to run MCTS for, input by the user
        /// </summary>
        private float timeToRunFor;

        /// <summary>
        /// The time left to run the MCTS for <para/>
        /// Starts at <see cref="timeToRunFor"/> and counts down to zero
        /// </summary>
        private float timeLeft;

        /// <summary>
        /// Used as a toggle to start the visualisation process of assigning each node a gameobject
        /// </summary>
        bool startedVisualisation;

        /// <summary>
        /// Used as a flag to display progress made on the visualisation process whilst each nodes does not have a gameobject assigned
        /// </summary>
        bool allNodesGenerated;

        /// <summary>
        /// The current amount of node objects that have been created
        /// </summary>
        int nodesGenerated;

        /// <summary>
        /// Ensure that the application runs in the background when it is started
        /// </summary>
        void Start()
        {
            Application.runInBackground = true;

            //Set the singleton reference
            Controller = this;
        }

        /// <summary>
        /// Called when the start/stop button is pressed <para/>
        /// If MCTS is not running, then it will be started <para/>
        /// If MCTS is running, this will make it finish early
        /// </summary>
        public void StartStopButtonPressed()
        {
            //Starts or ends MCTS depending on when the button is pressed
            if (mcts == null)
            {
                //Create an empty board instance, which will have whatever game the user chooses assigned to it
                Board board;

                //Assign whatever game board the user has chosen to the board instance
                switch (TreeUIController.GetGameChoice)
                {
                    case 0:
                        displayBoardModel = false;
                        board = new TTTBoard();
                        break;
                    case 1:
                        displayBoardModel = true;
                        board = new C4Board();

                        //Create a C4 Board GameObject and obtain a reference to its BoardModelController Component
                        GameObject boardModel = Instantiate(Resources.Load("C4 Board", typeof(GameObject))) as GameObject;
                        boardModelController = boardModel.GetComponent<BoardModelController>();
                        boardModelController.Initialise();
                        break;
                    case 2:
                        displayBoardModel = false;
                        board = new OthelloBoard();
                        break;
                    default:
                        throw new System.Exception("Unknown game type index has been input");
                }

                //Assign whatever visualisation type the user has chosen
                switch (TreeUIController.GetVisualisationChoice)
                {
                    case 0:
                        visualisationType = VisualisationType.Standard3D;
                        break;
                    case 1:
                        visualisationType = VisualisationType.Disk2D;
                        break;
                    case 2:
                        visualisationType = VisualisationType.Cone;
                        break;
                    default:
                        throw new System.Exception("Unknown visualisation type: encountered");
                }

                //Initialise MCTS on the given game board
                mcts = new TreeSearch<NodeObject>(board);

                //Obtain the time to run mcts for from the input user amount
                timeToRunFor = TreeUIController.GetTimeToRunInput;
                timeLeft = timeToRunFor;

                //Run mcts asyncronously
                RunMCTS(mcts);
                TreeUIController.StartButtonPressed();
            }
            else
            {
                //Stop the MCTS early
                mcts.Finish();
            }
        }

        /// <summary>
        /// If the user has started running MCTS, then display information about it to the UI whilst it generates <para/>
        /// When the MCTS has finished generating, set the position of each <see cref="NodeObject"/> so that they can be rendered on-screen <para/>
        /// When each nodes position has been set, switch to the tree navigation UI
        /// </summary>
        void Update()
        {
            //Don't do anything until the user has started running the MCTS
            if (mcts == null)
                return;

            //While the MCTS is still running, display progress information about the time remaining and the amounts of nodes created to the user
            if (!mcts.Finished)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0)
                {
                    mcts.Finish();
                }
                TreeUIController.UpdateProgressBar((1 - (timeLeft / timeToRunFor)) / 2, "Running MCTS   " + mcts.UniqueNodes + " nodes     " + timeLeft.ToString("0.0") + "s/" + timeToRunFor.ToString("0.0") + "s");
            }

            //Return if the MCTS has not finished being created
            if (!mcts.Finished)
                return;

            //If the MCTS has finished being computed, start to create gameobjects for each node in the tree
            if (!startedVisualisation)
            {
                rootNodeObject = (NodeObject)mcts.Root;
                rootNodeObject.SetPosition(visualisationType);
                StartCoroutine(SetNodePosition(rootNodeObject));
                startedVisualisation = true;
            }

            //Display information on the progress bar about how many node objects have been created, until every node in the tree has its own gameobject
            if (!allNodesGenerated)
            {
                if (nodesGenerated < mcts.UniqueNodes)
                {
                    TreeUIController.UpdateProgressBar(0.5f + ((float)nodesGenerated / mcts.UniqueNodes / 2), "Creating node objects: " + nodesGenerated + "/" + mcts.UniqueNodes);
                }
                else if (nodesGenerated == mcts.UniqueNodes)
                {
                    //If every node has had a gameobject created for it, then switch to the navigation UI and start to render the game tree
                    TreeUIController.SwitchToNavigationUI();
                    Camera.main.GetComponent<LineDraw>().linesVisible = true;
                    Camera.main.GetComponent<TreeCameraControl>().CurrentNode = rootNodeObject;
                    TreeUIController.DisplayNodeInfo(mcts.Root);
                    allNodesGenerated = true;
                    LineDraw.SelectNode(rootNodeObject);
                }
            }
        }

        /// <summary>
        /// Sets the position of a <see cref="NodeObject"/> in the world, and all its children, recursively <para/>
        /// This method is an <see cref="IEnumerator"/> so the tree is given time to be created, instead of the program freezing whilst it creates the tree in one frame
        /// </summary>
        /// <param name="node">The starting node to set the position of</param>
        IEnumerator SetNodePosition(NodeObject node)
        {
            node.SetPosition(visualisationType);
            nodesGenerated++;

            foreach (Node child in node.Children)
            {
                yield return new WaitForSeconds(.1f);

                StartCoroutine(SetNodePosition((NodeObject)child));
            }
        }

        /// <summary>
        /// Runs MCTS until completion asyncronously and then disables the stop button
        /// </summary>
        /// <param name="mcts">The MCTS instance to run</param>
        private static async void RunMCTS(TreeSearch<NodeObject> mcts)
        {
            await Task.Factory.StartNew(() => { while (!mcts.Finished) { mcts.Step(); } }, TaskCreationOptions.LongRunning);
            TreeUIController.StopButtonPressed();
        }
    }
}