using Platformer.Core;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the Jump Input is deactivated by the user, cancelling the upward velocity of the jump.
    /// </summary>
    /// <typeparam name="PlayerStopJump"></typeparam>
    public class PlayerStopJump : Simulation.Event<PlayerStopJump>
    {
        public override void Execute()
        {
            // CUSTOM ADDITION TO TRY AND PREVENT CREAM FROM CRASHING AT THIS FILE
            Debug.Log("PlayerStopJump executed");
        }
    }
}