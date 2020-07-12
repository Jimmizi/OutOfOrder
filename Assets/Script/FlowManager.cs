using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FlowManager : MonoBehaviour
{
    public AudioSource MainMenuMusic;
    public AudioSource GameMusic;

    public const float Score_InternalInSeconds = 4f;
    public const int Score_PerInterval = 1;

    public const int Score_CollectedPellet = 20;
    public const int Score_LevelCompleted = 150;

    public CanvasGroup TitleScreenCanvas;
    public CanvasGroup PrePlayScreenCanvas;
    public CanvasGroup PlayingScreenCanvas;
    public CanvasGroup ProgressedLevelCanvas;
    public CanvasGroup ScoreScreenCanvas;

    public GameObject LevelOneTutorialObject;
    public GameObject LevelTwoTutorialObject;

    public Text ScoreCogText;
    public Text ScoreCogShadowText;

    public Text PrePlayCountdownText;
    public Text PrePlayCountdownText_Shadow;

    public Text ProgressedScoreText;
    public Text ProgressedLevelText;

    public Text GameOverScoreText;
    public Text GameOverLevelText;

    public Text LevelNameText;
    public Text LevelNameText_Shadow;
    private float timeBeforeLevelTextDisappears;

    [Serializable]
    public struct LevelTuning
    {
        public string LevelName;
        public string DebugDescription;

        public int SpawnCount;
        public int PelletsNeed;
        public int PelletSpawnCount;
        public List<GameObject> EnemyPrefabs;

        public GameObject GridPrefab;
    }

    private bool activatedTutorialOne, activatedTutorialTwo;

    public GameObject ScorePelletPrefab;

    public List<LevelTuning> LevelTunings = new List<LevelTuning>();


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
    [HideInInspector]
    public bool DebugIgnoreEnemySpawning = false;
#endif

    [HideInInspector]
    public GameState CurrentState;
    
    public int CurrentLevel;

    [HideInInspector]
    public int CurrentPellets;

    [HideInInspector]
    public uint CurrentScore;

    [HideInInspector]
    public uint TotalScore;

    private float prePlayTimer = 3f;
    private float prePlayerSpawnTimer;
    private float prePlayerPelletSpawnTimer;
    private float enoughCogsCollectedTimer;
    private int cogTextTimesToFlash;

    private float scoreIntervalTimer;

    private bool hasDoneEnoughCogsFlash = false;
    

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

    void ProcessCogsCollectedFlashing()
    {
        if (HasEnoughScoreToProgress())
        {
            SetHasCollectedEnoughCogs();
        }

        if (cogTextTimesToFlash <= 0)
        {
            return;
        }

        enoughCogsCollectedTimer += GameConfig.GetDeltaTime();
        if (enoughCogsCollectedTimer >= 0.5f)
        {
            enoughCogsCollectedTimer = 0f;
            cogTextTimesToFlash--;

            if (cogTextTimesToFlash > 0)
            {
                ScoreCogText.enabled = !ScoreCogText.enabled;
                ScoreCogShadowText.enabled = ScoreCogText.enabled;
            }
            else
            {
                ScoreCogText.enabled = true;
                ScoreCogShadowText.enabled = true;
            }
        }
    }

    void SetHasCollectedEnoughCogs()
    {
        if (!hasDoneEnoughCogsFlash)
        {
            hasDoneEnoughCogsFlash = true;

            cogTextTimesToFlash = 13;
            enoughCogsCollectedTimer = 0f;

            ScoreCogText.color = new Color(225f / 255f, 240f / 255f, 232f / 255f, 1f);
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


    public string GetNameForCurrentLevel()
    {
        return "";

        if (LevelTunings.Count == 0)
        {
            return "";
        }

        if (CurrentLevel >= LevelTunings.Count)
        {
            return "";
        }

        return LevelTunings[CurrentLevel].LevelName;
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
        CurrentScore += Score_CollectedPellet;
        CurrentPellets++;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        ScoreCogText.text = $"Cogs: {CurrentPellets}";
        ScoreCogShadowText.text = $"Cogs: {CurrentPellets}";
    }

    public bool HasEnoughScoreToProgress()
    {
        return CurrentPellets >= GetPelletGoalForCurrentLevel();
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

            GameOverLevelText.text = $"Floor: {CurrentLevel+1}";
            GameOverScoreText.text = $"Score: {TotalScore}";
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
        PrePlayCountdownText_Shadow.text = "3";
        prePlayerSpawnTimer = 0f;

        timeBeforeLevelTextDisappears = 0f;
        LevelNameText.enabled = false;
        LevelNameText_Shadow.enabled = false;

        var thisLevelName = GetNameForCurrentLevel();
        if (thisLevelName != "")
        {
            LevelNameText.enabled = true;
            LevelNameText_Shadow.enabled = true;

            timeBeforeLevelTextDisappears = 5f;
            LevelNameText.text = thisLevelName;
            LevelNameText_Shadow.text = thisLevelName;


            var col = LevelNameText.color;
            col.a = 1f;
            LevelNameText.color = col;

            var colS = LevelNameText_Shadow.color;
            colS.a = 1f;
            LevelNameText_Shadow.color = colS;
        }

        hasDoneEnoughCogsFlash = false;

        ScoreCogText.color = new Color(158f / 255f, 192f / 255f, 176f / 255f, 1f);

        if (CurrentGridLevel)
        {
            Destroy(CurrentGridLevel);
        }

        if (isGameInit)
        {
            TotalScore = 0;

            // if we did not get to the third level (the first level with enemies) then start from the beginning
            if (CurrentLevel < 2)
            {
                CurrentLevel = 0;
            }
            else
            {
                //Otherwise skip the tutorial levels if we 
                CurrentLevel = 2;

                //for (int i = 0; i < LevelTunings.Count || i < CurrentLevel; i++)
                //{
                //    TotalScore += Score_LevelCompleted;
                //    TotalScore += (uint)(Score_CollectedPellet * LevelTunings[i].PelletsNeed);
                //}

                //Give baseline score to get to this point
                TotalScore = Score_LevelCompleted * 2; // Two levels under our belt
                TotalScore += Score_CollectedPellet * 5; // 5 cogs needed to progress to this point
            }
        }

        if (CurrentLevel == 0 && !activatedTutorialOne)
        {
            LevelOneTutorialObject.SetActive(true);
            LevelTwoTutorialObject.SetActive(false);
            activatedTutorialOne = true;
        }
        else if (CurrentLevel == 1 && !activatedTutorialTwo)
        {
            LevelOneTutorialObject.SetActive(false);
            LevelTwoTutorialObject.SetActive(true);
            activatedTutorialTwo = true;
        }
        else
        {
            LevelTwoTutorialObject?.SetActive(false);
        }


    }

    bool KeyPressedToProgressFlow()
    {
        return
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return);
    }

    IEnumerator FadeMenuMusic()
    {
        while (MainMenuMusic.volume > 0f)
        {
            MainMenuMusic.volume -= GameConfig.GetDeltaTime() / 2f;
            yield return null;
        }

        MainMenuMusic.Stop();

        GameMusic.volume = 0f;
        GameMusic.Play();

        while (GameMusic.volume < 1f)
        {
            GameMusic.volume += GameConfig.GetDeltaTime();
            yield return null;
        }

        GameMusic.volume = 1f;

    }

    void ProcessTitle()
    {
        if (KeyPressedToProgressFlow())
        {
            
            StartCoroutine("FadeMenuMusic");
            

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
            PrePlayCountdownText_Shadow.text = "2";
        }
        else if (prePlayTimer <= 1f && prePlayTimer > 0f)
        {
            PrePlayCountdownText.text = "1";
            PrePlayCountdownText_Shadow.text = "1";
        }
        else if(prePlayTimer <= 0f && prePlayTimer > -0.4f)
        {
            PrePlayCountdownText.text = "GO";
            PrePlayCountdownText_Shadow.text = "GO";
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
        if (timeBeforeLevelTextDisappears > 0f)
        {
            timeBeforeLevelTextDisappears -= GameConfig.GetDeltaTime();


            var col = LevelNameText.color;
            col.a = Mathf.Clamp(timeBeforeLevelTextDisappears, 0.0f, 1.0f);
            LevelNameText.color = col;

            var colS = LevelNameText_Shadow.color;
            colS.a = Mathf.Clamp(timeBeforeLevelTextDisappears, 0.0f, 1.0f);
            LevelNameText_Shadow.color = colS;

            if (timeBeforeLevelTextDisappears <= 0f)
            {
                LevelNameText.enabled = false;
                LevelNameText_Shadow.enabled = false;
            }

        }

        ProcessCogsCollectedFlashing();

//#if UNITY_EDITOR
//        if (DebugPassCurrentLevel)
//        {
//            DebugPassCurrentLevel = false;
//            ProgressCurrentLevel();
//            return;
//        }
//#endif

        scoreIntervalTimer += GameConfig.GetDeltaTime();
        if (scoreIntervalTimer >= Score_InternalInSeconds)
        {
            scoreIntervalTimer = 0f;
            CurrentScore += Score_PerInterval;
        }
    }

    void ProcessGameOver()
    {
        if (KeyPressedToProgressFlow())
        {
            DestroyAdditionalGameObjects();
            TransitionToPrePlay(true);
        }
    }

    void ProgressCurrentLevel()
    {
        ResetCanvasAlphas();
        ProgressedLevelCanvas.alpha = 1f;

        CurrentLevel++;

        TotalScore += CurrentScore;
        TotalScore += Score_LevelCompleted;

        CurrentPellets = 0;
        UpdateScoreText();

        // Text is disabled on the canvas
        //ProgressedLevelText.text = $"Floor: {CurrentLevel+1}";
        ProgressedScoreText.text = $"Score: {TotalScore}";

        CurrentState = GameState.ProgressLevel;
    }

    void ProcessProgressionScreen()
    {
        if (KeyPressedToProgressFlow())
        {
            DestroyAdditionalGameObjects();
            TransitionToPrePlay();
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
