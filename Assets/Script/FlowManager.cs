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
        public int PelletsNeed;
        public int PelletSpawnCount;
        public List<GameObject> EnemyPrefabs;

        public GameObject GridPrefab;
    }

    public GameObject ScorePelletPrefab;

    public List<LevelTuning> LevelTunings = new List<LevelTuning>();

    public uint ScorePerLevel = 100;

    [HideInInspector]
    public GameObject CurrentGridLevel = null;

    public enum GameState
    {
        Title,
        PrePlaying,
        Playing,
        ProgressLevel,
        GameOver
    }

    public bool DebugPassCurrentLevel = false;

#if UNITY_EDITOR
    public bool DebugIgnoreEnemySpawning = false;
#endif

    public GameState CurrentState;
    public int CurrentLevel;
    public uint CurrentScore;
    public uint TotalScore;

    private float prePlayTimer = 3f;
    private float prePlayerSpawnTimer;
    private float prePlayerPelletSpawnTimer;

    public bool IsGameRunning => CurrentState == GameState.Playing;

    private bool firstUpdate = false;

    [HideInInspector]
    public List<GameObject> ObjectsToDestroyOnLevelEnd = new List<GameObject>();

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
                SpawnAllRemainingPellets();
            }
            else if (CurrentState == GameState.PrePlaying)
            {
                TransitionToPrePlay(true);
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

    public GameObject GetGridForCurrentLevel()
    {
        if (LevelTunings.Count == 0)
        {
#if UNITY_EDITOR
            throw new Exception("Pellet count per level not set up at all.");
#endif
            return null;
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            return LevelTunings[LevelTunings.Count - 1].GridPrefab;
        }

        return LevelTunings[CurrentLevel].GridPrefab;
    }

    public int GetPelletGoalForCurrentLevel()
    {
        if (LevelTunings.Count == 0)
        {
#if UNITY_EDITOR
            throw new Exception("Pellet count per level not set up at all.");
#endif
            return 1;
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            return LevelTunings[LevelTunings.Count - 1].PelletsNeed;
        }

        return LevelTunings[CurrentLevel].PelletsNeed;
    }

    public int GetPelletCountForCurrentLevel()
    {
        if (LevelTunings.Count == 0)
        {
#if UNITY_EDITOR
            throw new Exception("Pellet count per level not set up at all.");
#endif
            return 4;
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            return LevelTunings[LevelTunings.Count - 1].PelletSpawnCount;
        }

        return LevelTunings[CurrentLevel].PelletSpawnCount;
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

    public void AddScore()
    {
        CurrentScore++;
    }

    public bool HasEnoughScoreToProgress()
    {
        return CurrentScore >= GetPelletGoalForCurrentLevel();
    }

    public void TryToProgressLevel()
    {
        if (HasEnoughScoreToProgress())
        {
            ProgressCurrentLevel();
        }
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

    void DestroyAdditionalGameObjects()
    {
        for (int i = ObjectsToDestroyOnLevelEnd.Count - 1; i >= 0; i--)
        {
            Destroy(ObjectsToDestroyOnLevelEnd[i]);
            ObjectsToDestroyOnLevelEnd.RemoveAt(i);
        }
    }

    void TransitionToPrePlay(bool isGameInit = false)
    {
        DestroyAllEnemyActors();
        DestroyAllRemainingPellets();

        ResetCanvasAlphas();
        PrePlayScreenCanvas.alpha = 1.0f;

        CurrentState = GameState.PrePlaying;

        prePlayTimer = 3f;
        PrePlayCountdownText.text = "3";
        prePlayerSpawnTimer = 0f;

        if (CurrentGridLevel)
        {
            Destroy(CurrentGridLevel);
        }

        if (isGameInit)
        {
            CurrentLevel = 0;
            TotalScore = 0;
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
        ProcessPrePlayPelletSpawning();

        if (CurrentGridLevel == null)
        {
            var prefabToUse = GetGridForCurrentLevel();
            CurrentGridLevel = (GameObject) Instantiate(prefabToUse, prefabToUse.transform.position, Quaternion.identity);
        }

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
            SpawnAllRemainingPellets();
        }
    }

    void ProcessPrePlayEnemySpawning()
    {
#if UNITY_EDITOR
        if (DebugIgnoreEnemySpawning)
        {
            return;
        }
#endif

        if (GridActor.GetNumberOfEnemies() >= GetMaximumSpawnsForCurrentLevel())
        {
            return;
        }

        prePlayerSpawnTimer += GameConfig.GetDeltaTime();
        if (prePlayerSpawnTimer >= 2.5f / GetMaximumSpawnsForCurrentLevel())
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

    void DestroyAllRemainingPellets()
    {
        for (int i = ScorePellet.PelletList.Count - 1; i >= 0; i--)
        {
            Destroy(ScorePellet.PelletList[i].gameObject);
            ScorePellet.PelletList.RemoveAt(i);
        }
    }

    void ProcessPrePlayPelletSpawning()
    {
        if (ScorePellet.GetNumberOfPellets() >= GetPelletCountForCurrentLevel())
        {
            return;
        }

        prePlayerPelletSpawnTimer += GameConfig.GetDeltaTime();
        if (prePlayerPelletSpawnTimer >= 2.5f / GetPelletCountForCurrentLevel())
        {
            prePlayerPelletSpawnTimer = 0f;

            if (!SpawnPelletActor(ScorePelletPrefab))
            {
                Debug.LogWarning("Failed to spawn in an enemy during pre play, should probably take a look.");
            }
            
        }
    }

    void SpawnAllRemainingPellets()
    {
        if (ScorePellet.GetNumberOfPellets() >= GetPelletCountForCurrentLevel())
        {
            return;
        }

        Debug.LogWarning("Managed to get into SpawnAllRemainingPellets. Should really have spawned all enemies before this point.");

        for (int i = GetPelletCountForCurrentLevel() - ScorePellet.GetNumberOfPellets(); i > 0; i--)
        {
            SpawnPelletActor(ScorePelletPrefab);
        }
    }

    /// <summary>
    /// Backup for spawning the remaining enemies before popping into the current level
    /// </summary>
    void SpawnAllRemainingEnemies()
    {
#if UNITY_EDITOR
        if (DebugIgnoreEnemySpawning)
        {
            return;
        }
#endif

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
            DestroyAdditionalGameObjects();
            TransitionToPrePlay(true);
        }
    }

    void ProgressCurrentLevel()
    {
        ResetCanvasAlphas();
        ProgressedLevelCanvas.alpha = 1f;

        TotalScore += CurrentScore;
        TotalScore += ScorePerLevel;

        CurrentScore = 0;

        ProgressedLevelText.text = $"Level: {CurrentLevel}";
        ProgressedScoreText.text = $"Score: {TotalScore}";

        CurrentState = GameState.ProgressLevel;
    }

    void ProcessProgressionScreen()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DestroyAdditionalGameObjects();
            TransitionToPrePlay();
            CurrentLevel++;
        }
    }

    #endregion

    public bool SpawnPelletActor(GameObject prefab)
    {
        if (ScorePellet.GetNumberOfPellets() >= GetPelletCountForCurrentLevel())
        {
            return false;
        }

        GridManager.GetPositionOptions options = new GridManager.GetPositionOptions
        {
            AvoidanceDistance = 3.5f,
            AddPelletsToBeAvoided = true
        };

        options.AvoidancePoints = new List<Vector2Int>();
        options.AvoidancePoints.Add(Service.Player.GetGridPosition());

        var foundPosition = Service.Grid.GetPositionOnGrid(options);
        if (foundPosition == GridManager.INVALID_TILE)
        {
            return false;
        }

        var gameObj = (GameObject)Instantiate(prefab, new Vector3(foundPosition.x, foundPosition.y, 0f), Quaternion.identity);
        return true;
    }

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
