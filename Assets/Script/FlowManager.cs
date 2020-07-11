using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FlowManager : MonoBehaviour
{
    public CanvasGroup TitleScreenCanvas;
    public CanvasGroup PrePlayScreenCanvas;
    public CanvasGroup PlayingScreenCanvas;
    public CanvasGroup ProgressedLevelCanvas;
    public CanvasGroup ScoreScreenCanvas;

    public Text PrePlayCountdownText;
    public Text ProgressedScoreText;
    public Text ProgressedLevelText;

    [Serializable]
    public struct LevelTuning
    {
        public int SpawnCount;
        public List<GameObject> EnemyPrefabs;
    }

    public List<LevelTuning> LevelTunings = new List<LevelTuning>();

    public uint ScorePerLevel = 100;


    public enum GameState
    {
        Title,
        PrePlaying,
        Playing,
        ProgressLevel,
        GameOver
    }

    public bool DebugPassCurrentLevel = false;

    public GameState CurrentState;
    public int CurrentLevel;
    public uint CurrentScore;

    private float prePlayTimer;
    private float prePlayerSpawnTimer;

    public bool IsGameRunning => CurrentState == GameState.Playing;

    private bool firstUpdate = false;

    void Awake()
    {
        Service.Flow = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ResetCanvasAlphas();
        TitleScreenCanvas.alpha = 1f;
    }

    #region States

    // Update is called once per frame
    void Update()
    {
        if (!firstUpdate)
        {
            firstUpdate = true;

            if (CurrentState == GameState.Playing)
            {
                SpawnAllRemainingEnemies();
            }
        }

        switch (CurrentState)
        {
            case GameState.Title:
                ProcessTitle();
                break;
            case GameState.PrePlaying:
                ProcessPrePlaying();
                break;
            case GameState.Playing:
                ProcessPlaying();
                break;
            case GameState.ProgressLevel:
                ProcessProgressionScreen();
                break;
            case GameState.GameOver:
                ProcessGameOver();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void ResetCanvasAlphas()
    {
        TitleScreenCanvas.alpha = 0f;
        PrePlayScreenCanvas.alpha = 0f;
        PlayingScreenCanvas.alpha = 0f;
        ScoreScreenCanvas.alpha = 0f;
        ProgressedLevelCanvas.alpha = 0f;
    }

    public int GetMaximumSpawnsForCurrentLevel()
    {
        if (LevelTunings.Count == 0)
        {
#if UNITY_EDITOR
            throw new Exception("Spawn Count per level not set up at all.");
#endif
            return 4;
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            return LevelTunings[LevelTunings.Count - 1].SpawnCount;
        }

        return LevelTunings[CurrentLevel].SpawnCount;
    }

    public GameObject GetEnemyPrefabForCurrentLevel()
    {
        if (LevelTunings.Count == 0)
        {
#if UNITY_EDITOR
            throw new Exception("Enemy prefabs per level not set up at all.");
#endif
            return null;
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            var levelToUse = LevelTunings.Count - 1;

            if (LevelTunings[levelToUse].EnemyPrefabs.Count == 0)
            {
#if UNITY_EDITOR
                throw new Exception("Enemy prefabs per level has the level setup without any prefabs.");
#endif
                return null;
            }

            int rand = Random.Range(0, LevelTunings[levelToUse].EnemyPrefabs.Count);
            return LevelTunings[levelToUse].EnemyPrefabs[rand];
        }

        int randCurrent = Random.Range(0, LevelTunings[CurrentLevel].EnemyPrefabs.Count);
        return LevelTunings[CurrentLevel].EnemyPrefabs[randCurrent];
    }
        
    public void SetGameOver()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.GameOver;
            ResetCanvasAlphas();
            ScoreScreenCanvas.alpha = 1f;
        }
    }

    void DestroyAllEnemyActors()
    {
        for (int i = GridActor.ActorList.Count - 1; i >= 0; i--)
        {
            if (GridActor.ActorList[i].tag == "enemy")
            {
                Destroy(GridActor.ActorList[i].gameObject);
                GridActor.ActorList.RemoveAt(i);
            }
        }
    }

    void TransitionToPrePlay(bool isGameInit = false)
    {
        DestroyAllEnemyActors();

        ResetCanvasAlphas();
        PrePlayScreenCanvas.alpha = 1.0f;

        CurrentState = GameState.PrePlaying;

        prePlayTimer = 3f;
        PrePlayCountdownText.text = "3";
        prePlayerSpawnTimer = 0f;
        
        if (isGameInit)
        {
            CurrentLevel = 0;
            CurrentScore = 0;
        }
    }

    void ProcessTitle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TransitionToPrePlay(true);
        }
    }

    void ProcessPrePlaying()
    {
        prePlayTimer -= GameConfig.GetDeltaTime();
        ProcessPrePlayEnemySpawning();

        if (prePlayTimer <= 2f && prePlayTimer > 1f)
        {
            PrePlayCountdownText.text = "2";
        }
        else if (prePlayTimer <= 1f && prePlayTimer > 0f)
        {
            PrePlayCountdownText.text = "1";
        }
        else if(prePlayTimer <= 0f && prePlayTimer > -0.4f)
        {
            PrePlayCountdownText.text = "GO";
        }
        else if(prePlayTimer <= -0.4f)
        {
            ResetCanvasAlphas();
            PlayingScreenCanvas.alpha = 1f;

            CurrentState = GameState.Playing;

            SpawnAllRemainingEnemies();
        }
    }

    void ProcessPrePlayEnemySpawning()
    {
        if (GridActor.GetNumberOfEnemies() >= GetMaximumSpawnsForCurrentLevel())
        {
            return;
        }

        prePlayerSpawnTimer += GameConfig.GetDeltaTime();
        if (prePlayerSpawnTimer >= GetMaximumSpawnsForCurrentLevel() / 2.5f)
        {
            prePlayerSpawnTimer = 0f;

            var prefab = GetEnemyPrefabForCurrentLevel();
            if (prefab)
            {
                if (!SpawnEnemyActor(prefab))
                {
                    Debug.LogWarning("Failed to spawn in an enemy during pre play, should probably take a look.");
                }
            }
        }
    }

    /// <summary>
    /// Backup for spawning the remaining enemies before popping into the current level
    /// </summary>
    void SpawnAllRemainingEnemies()
    {
        if (GridActor.GetNumberOfEnemies() >= GetMaximumSpawnsForCurrentLevel())
        {
            return;
        }

        Debug.LogWarning("Managed to get into SpawnAllRemainingEnemies. Should really have spawned all enemies before this point.");

        for (int i = GetMaximumSpawnsForCurrentLevel() - GridActor.GetNumberOfEnemies(); i > 0; i--)
        {
            var prefab = GetEnemyPrefabForCurrentLevel();
            if (prefab)
            {
                SpawnEnemyActor(prefab);
            }
        }
    }

    void ProcessPlaying()
    {
#if UNITY_EDITOR
        if (DebugPassCurrentLevel)
        {
            DebugPassCurrentLevel = false;
            ProgressCurrentLevel();
        }
#endif
    }

    void ProcessGameOver()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TransitionToPrePlay(true);
        }
    }

    void ProgressCurrentLevel()
    {
        ResetCanvasAlphas();
        ProgressedLevelCanvas.alpha = 1f;

        ProgressedLevelText.text = $"Level: {CurrentLevel}";
        ProgressedScoreText.text = $"Score: {CurrentScore}";

        CurrentState = GameState.ProgressLevel;
        CurrentScore += ScorePerLevel;
    }

    void ProcessProgressionScreen()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TransitionToPrePlay();
            CurrentLevel++;
        }
    }

    #endregion


    public bool SpawnEnemyActor(GameObject prefab)
    {
        if (GridActor.GetNumberOfEnemies() >= GetMaximumSpawnsForCurrentLevel())
        {
            return false;
        }

        GridManager.GetPositionOptions options = new GridManager.GetPositionOptions
        {
            AvoidanceDistance = 7f,
            AddEnemiesToBeAvoided = true
        };

        options.AvoidancePoints = new List<Vector2Int>();
        options.AvoidancePoints.Add(Service.Player.GetGridPosition());

        var foundPosition = Service.Grid.GetPositionOnGrid(options);
        if (foundPosition == GridManager.INVALID_TILE)
        {
            return false;
        }

        var gameObj = (GameObject) Instantiate(prefab, new Vector3(foundPosition.x, foundPosition.y, 0f), Quaternion.identity);
        return true;
    }
}
