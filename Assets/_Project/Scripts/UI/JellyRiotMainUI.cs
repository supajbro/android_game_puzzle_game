using System;
using JellyRiot.Core;
using JellyRiot.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JellyRiot.UI
{
    /// <summary>
    /// Production shell for the menu and gameplay screens. Gameplay commands go to Core and the
    /// GridViewPresenter reconciles every visual against the resulting authoritative snapshot.
    /// </summary>
    public sealed class JellyRiotMainUI : MonoBehaviour
    {
        private const float ReferenceWidth = 1080f;
        private static readonly Color Cream = new Color32(255, 245, 232, 255);
        private static readonly Color Plum = new Color32(20, 20, 43, 255);
        private static readonly Color Indigo = new Color32(41, 42, 86, 255);

        [Header("Brand")]
        [SerializeField] private Sprite background;
        [SerializeField] private Sprite logo;
        [SerializeField] private Sprite mascot;
        [SerializeField] private Sprite boardOverlay;

        [Header("Panels")]
        [SerializeField] private Sprite cellHole;
        [SerializeField] private Sprite pillButton;
        [SerializeField] private Sprite scorePanel;
        [SerializeField] private Sprite informationPanel;

        [Header("Icons")]
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite replayIcon;
        [SerializeField] private Sprite soundIcon;
        [SerializeField] private Sprite hapticsIcon;
        [SerializeField] private Sprite homeIcon;
        [SerializeField] private Sprite dailyIcon;
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite selectionArrow;
        [SerializeField] private Sprite selectionHighlight;

        [Header("Jellies: Coral, Lemon, Aqua, Lilac, Mint")]
        [SerializeField] private Sprite[] jellySprites = new Sprite[5];
        [SerializeField] private Sprite jellyShadow;
        [SerializeField] private JellyAnimationLibrary animationLibrary;

        private readonly Image[] _boardJellies = new Image[BoardModel.CellCount];
        private readonly Image[] _boardShadows = new Image[BoardModel.CellCount];
        private readonly Button[] _columnButtons = new Button[BoardModel.Width];
        private readonly Image[] _columnFeedback = new Image[BoardModel.Width];
        private readonly Image[] _rackJellies = new Image[GameSession.RackSize];
        private readonly Image[] _rackShadows = new Image[GameSession.RackSize];
        private readonly Image[] _rackSelections = new Image[GameSession.RackSize];
        private GridViewPresenter _gridPresenter;
        private GameSession _session;
        private GameObject _homeScreen;
        private GameObject _gameScreen;
        private GameObject _pauseScreen;
        private GameObject _settingsScreen;
        private GameObject _resultsScreen;
        private GameObject _dailyScreen;
        private Text _scoreText;
        private Text _bestText;
        private Text _pressureText;
        private Text _resultText;
        private Text _soundText;
        private Text _hapticsText;
        private Text _reducedMotionText;
        private Text _slowMotionText;
        private bool _soundEnabled = true;
        private bool _hapticsEnabled = true;
        private bool _reducedMotion;
        private bool _settingsFromPause;
        private Font _font;

        public int BoardCellCount => _boardJellies.Length;
        public int RackSlotCount => _rackJellies.Length;
        public GameSession Session => _session;
        public GridViewPresenter GridPresenter => _gridPresenter;
        public Sprite GetJellySprite(JellyColour colour) => SpriteFor(colour);

        public void Configure(Sprite backgroundSprite, Sprite logoSprite, Sprite mascotSprite,
            Sprite overlaySprite, Sprite cellSprite, Sprite pillSprite, Sprite scoreSprite,
            Sprite infoPanelSprite, Sprite pauseSprite, Sprite replaySprite, Sprite soundSprite,
            Sprite hapticsSprite, Sprite homeSprite, Sprite dailySprite, Sprite infoSprite,
            Sprite arrowSprite, Sprite highlightSprite, Sprite[] colours, Sprite shadowSprite,
            JellyAnimationLibrary animations)
        {
            background = backgroundSprite;
            logo = logoSprite;
            mascot = mascotSprite;
            boardOverlay = overlaySprite;
            cellHole = cellSprite;
            pillButton = pillSprite;
            scorePanel = scoreSprite;
            informationPanel = infoPanelSprite;
            pauseIcon = pauseSprite;
            replayIcon = replaySprite;
            soundIcon = soundSprite;
            hapticsIcon = hapticsSprite;
            homeIcon = homeSprite;
            dailyIcon = dailySprite;
            infoIcon = infoSprite;
            selectionArrow = arrowSprite;
            selectionHighlight = highlightSprite;
            jellySprites = colours;
            jellyShadow = shadowSprite;
            animationLibrary = animations;
        }

        private void Awake()
        {
            ValidateArtwork();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _session = new GameSession();
            _session.CompleteBoot();
            BuildInterface();
            ShowHome();
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (_settingsScreen.activeSelf)
            {
                CloseSettings();
            }
            else if (_dailyScreen.activeSelf)
            {
                ShowHome();
            }
            else if (_pauseScreen.activeSelf)
            {
                ResumeGame();
            }
            else if (_gameScreen.activeSelf && _session.State == SessionState.Ready)
            {
                PauseGame();
            }
        }

        private void ValidateArtwork()
        {
            if (background == null || logo == null || mascot == null || boardOverlay == null ||
                cellHole == null || pillButton == null || scorePanel == null || informationPanel == null ||
                jellyShadow == null || jellySprites == null || jellySprites.Length != 5)
            {
                throw new InvalidOperationException("Jelly Riot artwork references are incomplete. Run Jelly Riot/Visuals/Build Main Scene.");
            }

            for (int i = 0; i < jellySprites.Length; i++)
            {
                if (jellySprites[i] == null)
                {
                    throw new InvalidOperationException("Every JellyColour must have a gameplay sprite.");
                }
            }
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("Jelly Riot Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = CreateImage("Background", canvasObject.transform, background, false);
            Stretch(backdrop.rectTransform);

            GameObject safeArea = CreateRect("Safe Area", canvasObject.transform);
            Stretch((RectTransform)safeArea.transform);
            safeArea.AddComponent<SafeAreaFitter>();

            _homeScreen = CreateScreen("Home Screen", safeArea.transform);
            BuildHome(_homeScreen.transform);
            _gameScreen = CreateScreen("Gameplay Screen", safeArea.transform);
            BuildGameplay(_gameScreen.transform);
            _pauseScreen = CreateScreen("Pause Screen", safeArea.transform);
            BuildPause(_pauseScreen.transform);
            _settingsScreen = CreateScreen("Settings Screen", safeArea.transform);
            BuildSettings(_settingsScreen.transform);
            _resultsScreen = CreateScreen("Results Screen", safeArea.transform);
            BuildResults(_resultsScreen.transform);
            _dailyScreen = CreateScreen("Daily Screen", safeArea.transform);
            BuildDaily(_dailyScreen.transform);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
            eventSystem.transform.SetParent(transform, false);
        }

        private void BuildHome(Transform parent)
        {
            CreateImage("Logo", parent, logo, true, new Vector2(760f, 430f), new Vector2(0f, 570f));
            CreateImage("Mascot", parent, mascot, true, new Vector2(430f, 430f), new Vector2(0f, 185f));
            CreateLabelledButton("Play Button", parent, "PLAY", new Vector2(600f, 190f),
                new Vector2(0f, -225f), StartEndless);
            CreateLabelledButton("Daily Button", parent, "DAILY CHALLENGE", new Vector2(600f, 160f),
                new Vector2(0f, -430f), ShowDaily, dailyIcon);
            CreateLabelledButton("Settings Button", parent, "SETTINGS", new Vector2(460f, 135f),
                new Vector2(0f, -600f), OpenHomeSettings, infoIcon);
        }

        private void BuildGameplay(Transform parent)
        {
            Image score = CreateImage("Score Panel", parent, scorePanel, true,
                new Vector2(610f, 205f), new Vector2(-105f, 770f));
            _scoreText = CreateText("Score", score.transform, "0", 58, TextAnchor.MiddleCenter, Cream);
            Stretch(_scoreText.rectTransform, 50f, 30f, 50f, 30f);
            _bestText = CreateText("Best", parent, "BEST 0", 34, TextAnchor.MiddleCenter, Cream,
                new Vector2(380f, 70f), new Vector2(-160f, 662f));
            _pressureText = CreateText("Pressure", parent, "NEXT ROW IN 6", 32, TextAnchor.MiddleCenter, Cream,
                new Vector2(430f, 70f), new Vector2(160f, 662f));

            CreateIconButton("Pause Button", parent, pauseIcon, new Vector2(145f, 145f),
                new Vector2(435f, 765f), PauseGame);

            Image board = CreateImage("Board Frame", parent, boardOverlay, true,
                new Vector2(760f, 1075f), new Vector2(0f, 35f));
            RectTransform grid = (RectTransform)CreateRect("7 x 9 Grid", board.transform).transform;
            grid.sizeDelta = new Vector2(574f, 738f);
            grid.anchoredPosition = new Vector2(0f, -4f);
            const float cellSize = 82f;
            // Keep each rendering layer contiguous so later cell holes never cover earlier jellies.
            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    int index = (row * BoardModel.Width) + column;
                    Vector2 position = new Vector2((column - 3) * cellSize, (4 - row) * cellSize);
                    CreateImage($"Cell {column},{row}", grid, cellHole, true,
                        new Vector2(78f, 78f), position);
                }
            }

            for (int column = 0; column < BoardModel.Width; column++)
            {
                _columnFeedback[column] = CreateImage($"Column Feedback {column}", grid,
                    selectionHighlight, true, new Vector2(86f, 86f),
                    new Vector2((column - 3) * cellSize, 4 * cellSize));
                _columnFeedback[column].raycastTarget = false;
                _columnFeedback[column].gameObject.SetActive(false);
            }

            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    int index = (row * BoardModel.Width) + column;
                    Vector2 position = new Vector2((column - 3) * cellSize, (4 - row) * cellSize);
                    _boardShadows[index] = CreateImage($"Shadow {column},{row}", grid, jellyShadow, true,
                        new Vector2(72f, 72f), position + new Vector2(0f, -5f));
                }
            }

            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    int index = (row * BoardModel.Width) + column;
                    Vector2 position = new Vector2((column - 3) * cellSize, (4 - row) * cellSize);
                    _boardJellies[index] = CreateImage($"Jelly {column},{row}", grid, jellySprites[0], true,
                        new Vector2(75f, 75f), position + new Vector2(0f, 2f));
                }
            }

            for (int column = 0; column < BoardModel.Width; column++)
            {
                int capturedColumn = column;
                Button target = CreateTransparentButton($"Column {column}", grid,
                    new Vector2(cellSize, cellSize * BoardModel.Height),
                    new Vector2((column - 3) * cellSize, 0f));
                target.onClick.AddListener(() => DropInColumn(capturedColumn));
                _columnButtons[column] = target;
            }

            Image rackBacking = CreateImage("Three Slot Rack", parent, scorePanel, true,
                new Vector2(690f, 235f), new Vector2(0f, -700f));
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                int capturedSlot = slot;
                Vector2 slotPosition = new Vector2((slot - 1) * 205f, 0f);
                CreateImage($"Rack Hole {slot}", rackBacking.transform, cellHole, true,
                    new Vector2(165f, 165f), slotPosition);
                _rackSelections[slot] = CreateImage($"Selection {slot}", rackBacking.transform,
                    selectionHighlight, true, new Vector2(185f, 185f), slotPosition);
                _rackShadows[slot] = CreateImage($"Rack Shadow {slot}", rackBacking.transform, jellyShadow, true,
                    new Vector2(130f, 130f), slotPosition + new Vector2(0f, -7f));
                _rackJellies[slot] = CreateImage($"Rack Jelly {slot}", rackBacking.transform,
                    jellySprites[slot], true, new Vector2(135f, 135f), slotPosition + new Vector2(0f, 3f));
                Button button = CreateTransparentButton($"Rack Button {slot}", rackBacking.transform,
                    new Vector2(190f, 190f), slotPosition);
                button.onClick.AddListener(() => SelectRack(capturedSlot));
            }

            CreateImage("Selection Arrow", parent, selectionArrow, true,
                new Vector2(105f, 90f), new Vector2(0f, -850f));

            Text debugText = null;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            debugText = CreateText("Visual State Debug", parent, string.Empty, 20,
                TextAnchor.UpperLeft, Cream, new Vector2(420f, 100f), new Vector2(-300f, 560f));
            debugText.raycastTarget = false;
            debugText.gameObject.SetActive(false);
#endif
            _gridPresenter = gameObject.AddComponent<GridViewPresenter>();
            _gridPresenter.Initialise(_session, jellySprites, animationLibrary, _boardJellies, _boardShadows,
                _rackJellies, _rackShadows, _rackSelections, _columnFeedback, _columnButtons, debugText);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Button slowMotion = CreateLabelledButton("Animation Slow Motion", parent, "ANIM 1x",
                new Vector2(220f, 72f), new Vector2(380f, 585f), ToggleSlowMotion);
            _slowMotionText = slowMotion.transform.Find("Label").GetComponent<Text>();
#endif
        }

        private void BuildPause(Transform parent)
        {
            CreateDimmer(parent);
            Image panel = CreateImage("Pause Panel", parent, informationPanel, true,
                new Vector2(850f, 900f), Vector2.zero);
            CreateText("Title", panel.transform, "PAUSED", 76, TextAnchor.MiddleCenter, Indigo,
                new Vector2(600f, 120f), new Vector2(0f, 270f));
            CreateLabelledButton("Resume", panel.transform, "RESUME", new Vector2(520f, 150f),
                new Vector2(0f, 80f), ResumeGame);
            CreateLabelledButton("Settings", panel.transform, "SETTINGS", new Vector2(520f, 135f),
                new Vector2(0f, -100f), OpenPauseSettings, infoIcon);
            CreateLabelledButton("Home", panel.transform, "HOME", new Vector2(420f, 125f),
                new Vector2(0f, -270f), ReturnHome, homeIcon);
        }

        private void BuildSettings(Transform parent)
        {
            CreateDimmer(parent);
            Image panel = CreateImage("Settings Panel", parent, informationPanel, true,
                new Vector2(880f, 1040f), Vector2.zero);
            CreateText("Title", panel.transform, "SETTINGS", 72, TextAnchor.MiddleCenter, Indigo,
                new Vector2(650f, 110f), new Vector2(0f, 350f));
            _soundText = CreateToggleButton("Sound Toggle", panel.transform, soundIcon,
                new Vector2(0f, 140f), ToggleSound);
            _hapticsText = CreateToggleButton("Haptics Toggle", panel.transform, hapticsIcon,
                new Vector2(0f, -55f), ToggleHaptics);
            _reducedMotionText = CreateToggleButton("Reduced Motion Toggle", panel.transform, infoIcon,
                new Vector2(0f, -250f), ToggleReducedMotion);
            CreateLabelledButton("Close", panel.transform, "DONE", new Vector2(480f, 140f),
                new Vector2(0f, -430f), CloseSettings);
            RefreshSettingsLabels();
        }

        private void BuildResults(Transform parent)
        {
            CreateDimmer(parent);
            Image panel = CreateImage("Results Panel", parent, informationPanel, true,
                new Vector2(900f, 1120f), Vector2.zero);
            CreateText("Title", panel.transform, "JELLY JAM!", 72, TextAnchor.MiddleCenter, Indigo,
                new Vector2(680f, 120f), new Vector2(0f, 390f));
            _resultText = CreateText("Result", panel.transform, "SCORE 0", 52, TextAnchor.MiddleCenter, Indigo,
                new Vector2(650f, 220f), new Vector2(0f, 165f));
            CreateLabelledButton("Replay", panel.transform, "REPLAY", new Vector2(520f, 150f),
                new Vector2(0f, -95f), Replay, replayIcon);
            CreateLabelledButton("Home", panel.transform, "HOME", new Vector2(430f, 130f),
                new Vector2(0f, -300f), ReturnHome, homeIcon);
        }

        private void BuildDaily(Transform parent)
        {
            Image panel = CreateImage("Daily Panel", parent, informationPanel, true,
                new Vector2(900f, 1260f), Vector2.zero);
            CreateImage("Daily Icon", panel.transform, dailyIcon, true,
                new Vector2(230f, 230f), new Vector2(0f, 395f));
            CreateText("Title", panel.transform, "DAILY CHALLENGE", 64, TextAnchor.MiddleCenter, Indigo,
                new Vector2(720f, 110f), new Vector2(0f, 235f));
            CreateText("Description", panel.transform,
                "A shared puzzle for today.\nOne seed. Three choices. Your best score.", 36,
                TextAnchor.MiddleCenter, Indigo, new Vector2(700f, 240f), new Vector2(0f, 45f));
            CreateLabelledButton("Play Daily", panel.transform, "PLAY DAILY", new Vector2(560f, 155f),
                new Vector2(0f, -205f), StartDaily);
            CreateLabelledButton("Back", panel.transform, "BACK", new Vector2(400f, 125f),
                new Vector2(0f, -395f), ShowHome, homeIcon);
        }

        private void StartEndless()
        {
            _gridPresenter.Present(_session.StartNewRun((ulong)DateTime.UtcNow.Ticks, GameMode.Endless));
            ShowGame();
        }

        private void StartDaily()
        {
            string key = DateTime.UtcNow.ToString("yyyy-MM-dd");
            ulong seed = DailySeed.Derive(key, 1, "jelly-riot-v1");
            _gridPresenter.Present(_session.StartNewRun(seed, GameMode.Daily, key));
            ShowGame();
        }

        private void SelectRack(int slot)
        {
            if (_gridPresenter.IsPresenting) return;
            SessionActionResult result = _session.SelectRackSlot(slot);
            _gridPresenter.Present(result);
            if (result.Succeeded)
            {
                RefreshGame();
            }
        }

        private void DropInColumn(int column)
        {
            if (_gridPresenter.IsPresenting) return;
            SessionActionResult begin = _session.TryBeginTurn(column);
            if (!begin.Succeeded)
            {
                _gridPresenter.Present(begin);
                _gridPresenter.ShowInvalidColumn(column);
                return;
            }

            SessionActionResult complete = _session.CompleteTurn();
            SessionActionResult terminal = null;
            if (_session.State == SessionState.PendingLoss)
            {
                terminal = _session.ConfirmGameOver();
            }
            _gridPresenter.PresentTurn(begin, complete, terminal, FinishTurnPresentation);
        }

        private void FinishTurnPresentation()
        {
            RefreshGame();
            if (_session.State == SessionState.GameOver) ShowResults();
        }

        private void PauseGame()
        {
            SessionActionResult result = _session.Pause();
            _gridPresenter.Present(result);
            if (result.Succeeded)
            {
                _pauseScreen.SetActive(true);
            }
        }

        private void ResumeGame()
        {
            SessionActionResult result = _session.Resume();
            _gridPresenter.Present(result);
            if (result.Succeeded)
            {
                _pauseScreen.SetActive(false);
            }
        }

        private void Replay()
        {
            SessionActionResult result = _session.Replay();
            _gridPresenter.Present(result);
            if (result.Succeeded)
            {
                ShowGame();
            }
        }

        private void ReturnHome()
        {
            _gridPresenter.CancelPendingAndSynchronise();
            if (_session.State == SessionState.Paused)
            {
                _gridPresenter.Present(_session.Resume());
            }

            _gridPresenter.Present(_session.ReturnHome());
            ShowHome();
        }

        private void ShowHome()
        {
            SetOnly(_homeScreen);
        }

        private void ShowDaily()
        {
            SetOnly(_dailyScreen);
        }

        private void ShowGame()
        {
            SetOnly(_gameScreen);
            RefreshGame();
        }

        private void ShowResults()
        {
            SetOnly(_resultsScreen);
            RunScoreResult result = _session.CurrentResult;
            _resultText.text = result == null
                ? $"SCORE {_session.Score.CurrentScore:N0}"
                : $"SCORE {result.FinalScore:N0}\nBEST {_session.Score.BestEndlessScore:N0}\nLONGEST CHAIN {result.LongestChain}";
        }

        private void OpenHomeSettings()
        {
            _settingsFromPause = false;
            _settingsScreen.SetActive(true);
        }

        private void OpenPauseSettings()
        {
            _settingsFromPause = true;
            _settingsScreen.SetActive(true);
        }

        private void CloseSettings()
        {
            _settingsScreen.SetActive(false);
            if (!_settingsFromPause && !_gameScreen.activeSelf)
            {
                ShowHome();
            }
        }

        private void ToggleSound()
        {
            _soundEnabled = !_soundEnabled;
            RefreshSettingsLabels();
        }

        private void ToggleHaptics()
        {
            _hapticsEnabled = !_hapticsEnabled;
            RefreshSettingsLabels();
        }

        private void ToggleReducedMotion()
        {
            _reducedMotion = !_reducedMotion;
            _gridPresenter.SetReducedMotion(_reducedMotion);
            RefreshSettingsLabels();
        }

        private void ToggleSlowMotion()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _gridPresenter.ToggleSlowMotion();
            if (_slowMotionText != null)
                _slowMotionText.text = _gridPresenter.SlowMotionEnabled ? "ANIM 0.25x" : "ANIM 1x";
#endif
        }

        private void RefreshSettingsLabels()
        {
            _soundText.text = _soundEnabled ? "SOUND  ON" : "SOUND  OFF";
            _hapticsText.text = _hapticsEnabled ? "HAPTICS  ON" : "HAPTICS  OFF";
            _reducedMotionText.text = _reducedMotion ? "REDUCED MOTION  ON" : "REDUCED MOTION  OFF";
        }

        private void RefreshGame()
        {
            if (_session.Board == null)
            {
                return;
            }

            if (!_gridPresenter.IsPresenting) _gridPresenter.SynchroniseSnapshot();

            _scoreText.text = _session.Score.CurrentScore.ToString("N0");
            long best = _session.Mode == GameMode.Daily
                ? _session.Score.GetDailyBest(_session.DailyKey)
                : _session.Score.BestEndlessScore;
            _bestText.text = _session.Mode == GameMode.Daily ? $"DAILY BEST {best:N0}" : $"BEST {best:N0}";
            _pressureText.text = $"NEXT ROW IN {_session.NextRowIn}";
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus || _session == null || _gridPresenter == null) return;
            _gridPresenter.CancelPendingAndSynchronise();
            if (_session.State == SessionState.Ready && _gameScreen.activeSelf)
            {
                SessionActionResult result = _session.Pause();
                _gridPresenter.Present(result);
                if (result.Succeeded) _pauseScreen.SetActive(true);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
            else _gridPresenter?.CancelPendingAndSynchronise();
        }

        private Sprite SpriteFor(JellyColour colour)
        {
            int index = (int)colour - 1;
            return index >= 0 && index < jellySprites.Length ? jellySprites[index] : jellySprites[0];
        }

        private void SetOnly(GameObject screen)
        {
            _homeScreen.SetActive(screen == _homeScreen);
            _gameScreen.SetActive(screen == _gameScreen);
            _pauseScreen.SetActive(false);
            _settingsScreen.SetActive(false);
            _resultsScreen.SetActive(screen == _resultsScreen);
            _dailyScreen.SetActive(screen == _dailyScreen);
        }

        private GameObject CreateScreen(string name, Transform parent)
        {
            GameObject screen = CreateRect(name, parent);
            Stretch((RectTransform)screen.transform);
            return screen;
        }

        private void CreateDimmer(Transform parent)
        {
            Image dimmer = CreateImage("Dimmer", parent, null, false);
            dimmer.color = new Color(Plum.r, Plum.g, Plum.b, 0.78f);
            Stretch(dimmer.rectTransform);
        }

        private Text CreateToggleButton(string name, Transform parent, Sprite icon, Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            Button button = CreateLabelledButton(name, parent, string.Empty, new Vector2(600f, 155f), position, action, icon);
            Text label = CreateText("Label", button.transform, string.Empty, 42, TextAnchor.MiddleCenter, Cream);
            Stretch(label.rectTransform, 140f, 20f, 25f, 20f);
            return label;
        }

        private Button CreateLabelledButton(string name, Transform parent, string label, Vector2 size,
            Vector2 position, UnityEngine.Events.UnityAction action, Sprite icon = null)
        {
            Image image = CreateImage(name, parent, pillButton, true, size, position);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColours();
            button.onClick.AddListener(action);
            if (icon != null)
            {
                CreateImage("Icon", image.transform, icon, true, new Vector2(size.y * 0.7f, size.y * 0.7f),
                    new Vector2((-size.x * 0.5f) + (size.y * 0.65f), 0f));
            }

            Text text = CreateText("Label", image.transform, label, Mathf.RoundToInt(size.y * 0.34f),
                TextAnchor.MiddleCenter, Cream);
            Stretch(text.rectTransform, icon == null ? 30f : size.y, 16f, 30f, 16f);
            return button;
        }

        private Button CreateIconButton(string name, Transform parent, Sprite sprite, Vector2 size,
            Vector2 position, UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(name, parent, sprite, true, size, position);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColours();
            button.onClick.AddListener(action);
            return button;
        }

        private static ColorBlock CreateButtonColours()
        {
            ColorBlock colours = ColorBlock.defaultColorBlock;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colours.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            colours.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.5f);
            colours.colorMultiplier = 1f;
            return colours;
        }

        private Button CreateTransparentButton(string name, Transform parent, Vector2 size, Vector2 position)
        {
            Image image = CreateImage(name, parent, null, false, size, position);
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect,
            Vector2? size = null, Vector2? position = null)
        {
            GameObject gameObject = CreateRect(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = size ?? Vector2.zero;
            rect.anchoredPosition = position ?? Vector2.zero;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size,
            TextAnchor alignment, Color colour, Vector2? dimensions = null, Vector2? position = null)
        {
            GameObject gameObject = CreateRect(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = colour;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = size;
            text.rectTransform.sizeDelta = dimensions ?? Vector2.zero;
            text.rectTransform.anchoredPosition = position ?? Vector2.zero;
            return text;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return gameObject;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
