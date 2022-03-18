using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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

    // How Approx-Asserts can deviate without it resulting to FAIL
    float differenceTolerance = 0.001f;

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
        float timeout = 3f; // Stop test if timeout has passed

        // Releasing space-key after interruptTime should result in player never getting higher than shortJumpHeight
        float interruptTime = 0.15f;
        float shortJumpHeight = 1f;

        float maxHeight = origPos.y + shortJumpHeight; // Player should never get above this
        Debug.Log($"maxHeight (origPos + shortJumpHeight): {origPos.y + shortJumpHeight}");

        // Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(interruptTime);
        // Release after interrupt time
        input.Release(keyboard.spaceKey);

        // Check until player is back on original ground position
        while (!Mathf.Approximately(player.position.y, origPos.y))
        {
            yield return null;

            // Stop loop if timeout has passed
            timeout -= Time.deltaTime;
            if (timeout < 0) Assert.Fail();

            Assert.Less(player.position.y, maxHeight);
        }
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

    // ========== Test Set 3 Improvements ========== 

    // Also improved InteeruptJump-test to kill mutations 32, 33

    // Verifies that enemy sprite is flipped when it changes facing direction
    // Should kill mutation 4 and mutation 5
    [UnityTest]
    public IEnumerator FlipEnemySprite()
    {
        float timeout = 10f;
        // Test not started until enemy is this far away from PatrolPath start/end
        float startPointThreshold = 0.5f; 
        // Move player to position where it's close to an enemy
        Vector3 newStartPos = new Vector3(19, 1, 1);
        player.position = newStartPos;

        // Get the needed enemy components
        GameObject enemy = GetClosestEnemy(player.position);
        SpriteRenderer enemySr = enemy.GetComponent<SpriteRenderer>();
        GameObject pp = GetClosestPatrolPath(player.position);

        // Get PatrolPath start and end positions as scene coordinates
        float ppStartX = pp.GetComponent<PatrolPath>().startPosition.x + pp.transform.position.x;
        float ppEndX = pp.GetComponent<PatrolPath>().endPosition.x + pp.transform.position.x;

        // Wait until enemy is not close to end points of patrol path to avoid weird test fails
        while (enemy.transform.position.x - ppEndX > -startPointThreshold || enemy.transform.position.x - ppStartX < startPointThreshold)
        {
            yield return null;
            timeout -= Time.deltaTime;
            if (timeout < 0)
            {
                Debug.Log("Timeout while waiting for enemy to be further away from patrol path end points");
                Assert.Fail();
                break;
            }
        }

        // Save enemy startPosX
        float startPosX = enemy.transform.position.x;

        // Wait until moving left
        yield return new WaitUntil(() => enemy.transform.position.x < startPosX);
        Debug.Log("Enemy moving left");
        // Assert that sprite is flipped
        Assert.IsTrue(enemySr.flipX);

        // Wait until moving right
        yield return new WaitUntil(() => enemy.transform.position.x > startPosX);
        Debug.Log("Enemy moving right");
        // Assert that sprite is not flipped
        Assert.IsTrue(!enemySr.flipX);
    }

    // Verifies that a patrolling enemy doesn't jump
    // Should kill mutation 6 and mutation 7
    [UnityTest]
    public IEnumerator PatrollingEnemyNoJump()
    {
        float monitorForJump = 5f; // How long test should run and keep checking that enemy isn't jumping
        float tolerance = 0.01f; // How much difference is tolerated in enemy position or height

        // Move player to position where it's close to a patrolling enemy
        Vector3 newStartPos = new Vector3(19, 1, 1);
        player.position = newStartPos;

        // Get the needed enemy components
        GameObject enemy = GetClosestEnemy(player.position);
        CapsuleCollider2D enemyCollider = enemy.GetComponent<CapsuleCollider2D>();

        // Get enemy values which show when enemy jumps
        float enemyStartY = enemy.transform.position.y;
        float enemyStartColliderHeight = enemyCollider.size.y;

        while(monitorForJump > 0)
        {
            // Assert that enemy.y and collider.y don't grow --> enemy hasn't jumped
            Assert.Less(enemyCollider.size.y, enemyStartColliderHeight + tolerance);
            Assert.Less(enemy.transform.position.y, enemyStartY + tolerance);

            // Wait for next frame
            monitorForJump -= Time.deltaTime;
            yield return null;
        }
    }

    // Verifies player x-velocity is reset when hitting something
    [UnityTest]
    public IEnumerator PlayerStopRunWhenColliding()
    {
        float moveTimeUntilWall = 2.5f;
        float monitorSpeedDuration = 2f;

        PlayerController pc = player.GetComponent<PlayerController>();

        // Move player to run against wall
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(moveTimeUntilWall);

        // Monitor for a duration
        while (monitorSpeedDuration > 0)
        {
            // Assert that player x-speed stays at 0 while trying to run against a wall
            Assert.AreEqual(0, pc.velocity.x);

            // Wait for next frame
            monitorSpeedDuration -= Time.deltaTime;
            yield return null;
        }
    }

    // Verifies that player y-velocity is reset when hitting something in the air
    // Should kill mutation 17
    [UnityTest]
    public IEnumerator PlayerStopJumpWhenColliding()
    {
        float maxHeightUnderTile = -0.27f; // Player can't be above this under the tile
        float monitorHeightDuration = 3f; // How long test should run and keep checking that player isn't above tile

        // Move player to position which is under a tile
        Vector3 underTilePos = new Vector3(19, -0.575f, 1);
        player.position = underTilePos;

        DisableEnemies();

        // Press jump and movement so player jumps and moves forward from under the tile while in air
        input.Press(keyboard.spaceKey);
        input.Press(keyboard.dKey);

        // Assert that player doesn't go above tile at any point because hitting the tile will reset player y-speed
        while (monitorHeightDuration > 0)
        {
            Assert.Less(player.transform.position.y, maxHeightUnderTile);

            // Wait for next frame
            monitorHeightDuration -= Time.deltaTime;
            yield return null;
        }

        input.Release(keyboard.spaceKey);
        input.Release(keyboard.dKey);
        yield return null;
    }

    // Verifies that player bounces up a little after killing enemy by jumping on it
    // Should kill mutation 18
    [UnityTest]
    public IEnumerator PlayerBounceAfterEnemyKill()
    {
        float timeout = 3f;
        float bounceHeight = 0.1f;
        float bounceDuration = 0.2f;
        float jumpDuration = 1f;

        // Get first enemy found in the scene and reset its movement so it will stay still under player
        GameObject enemy = GameObject.Find("Enemy");
        CapsuleCollider2D enemyCollider = enemy.GetComponent<CapsuleCollider2D>();
        enemy.GetComponent<EnemyController>().path = null;
        enemy.GetComponent<AnimationController>().move = Vector2.zero;
        float enemyStartY = enemy.transform.position.y;

        // Start Jump
        input.Press(keyboard.spaceKey);
        // Wait until jump is half way done and move enemy to player's original position
        yield return new WaitForSeconds(jumpDuration / 2);
        enemy.transform.position = origPos;
        // Release Jump-key after whole jumpDuration has passed to drop on top of enemy
        yield return new WaitForSeconds(jumpDuration / 2);
        input.Release(keyboard.spaceKey);

        // Wait until enemy has died
        while(enemyCollider.enabled != false)
        {
            yield return null;
            timeout -= Time.deltaTime;
            if (timeout < 0)
            {
                Debug.Log("Timeout while waiting for enemy to die (collider.enabled == false)");
                Assert.Fail();
                break;
            }
        }
        Vector3 playerPosAtKill = player.transform.position;
        Debug.Log($"PlayerPos at kill: {playerPosAtKill.y}");

        // Wait for bounce and assert that player position is at bounce height after it
        yield return new WaitForSeconds(bounceDuration);
        Debug.Log($"PlayerPos at bounce duration: {player.transform.position.y}");
        Assert.Greater(player.transform.position.y, playerPosAtKill.y + bounceHeight);
    }

    // Verifies that different tabs can be opened in Pause-menu
    // Should kill mutation 22
    [UnityTest]
    public IEnumerator OpenMenuTabs()
    {
        // Open menu and get UI-buttons
        input.PressAndRelease(keyboard.escapeKey);
        Button settingsButton = GameObject.Find("SettingsButton").GetComponent<Button>();
        Button controlsButton = GameObject.Find("ControlsButton").GetComponent<Button>();
        Button creditsButton = GameObject.Find("CreditsButton").GetComponent<Button>();

        // Click through all buttons and check that correct tab is open after it
        // Clicks should be simulated through Mouse-input but that doesn't seem to work
        settingsButton.onClick.Invoke();
        GameObject settingsScreen = GameObject.Find("Settings");
        Assert.IsTrue(settingsScreen.activeInHierarchy);

        controlsButton.onClick.Invoke();
        GameObject controlsScreen = GameObject.Find("Controls");
        Assert.IsTrue(controlsScreen.activeInHierarchy);

        creditsButton.onClick.Invoke();
        GameObject creditsScreen = GameObject.Find("Credits");
        Assert.IsTrue(creditsScreen.activeInHierarchy);

        yield return null;
    }

    // Verifies that patrolling enemy moves at correctish speed
    // Should kill mutation 28
    [UnityTest]
    public IEnumerator EnemyPatrolSpeed()
    {
        float travelTime = 1.75f; // How long it should take for enemy to travel this patrol path
        float tolerance = 0.01f; // How much difference is tolerated in travelTime

        // Move player to position where it's close to a patrolling enemy
        Vector3 newStartPos = new Vector3(19, 1, 1);
        player.position = newStartPos;

        // Get the needed enemy and patrolPath components
        GameObject enemy = GetClosestEnemy(player.position);
        PatrolPath pp = GetClosestPatrolPath(player.position).GetComponent<PatrolPath>();
        float ppLength = Vector2.Distance(pp.endPosition, pp.startPosition);

        float travelledDistance = 0;
        Vector2 lastPos = enemy.transform.position;

        // Travel until ppLength has been travelled
        while(travelledDistance < ppLength)
        {
            // Reduce travelTime left and add travelledDistance after every frame
            yield return null;
            travelTime -= Time.deltaTime;
            travelledDistance += Vector2.Distance(lastPos, enemy.transform.position);
            lastPos = enemy.transform.position;

            // Immediately fail test if too much time was passed
            if (travelTime < -tolerance)
            {
                Debug.Log($"Traveling patrolPath took too long, distance left to travel: {ppLength - travelledDistance}");
                Assert.Fail();
            }
        }

        // Assert that approx travelTime passed to travel ppLength
        Debug.Log("travelTime left at the end was: " + travelTime);
        UnityAssert.AreApproximatelyEqual(travelTime, 0, tolerance);
    }

    // Verifies that player sprite is flipped when it changes facing direction
    // Should kill mutation 34 and mutation 35
    [UnityTest]
    public IEnumerator FlipPlayerSprite()
    {
        SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();

        // Move player left and assert that sprite is flipped
        input.Press(keyboard.aKey);
        yield return null;
        Assert.IsTrue(playerSr.flipX);
        input.Release(keyboard.aKey);

        // Move player right and assert that sprite is not flipped
        input.Press(keyboard.dKey);
        yield return null;
        Assert.IsTrue(!playerSr.flipX);
        input.Release(keyboard.dKey);
    }

    // Verifies that player run animation is shown when player moves on the ground
    // Should kill mutation 36
    [UnityTest]
    public IEnumerator PlayerRunAnimation()
    {
        string runAnimationName = "PlayerRun";
        Animator playerAnim = player.GetComponent<Animator>();
        AnimatorClipInfo acInfo;

        // Move player left and assert that run animation is playing after a brief wait
        input.Press(keyboard.aKey);
        yield return new WaitForSeconds(0.5f);
        acInfo = playerAnim.GetCurrentAnimatorClipInfo(0)[0];
        Assert.AreEqual(acInfo.clip.name, runAnimationName);
        yield return new WaitForSeconds(0.5f);
        input.Release(keyboard.aKey);

        // Move player right and assert that run animation is playing after a brief wait
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(0.5f);
        acInfo = playerAnim.GetCurrentAnimatorClipInfo(0)[0];
        Assert.AreEqual(acInfo.clip.name, runAnimationName);
        yield return new WaitForSeconds(0.5f);
        input.Release(keyboard.dKey);
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

    // Returns PatrolPath-object that is closest to given position in scene
    private GameObject GetClosestPatrolPath(Vector3 pos)
    {
        GameObject[] patrolPaths = GameObject.FindGameObjectsWithTag("PatrolPath");
        Transform closestTransform = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject pp in patrolPaths)
        {
            float dist = Vector3.Distance(pos, pp.transform.position);
            if (dist < minDistance)
            {
                closestTransform = pp.transform;
                minDistance = dist;
            }
        }
        return closestTransform.gameObject;
    }

    // Returns Enemy-object that is closest to given position in scene
    private GameObject GetClosestEnemy(Vector3 pos)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestTransform = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            float dist = Vector3.Distance(pos, e.transform.position);
            if (dist < minDistance)
            {
                closestTransform = e.transform;
                minDistance = dist;
            }
        }
        return closestTransform.gameObject;
    }
}
