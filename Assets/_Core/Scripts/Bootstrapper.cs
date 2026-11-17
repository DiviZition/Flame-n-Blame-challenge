using UnityEngine;
using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.UseCases;
using Flame_n_Blame_Challenge.Domain;
using System;

namespace Flame_n_Blame_Challenge
{

    public class Bootstrapper : MonoBehaviour
    {
        private GameUpdater _gameUpdater;
        private GamePlaySession _gameCore;

        private void Start()
        {
            _gameUpdater = new GameUpdater();
            _gameCore = new GamePlaySession(_gameUpdater);
        }

        private void Update()
        {
            _gameUpdater.Update(Time.time, Time.deltaTime);
        }
    }
}

namespace Flame_n_Blame_Challenge.UseCases
{

    public class GamePlaySession
    {
        private GameUpdater _updater;

        public GamePlaySession(GameUpdater updater)
        {
            _updater = updater;
        }

        public void StartGamePlay()
        {
            IPlayerInput playerInput = new PlayerInputMain();
            // Spawn the player;
            // Provide input;
            // Spawn the level;
            // Initialize level systems;
            // Initialize Objectieves?;
        }
    }
}

namespace Flame_n_Blame_Challenge.Domain 
{ 
    public class PlayerState
    {
        public IPlayerInput _input;
        // PlayerWorldInteract
        // PlayerItemControl
        // PlayerMovement
        // PlayerHealth
        // PlayerStatuses

        public PlayerState(IPlayerInput input)
        {
            _input = input;
        }
    }

    public class PlayerMovement
    {

    }
}
