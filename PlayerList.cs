﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pictionary
{
    public partial class PlayerList : UserControl
    {
        List<Player> players;
        public PlayerList()
        {
            players = new List<Player>();
            InitializeComponent();
        }

        // Add a new player to game
        public void AddPlayer(string name)
        {
            Player player = new Player(name);
            players.Add(player);
            flpPlayers.Controls.Add(player);
        }

        // Remove a player from game
        public void RemovePlayer(string name)
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].GetName() == name)
                {
                    players.RemoveAt(i);
                    flpPlayers.Controls.RemoveAt(i);
                }
            }
        }

        public void NewGame()
        {
            foreach (Player p in players)
            {
                p.NewGame();
            }
        }

        // Return the list of players
        public List<Player> GetPlayers()
        {
            return players;
        }

        // Set player to ready
        public void Ready(string name)
        {
            foreach (Player p in players)
            {
                if (p.GetName() == name)
                {
                    p.Ready();
                }
            }
        }

        // Mark player as "Drawing"
        public void SetDrawing(string name)
        {
            foreach (Player p in players)
            {
                if (p.GetName() == name)
                {
                    p.SetDrawing();
                }
            }
        }

        // Mark player as "Choosing word"
        public void SetChoosingWord(string name)
        {
            foreach (Player p in players)
            {
                if (p.GetName() == name)
                {
                    p.SetChoosingWord();
                }
            }
        }

        public void EndRound()
        {
            foreach (Player p in players)
            {
                p.EndRound();
            }
        }

        // Update score for player
        public void AddScore(string player, int score)
        {
            foreach(Player p in players)
            {
                if (p.GetName() == player)
                {
                    p.AddScore(score);
                }
            }
        }

        // Reset score for all players (new round)
        public void ResetScore()
        {
            foreach (Player p in players)
            {
                p.ResetScore();
            }
        }
    }
}
