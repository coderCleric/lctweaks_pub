using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCTweaks
{
    public class DotColorController : MonoBehaviour
    {
        public PlayerControllerB player = null;
        private Material dotMat = null;
        private Material viewMat = null;

        private static Color FULL_HEALTH = new Color(0, 1, 0);
        private static Color LITTLE_HURT = new Color(1, 0.75f, 0);
        private static Color FAIRLY_HURT = new Color(1, 0.65f, 0);
        private static Color PRETTY_HURT = new Color(1, 0.58f, 0);
        private static Color VERY_HURT = new Color(1, 0.48f, 0);
        private static Color CRITICALLY_HURT = new Color(1, 0.33f, 0);
        private static Color DEAD = new Color(1, 0, 0);
        private static Color col = FULL_HEALTH;

        //On awake, nuke the animator of the dot
        private void Awake()
        {
            if (GetComponent<Animator>() != null)
                GetComponent<Animator>().enabled = false;
        }

        //Every frame, determine the color to use
        private void Update()
        {
            //If the player exists, determine color based on health
            if (player != null)
            {
                //If the player is dead, make them red
                if (player.isPlayerDead)
                    col = DEAD;

                //If player is alive, use health to determine color
                else
                {
                    if (player.health >= 100) //Full health
                        col = FULL_HEALTH;
                    else if (player.health >= 80) //Little hurt
                        col = LITTLE_HURT;
                    else if (player.health >= 60) //Fairly hurt
                        col = FAIRLY_HURT;
                    else if (player.health >= 40) //Pretty hurt
                        col = PRETTY_HURT;
                    else if (player.health >= 20) //Very hurt
                        col = VERY_HURT;
                    else if (player.health >= 0) //Critically hurt
                        col = CRITICALLY_HURT;
                    else //Dead
                        col = DEAD;
                }
            }

            //If no player part, just make it red
            else
                col = DEAD;

            //Grab the materials, if needed
            if (dotMat == null)
            {
                dotMat = GetComponent<Renderer>().material;
                viewMat = transform.Find("MapDirectionIndicator").GetComponent<Renderer>().material;
                if (player != null)
                    LCTweaks.DebugLog("Storing instanced material for player " + player.name);
                else
                    LCTweaks.DebugLog("Storing instanced material for nonplayer object");
            }

            //Apply the colors
            viewMat.color = col;
            viewMat.SetColor("_EmissiveColor", col);
            dotMat.color = col;
            dotMat.SetColor("_EmissiveColor", col);
        }
    }
}
