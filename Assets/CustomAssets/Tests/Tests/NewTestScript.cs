using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Platformer.Mechanics;

public class NewTestScript : InputTestFixture
{
    Keyboard keyboard;
    string mainSceneName = "SampleScene";

    // Setup that runs before every test
    public override void Setup()
    {
        base.Setup();

        keyboard = InputSystem.AddDevice<Keyboard>();
        SceneManager.LoadScene(mainSceneName);
    }

    // Verifies that test set up initialized correctly
    [Test]
    public void TestSetup()
    {
        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Keyboard>());
    }

    // Verifies that player character exists in the game
    [UnityTest]
    public IEnumerator GameStart()
    {
        yield return new WaitForEndOfFrame();
        GameObject player = GameObject.Find("Player");
        Assert.NotNull(player);
        Assert.True(player.activeInHierarchy);
    }

    // Verifies that player can be moved to both directions
    [UnityTest]
    public IEnumerator PlayerMoves()
    {
        yield return new WaitForEndOfFrame();
        GameObject player = GameObject.Find("Player");

        // Save original position for later comparison
        Vector2 origPos = player.transform.position;

        // Move forward
        Press(keyboard.dKey);
        yield return new WaitForSeconds(0.5f);
        Release(keyboard.dKey);
        // Save new position and compare to original position
        Vector2 fwPos = player.transform.position;
        Assert.Greater(fwPos.x, origPos.x);

        // Move backward
        Press(keyboard.aKey);
        yield return new WaitForSeconds(0.5f);
        Release(keyboard.aKey);
        // Save new position and compare to forward position
        Assert.Less(player.transform.position.x, origPos.x);
    }
}
