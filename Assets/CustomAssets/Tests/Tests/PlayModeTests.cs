using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Assertions.Comparers;
using Platformer.Mechanics;
using UnityAssert = UnityEngine.Assertions.Assert;

public class PlayModeTests
{
    private InputTestFixture input = new InputTestFixture();
    Keyboard keyboard;
    Transform player;
    Vector3 origPos;
    string mainSceneName = "SampleScene";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        //SceneManager.LoadScene(mainSceneName);
    }

    // Setup that runs before every test
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Execute InputTextFixture Setup
        input.Setup();

        keyboard = InputSystem.AddDevice<Keyboard>();
        SceneManager.LoadScene(mainSceneName);
        yield return new WaitForSeconds(0.1f); // Wait for Awake(), Start()...
        
        player = GameObject.Find("Player").transform;
        yield return new WaitForSeconds(1f); // Wait for player to be in idle state (land after spawn drop)

        // Save starting position for later comparison
        origPos = player.position;
        Debug.Log($"origPos: {origPos}");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        input.TearDown();
        yield return null;
    }

    // ========== MOVEMENT TESTS ==========

    // Verifies that player can be moved to both directions
    [UnityTest]
    public IEnumerator Move()
    {
        // Move forward
        input.Press(keyboard.dKey);
        Debug.Log("Pressed keyboard key started");
        yield return new WaitForSeconds(0.5f);
        input.Release(keyboard.dKey);

        // Save new position and compare to original position
        Vector2 fwPos = player.position;
        Debug.Log($"Position after forward move: {fwPos}");
        Assert.Greater(fwPos.x, origPos.x);

        // Move backward
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(0.5f);
        input.Release(keyboard.aKey);

        // Save new position and compare to forward position
        Vector2 bwPos = player.position;
        Debug.Log($"Position after backward move: {bwPos}");
        Assert.Less(bwPos.x, fwPos.x);
    }

    // Verifies that player moves about the correct distance in a given amount of time
    // Disabled for now, because doesn't really test any 'feature'
    /*[UnityTest]
    public IEnumerator PlayerMoveSpeed()
    {
        float moveTime = 1f;
        float moveDistance = 2.9f; // How much distance should be moved in moveTime
        float epsilon = 0.05f; // How much difference between xLeftPos and actual player.position.x is allowed

        // Move backward
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(moveTime);
        input.Release(keyboard.aKey);

        // Save new position and assert that it is (very close to) expected
        Vector2 releasePos = player.position;
        Debug.Log($"X-position after release: {releasePos.x}");
        UnityAssert.AreApproximatelyEqual(origPos.x - moveDistance, releasePos.x, epsilon);

        // Move forward again
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(moveTime);
        input.Release(keyboard.dKey);

        // Save new position and assert that it is (very close to) original position again
        releasePos = player.position;
        Debug.Log($"X-position after release: {releasePos.x}");
        UnityAssert.AreApproximatelyEqual(origPos.x, releasePos.x, epsilon);
    }*/


    // ========== JUMP TESTS ==========

    // Verifies that player can do a full jump (~ +1.75 to y-coordinate)
    [UnityTest]
    public IEnumerator JumpFullHeight()
    {
        // Achieving full jump height should take about 0.65s
        float fullJumpTime = 0.65f;
        float fullJumpHeight = 1.75f;

        // Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(fullJumpTime);
        // Release after fullJumpTime
        input.Release(keyboard.spaceKey);

        // Save new position and compare to original position
        Vector2 releasePos = player.position;
        Debug.Log($"Y-position at release was: {releasePos.y}");
        Assert.Greater(releasePos.y, origPos.y + fullJumpHeight);
    }

    // Verifies that player can interrupt jump by releasing space key
    [UnityTest]
    public IEnumerator InterruptJump()
    {
        // Releasing space-key after interruptTime should result in player never getting higher than shortJumpHeight
        float interruptTime = 0.15f;
        float shortJumpHeight = 1f;
        // AT LEAST how long it takes for player to start dropping after releasing jump-key
        float dropStartDuration = 0.05f;
        Debug.Log($"origPos.y + maxJumpHeight: {origPos.y + shortJumpHeight}");

        // Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(interruptTime);
        // Release after interrupt time
        input.Release(keyboard.spaceKey);

        // Save highest position and compare to original position, should be less than shortJumpHeight
        Vector2 releasePos = player.position;
        Debug.Log($"Y-position at release was: {releasePos.y}");
        Assert.Less(releasePos.y, origPos.y + shortJumpHeight);

        // Assert that jump 'overflows' for a little while after releasing jump-key
        yield return new WaitForSeconds(dropStartDuration);
        Debug.Log($"Y-position after dropStartDuration: {player.position.y}");
        Assert.Greater(player.position.y, releasePos.y);

        // Wait until player starts dropping down
        while (player.position.y >= releasePos.y)
        {
            yield return null;
        }
        Debug.Log("Player started dropping down");

        // Init lastFramePos as high number at start
        Vector2 lastFramePos = Vector2.up * 10;
        // Check that position gets lower in each frame after dropping down, until player is on the ground again
        while (!Mathf.Approximately(player.position.y, origPos.y))
        {
            Assert.Less(player.position.y, lastFramePos.y);
            lastFramePos = player.position;
            yield return null;
        }
        Debug.Log("Player landed");
    }

    // Verifies that player can't jump again until they have landed from previous jump
    [UnityTest]
    public IEnumerator NoDoubleJump()
    {
        // Releasing space-key after interruptTime should result in player never getting higher than maxJumpHeight
        float reJumpTime = 0.5f;
        float doubleJumpHeight = 2.5f;

        // Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(reJumpTime);
        // Release after reJumpTime
        input.Release(keyboard.spaceKey);
        // Instantly try to jump again
        input.Press(keyboard.spaceKey);
        Debug.Log($"Y-position at reJump: {player.position.y}");

        // Assert that player y-position never gets to doubleJumpHeight
        while (player.position.y > origPos.y)
        {
            Assert.Less(player.position.y, origPos.y + doubleJumpHeight);
            yield return null;               
        }
        Debug.Log("Player landed");
    }


    // ========== DEATH TESTS ==========

    // Verifies that player dies when walking into an enemy
    // Only way to verify death (beside animation) is to check that player is moved to starting position again?
    [UnityTest]
    public IEnumerator KilledByEnemy()
    {
        float moveTime = 3f;
        float deathAnimationTime = 3f;
        float spawnAnimationTime = 1.5f;

        // Move forward, enemy should be encountered before moveTime is over
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(moveTime);
        input.Release(keyboard.dKey);

        // Wait until death animation is done and player is spawned
        yield return new WaitForSeconds(deathAnimationTime + spawnAnimationTime);

        // Assert that player is spawned to starting position again
        Vector3 endPos = player.position;
        Debug.Log($"Ending pos: {endPos}");
        AssertVectorsApproxEqual(endPos, origPos);
    }

    // Verifies that player dies falling out of map
    // Only way to verify death (beside animation) is to check that player is moved to starting position again?
    [UnityTest]
    public IEnumerator KilledByMapBounds()
    {
        float moveTime = 4.5f; // How long should player press movement button
        float deathAnimationTime = 3f;
        float spawnAnimationTime = 1.5f;

        // Disable all enemies to prevent dying from them
        DisableEnemies();

        // Move forward, player should fall to death before moveTime is over
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(moveTime);
        input.Release(keyboard.dKey);

        // Wait until fall and death animation is done and player is spawned
        yield return new WaitForSeconds(deathAnimationTime + spawnAnimationTime);

        // Assert that player is spawned to starting position again
        Vector3 endPos = player.position;
        Debug.Log($"Ending pos: {endPos}");
        AssertVectorsApproxEqual(endPos, origPos);
    }

    // ========== MENU ==========

    // Verifies that main menu can be opened and closed
    [UnityTest]
    public IEnumerator OpenMainMenu()
    {
        // Open menu
        input.PressAndRelease(keyboard.escapeKey);
        GameObject menuButtonsParent = GameObject.Find("Buttons");
        GameObject menuMainScreen = GameObject.Find("Main");

        // Assert that main screen of menu is active
        Assert.IsTrue(menuMainScreen.activeInHierarchy);
        // Assert that all menu buttons are active
        foreach (Transform b in menuButtonsParent.transform)
        {
            Assert.IsTrue(b.gameObject.activeInHierarchy);
        }

        // Wait one frame
        yield return null;

        // Close menu
        input.PressAndRelease(keyboard.escapeKey);

        // Assert that main screen of menu is inactive
        Assert.IsFalse(menuMainScreen.activeInHierarchy);
        // Assert that all menu buttons are inactive
        foreach (Transform b in menuButtonsParent.transform)
        {
            Assert.IsFalse(b.gameObject.activeInHierarchy);
        }
    }

    // Verifies that game is paused when player open main menu and continued when player closes it
    [UnityTest]
    public IEnumerator PauseGame()
    {
        float jumpTime = 0.5f;
        float dropTime = 1f;

        // Jump 
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(jumpTime);
        input.Release(keyboard.spaceKey);
        Vector3 jumpPos = player.transform.position;

        // Open menu and wait
        input.PressAndRelease(keyboard.escapeKey);
        yield return new WaitForSecondsRealtime(dropTime);
        // Assert that player is still in same position and hasn't dropped down during pause time
        Assert.AreEqual(jumpPos, player.transform.position);

        // Close menu and wait
        input.PressAndRelease(keyboard.escapeKey);
        yield return new WaitForSeconds(dropTime);

        // Assert that player has dropped back to original position
        AssertVectorsApproxEqual(origPos, player.transform.position);
    }


    // ========== OTHER TESTS ==========

    // Verifies that enemies die when player jumps on them
    // Enemy drops outside of camera view when dying so best way to verify enemy death is to verify y-coordinate?
    [UnityTest]
    public IEnumerator KillEnemy()
    {
        float jumpDuration = 1f;    // How long will player press jump-key
        float enemyDeathTime = 2f;
        float yPosOutOfCamera = -4f; // Y-coordinate which isn't shown on camera

        // Get first enemy found in the scene and reset its movement so it will stay still under player
        GameObject enemy = GameObject.Find("Enemy"); 
        enemy.GetComponent<EnemyController>().path = null;
        enemy.GetComponent<AnimationController>().move = Vector2.zero;

        // Start Jump
        input.Press(keyboard.spaceKey);
        // Wait until jump is half way done and move enemy to player's original position
        yield return new WaitForSeconds(jumpDuration / 2);
        enemy.transform.position = origPos;
        // Release Jump-key after whole jumpDuration has passed to drop on top of enemy
        yield return new WaitForSeconds(jumpDuration / 2);
        input.Release(keyboard.spaceKey);

        // Assert that enemy has dropped outside of camera view after a while
        yield return new WaitForSeconds(enemyDeathTime);
        Vector2 endPos = enemy.transform.position;
        Debug.Log($"Enemy end position: {endPos}");
        Assert.Less(enemy.transform.position.y, yPosOutOfCamera);
    }

    // Verifies that player can collect tokens found in the map
    [UnityTest]
    public IEnumerator CollectToken()
    {
        float jumpDuration = 1f; // How long will player press jump-key
        
        // Get first token found in the scene and move it above player
        GameObject token = GameObject.Find("Token");
        token.transform.position = origPos + Vector3.up;

        // Execute Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(jumpDuration);
        input.Release(keyboard.spaceKey);

        // Assert that token has been disabled after being collected
        Assert.IsFalse(token.activeSelf);
    }

    // Verifies that game ends when player reaches goal
    // Only way to verify game end beside animation, is to check that player can't move anymore
    [UnityTest]
    public IEnumerator ReachGoal()
    {
        float moveDuration = 1f; // How long will player press jump-key

        // Get first goal zone found in the scene and move it to the left of player
        GameObject goalZone = GameObject.Find("Victory");
        goalZone.transform.position = origPos + Vector3.left;

        // Move to goal
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(moveDuration);
        input.Release(keyboard.aKey);
        Vector3 endPos = player.transform.position;

        // Verify that player can't move anymore

        // Assert that can't move right
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(moveDuration);
        input.Release(keyboard.dKey);
        AssertVectorsApproxEqual(endPos, player.transform.position);

        // Assert that can't move left
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(moveDuration);
        input.Release(keyboard.aKey);
        AssertVectorsApproxEqual(endPos, player.transform.position);

        // Assert that can't jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(moveDuration);
        input.Release(keyboard.spaceKey);
        AssertVectorsApproxEqual(endPos, player.transform.position);
    }

    // ========== HELPER METHODS ========== 

    // Disables all enemies from map
    private void DisableEnemies()
    {
        Transform enemiesParent = GameObject.Find("Enemies").transform;
        foreach (Transform o in enemiesParent)
        {
            o.gameObject.SetActive(false);
        }
        Debug.Log("Disabled all enemies");
    }

    // Asserts that 2 vectors are approximately equal
    private void AssertVectorsApproxEqual(Vector3 v1, Vector3 v2, float tolerance = 0.01f)
    {
        UnityAssert.AreApproximatelyEqual(v1.x, v2.x, tolerance);
        UnityAssert.AreApproximatelyEqual(v1.y, v2.y, tolerance);
        UnityAssert.AreApproximatelyEqual(v1.z, v2.z, tolerance);
    }

}
