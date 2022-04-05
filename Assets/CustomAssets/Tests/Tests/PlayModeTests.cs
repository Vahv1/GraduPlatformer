using System;
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
using UnityEngine.UI;

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
    // Updated version kills at least mutations UOI17 (After_UOI140) and UOI27 (After_UOI140)
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
        float moveDuration = 1f; // How long will player move

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

    // ========== Test Set 2 Improvements ========== 

    // Verifies that player has HP again after respawning
    // Kills at least mutations AOR13, AOR14, AOR15, AOR16, UOI121, UOI123, UOI126
    [UnityTest]
    public IEnumerator SpawnWithHealth()
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

        // Assert that player has HP again
        Assert.IsTrue(player.GetComponent<Health>().IsAlive);
    }

    // Verifies that player can collect multiple tokens in quick succession
    // Kills at least mutations ASR11
    [UnityTest]
    public IEnumerator CollectMultipleTokens()
    {
        float jumpDuration = 1.5f; // How long will player press jump-key
        int tokenAmt = 6;
        float tokenGap = 0.25f;
        List<GameObject> tokenObjects = new List<GameObject>();

        // Get multiple tokens from scene and move them in a line above player
        TokenInstance[] tokenInstances = GameObject.FindObjectsOfType<TokenInstance>();
        for (int i = 0; i < tokenAmt; i++)
        {
            tokenObjects.Add(tokenInstances[i].gameObject);
            tokenInstances[i].transform.position = origPos + (Vector3.up * 0.5f) + (Vector3.up * tokenGap * i);
        }

        // Execute Jump
        input.Press(keyboard.spaceKey);
        yield return new WaitForSeconds(jumpDuration);
        input.Release(keyboard.spaceKey);

        // Assert that tokens have been disabled after being collected
        foreach(GameObject t in tokenObjects)
        {
            Assert.IsFalse(t.activeSelf);
        }
    }

    // Verifies that idling enemy stays still
    // Should kill at least mutations LCR1, UOI18, UOI20
    [UnityTest]
    public IEnumerator IdleEnemyStaysStill()
    {
        float monitorPosDuration = 5f; // How long test should run and keep that enemy isn't moving

        // Get enemy and remove possible patrol path to make it idle
        GameObject enemy = GetClosestEnemy(player.position);
        enemy.GetComponent<EnemyController>().path = null;
        Vector3 enemyOrigPos = enemy.transform.position;

        while(monitorPosDuration > 0)
        {
            // Assert that enemy position has stayed the same
            AssertVectorsApproxEqual(enemy.transform.position, enemyOrigPos);

            // Wait for next frame
            monitorPosDuration -= Time.deltaTime;
            yield return null;
        }
    }

    // Verifies that token animation is ran at correct speed
    // Should kill at least mutations AOR6, ASR1, ASR2, ASR3, ASR4, ROR8, UOI38, UOI41
    [UnityTest]
    public IEnumerator TokenAnimationCorrectSpeed()
    {
        float tolerance = 0.01f; // How much full animation length is allowed to differ from expected
        float startLoopTimeout = 2f;

        // Set needed objects
        float tokenFrameRate = GameObject.FindObjectOfType<TokenController>().frameRate;
        GameObject token = GameObject.Find("Token");
        Sprite lastSprite = token.GetComponent<SpriteRenderer>().sprite;
        Sprite[] tokenSprites = token.GetComponent<TokenInstance>().idleAnimation;

        // List of sprites that have already been displayed
        List<Sprite> alreadyDisplayedSprites = new List<Sprite>();

        // Time it should take to go through all animation frames. Note -1 because dont need to wait for first frame.
        float rotationTime = (1 / tokenFrameRate) * (tokenSprites.Length - 1);

        // Wait at start until new animation frame has just been changed
        while(lastSprite == token.GetComponent<SpriteRenderer>().sprite && startLoopTimeout > 0)
        {
            startLoopTimeout -= Time.deltaTime;
            yield return null;
        }

        // Do until all animation frames have been displayed
        while(alreadyDisplayedSprites.Count < tokenSprites.Length)
        {
            // Wait until next frame and set variables again
            yield return null;
            rotationTime -= Time.deltaTime;
            Sprite currentSprite = token.GetComponent<SpriteRenderer>().sprite;

            // If sprite was changed from last, add new sprite to Displayed-list
            if (currentSprite != lastSprite)
            {
                alreadyDisplayedSprites.Add(currentSprite);
                lastSprite = currentSprite;
            }

            // If too long has passed and all sprites are still not displayed, FAIL test
            if (rotationTime < -tolerance)
            {
                Debug.Log($"Playing full token animation took too long, current amount of frames displayed:" +
                    $" {alreadyDisplayedSprites.Count}/{tokenSprites.Length}");
                Assert.Fail();
            }
        }

        // Assert that approx animation rotation time passed to display all frames
        Debug.Log("rotationTime left at the end was: " + rotationTime);
        UnityAssert.AreApproximatelyEqual(rotationTime, 0, tolerance);
    }

    // Verifies that player health is 0 after death
    // Kills at least mutations ROR31, ROR34, UOI134
    [UnityTest]
    public IEnumerator PlayerHealthZeroAfterDeath()
    {
        float moveTime = 3f;
        float spawnTime = 4f;

        // Move forward, enemy should be encountered before moveTime is over
        input.Press(keyboard.dKey);
        yield return new WaitForSeconds(moveTime);
        input.Release(keyboard.dKey);

        // Assert that player health is 0
        Assert.IsFalse(player.GetComponent<Health>().IsAlive);

        // Wait until player spawn to avoid weird FAILs in next tests
        yield return new WaitForSeconds(spawnTime);
    }

    // Verifies that different tabs can be opened in Pause-menu
    // Kills at least mutations UOI4, UOI7, UOI10, UOI11...
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

    // Verifies that enemy sprite is flipped when it changes facing direction
    // Kills at least mutations UOI22
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

    // Verifies that patrolling enemymoves to both end positions of its patrol path
    // Kills at least mutations UOI32
    [UnityTest]
    public IEnumerator EnemyVisitsPatrolPathEnds()
    {
        // How much enemy's end point can differentiate from patrol path end points
        float endPointTolerance = 0.35f;
        float monitorMovementDuration = 5f;

        // Move player to position where it's close to a patrolling enemy
        Vector3 newStartPos = new Vector3(19, 1, 1);
        player.position = newStartPos;

        // Get the needed enemy and patrolPath components
        GameObject enemy = GetClosestEnemy(player.position);
        PatrolPath pp = GetClosestPatrolPath(player.position).GetComponent<PatrolPath>();
        float ppStartX = pp.transform.position.x + pp.startPosition.x;
        float ppEndX = pp.transform.position.x + pp.endPosition.x;

        // Variables for tracking enemy's max and min position
        float minX = ppEndX;
        float maxX = ppStartX;

        // Monitor movement and get max and min points of enemy x-position
        while(monitorMovementDuration > 0)
        {
            // Reduce monitor duration each frame
            yield return null;
            monitorMovementDuration -= Time.deltaTime;

            if (enemy.transform.position.x < minX) minX = enemy.transform.position.x;
            if (enemy.transform.position.x > maxX) maxX = enemy.transform.position.x;
        }

        // Assert that max and min points are approx equal to patrol path end points
        UnityAssert.AreApproximatelyEqual(minX, ppStartX, endPointTolerance);
        UnityAssert.AreApproximatelyEqual(maxX, ppEndX, endPointTolerance);
    }

    // Verifies that token collect animation is played when player hits token
    // Should kill at least mutations UOI62, ASR9, ASR10
    [UnityTest]
    public IEnumerator TokenCollectAnimation()
    {
        float timeoutDuration = 2f; // How long to wait until test fails

        // Get first token found in the scene and move it above player
        GameObject token = GameObject.Find("Token");
        token.transform.position = origPos + Vector3.up;
        // Set needed objects
        Sprite lastSprite = token.GetComponent<SpriteRenderer>().sprite;
        List<Sprite> collectedSprites = new List<Sprite>(token.GetComponent<TokenInstance>().collectedAnimation);
        // List of sprites that have already been displayed
        List<Sprite> alreadyDisplayedSprites = new List<Sprite>();

        // Execute Jump to collect token
        input.Press(keyboard.spaceKey);

        // Do until all animation frames have been displayed
        while (alreadyDisplayedSprites.Count < collectedSprites.Count)
        {
            // Wait until next frame and set variables again
            yield return null;
            timeoutDuration -= Time.deltaTime;
            Sprite currentSprite = token.GetComponent<SpriteRenderer>().sprite;

            // If sprite was changed from last and is a collected-sprite, add it to displayed sprites list
            if (currentSprite != lastSprite && collectedSprites.Contains(currentSprite))
            {
                alreadyDisplayedSprites.Add(currentSprite);
                lastSprite = currentSprite;
            }

            // If too long has passed and all sprites are still not displayed, FAIL test
            if (timeoutDuration < 0)
            {
                Debug.Log($"Took too long to display all Collected_Token sprites, timing out");
                Debug.Log($"Displayed Collected_Token sprites: {alreadyDisplayedSprites.Count}/{collectedSprites.Count}");
                Assert.Fail();
            }
        }

        input.Release(keyboard.spaceKey);
    }

    // Verifies that player run audio effect is played correctly
    // Should kill at least mutations LCR5, PRV3, PRV4, ROR19, ROR20, ROR21, ROR22, ROR23, ROR24, UOI102
    // UOI114, UOI115, UOI116, UOI117
    [UnityTest]
    public IEnumerator PlayerRunAudio()
    {
        // How much can played amount of sounds differ from expected amount
        int playedSoundsAmtTolerance = 1;
        float moveRightDuration = 3f;
        float moveLeftDuration = 3f;
        float runAnimationLength = 0.604f;

        // Expected amount when 1 sound played at start of the run and then 1 sound for every animation loop
        int expectedSoundsAmt = (int)Math.Floor(moveRightDuration / runAnimationLength + 1);

        // No enemies needed for this test, disable so they won't interrupt running
        DisableEnemies();

        // Expected amount of audio sources when no sound effects are played
        int defaultAudioSourceAmt = GameObject.FindObjectsOfType<AudioSource>().Length;
        int currentAudioSourceAmt;

        // List of sound effect objects that are used during a run
        List<GameObject> playedSoundObjects = new List<GameObject>();

        // Move right
        Debug.Log("Starting run to right");
        input.Press(keyboard.dKey);

        Debug.Log("Default audio amount: " + defaultAudioSourceAmt);

        // Monitor for given duration
        while(moveRightDuration > 0)
        {
            yield return null;
            moveRightDuration -= Time.deltaTime;

            // Assert that maximum of two sound effects are played at once when player is running
            currentAudioSourceAmt = GameObject.FindObjectsOfType<AudioSource>().Length;
            Assert.IsTrue(currentAudioSourceAmt <= defaultAudioSourceAmt + 2);

            // Save all soundObjects used to the amount can be asserted later
            GameObject runSoundObj = GameObject.Find("One shot audio");
            
            if (runSoundObj != null)
            {
                if (!playedSoundObjects.Contains(runSoundObj))
                {
                    playedSoundObjects.Add(runSoundObj);
                }
            }
        }
        input.Release(keyboard.dKey);

        // Assert that correct amount of sound effects was played during the running
        Debug.Log($"played sounds amt: {playedSoundObjects.Count}, expected sounds amt: {expectedSoundsAmt}");
        UnityAssert.AreApproximatelyEqual(playedSoundObjects.Count, expectedSoundsAmt, playedSoundsAmtTolerance);

        // Empty played sound objects list before running to other direction
        playedSoundObjects.Clear();

        // Move left
        Debug.Log("Starting run to right");
        input.Press(keyboard.aKey);
        while (moveLeftDuration > 0)
        {
            yield return null;
            moveLeftDuration -= Time.deltaTime;

            // Assert that maximum of two sound effects are played at once when player is running
            currentAudioSourceAmt = GameObject.FindObjectsOfType<AudioSource>().Length;
            Assert.IsTrue(currentAudioSourceAmt <= defaultAudioSourceAmt + 2);

            GameObject runSoundObj = GameObject.Find("One shot audio");
            if (runSoundObj != null)
            {
                if (!playedSoundObjects.Contains(runSoundObj))
                {
                    playedSoundObjects.Add(runSoundObj);
                }
            }
        }
        input.Release(keyboard.aKey);

        // Assert that correct amount of sound effects was played during the running
        UnityAssert.AreApproximatelyEqual(playedSoundObjects.Count, expectedSoundsAmt, playedSoundsAmtTolerance);
    }

    // Verifies that player sprite is flipped when it changes facing direction
    // Kills at least mutations UOI28 (After_UOI140)
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

}
