using DodgeRunner;
using NUnit.Framework;
using UnityEngine;

// GameManager の状態遷移を保証する: Ready→Playing で速度が立ち上がり、
// Playing→GameOver で速度 0 になり、Ready 中の GameOver 呼び出しは無視される。
public class GameManagerTests
{
    GameObject host;
    GameManager gm;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("GameManagerTest");
        gm = host.AddComponent<GameManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
    }

    [Test]
    public void StartsInReadyWithZeroSpeed()
    {
        Assert.AreEqual(GameState.Ready, gm.State);
        Assert.AreEqual(0f, gm.Speed);
        Assert.AreEqual(0f, gm.Score);
    }

    [Test]
    public void StartGameEntersPlayingWithPositiveSpeed()
    {
        gm.StartGame();
        Assert.AreEqual(GameState.Playing, gm.State);
        Assert.Greater(gm.Speed, 0f);
    }

    [Test]
    public void GameOverStopsMovementOnlyWhilePlaying()
    {
        gm.GameOver();
        Assert.AreEqual(GameState.Ready, gm.State, "Ready 中の GameOver は無視される");

        gm.StartGame();
        gm.GameOver();
        Assert.AreEqual(GameState.GameOver, gm.State);
        Assert.AreEqual(0f, gm.Speed);
    }
}
