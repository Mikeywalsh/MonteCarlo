﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCTS.Visualisation.Menu
{
    /// <summary>
    /// A class which controls the logic behind user interaction with UI elements in the main menu
    /// </summary>
    public class MenuUIController : MonoBehaviour
    {
        /// <summary>
        /// This method will be called when the tree visualisation button is pressed
        /// </summary>
        public void TreeVisualisationButtonPressed()
        {
            SceneManager.LoadScene("TreeVisualisation");
        }

        /// <summary>
        /// This method will be called when the hashing visualisation button is pressed
        /// </summary>
        public void HashingVisualisationButtonPressed()
        {
            SceneManager.LoadScene("HashingVisualisation");
        }

        /// <summary>
        /// This method will be called when the play connect four button is pressed
        /// </summary>
        public void PlayConnectFourButtonPressed()
        {
            SceneManager.LoadScene("PlayConnect4");
        }
    }
}