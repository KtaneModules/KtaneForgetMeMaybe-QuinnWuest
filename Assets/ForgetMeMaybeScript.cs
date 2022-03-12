using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class ForgetMeMaybeScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMBombModule Module;
    public KMBossModule Boss;

    public KMSelectable[] ButtonSels;
    public Renderer[] Lights;
    public Material[] LightColors;

    public TextMesh DisplayScreen;
    public TextMesh InputScreen;
    public TextMesh StageScreen;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private string[] _ignoredModules;
    private int _solveCount;
    private int _currentSolves;
    private int _currentStage;

    private List<int> _displayedDigits = new List<int>();
    private List<int> _addedDigits = new List<int>();
    private List<int> _calculatedDigits = new List<int>();

    private int _snSum;
    private int _alphNum = 99;

    private int _inputIx;
    private int _inputLength;
    private bool _isInputting;
    private bool _hereWeGo = true;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);

        DisplayScreen.text = "";
        InputScreen.text = "";
        StageScreen.text = "";
        foreach (var ch in BombInfo.GetSerialNumber())
        {
            if (ch >= '0' && ch <= '9')
                _snSum += ch - '0';
            else if (_alphNum == 99)
                _alphNum = (ch - 'A' + 1) % 10;
        }
        StartCoroutine(Init());
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonSels[btn].transform);
            ButtonSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            if (!_isInputting)
            {
                Module.HandleStrike();
                Debug.LogFormat("[Forget Me Maybe #{0}] Pressed a button before input was expected. Strike.", _moduleId);
                return false;
            }
            if ((btn + 1) % 10 == _calculatedDigits[_inputIx])
            {
                _inputIx++;
                LedSetup();
                StageScreen.text = "--";
                DisplayInputScreen();
                if (_hereWeGo && _inputIx != _inputLength)
                {
                    Audio.PlaySoundAtTransform("HereWeGo", transform);
                    _hereWeGo = false;
                }
            }
            else
            {
                Module.HandleStrike();
                LedSetup(_displayedDigits[_inputIx]);
                Debug.LogFormat("[Forget Me Maybe #{0}] Incorrectly pressed {1} at stage {2}. Strike.", _moduleId, (btn + 1) % 10, _inputIx + 1);
                _hereWeGo = true;
            }
            if (_inputIx == _inputLength)
            {
                _moduleSolved = true;
                Module.HandlePass();
                Debug.LogFormat("[Forget Me Maybe #{0}] Module solved!", _moduleId);
                Audio.PlaySoundAtTransform("Reddit", transform);
            }
            return false;
        };
    }

    private IEnumerator Init()
    {
        yield return null;
        if (_ignoredModules == null)
            _ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Forget Me Maybe", new string[] {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "The Board Walk",
                "Busy Beaver",
                "Don't Touch Anything",
                "Doomsday Button",
                "Duck Konundrum",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Maybe",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForget",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn the Key",
                "The Twin",
                "Twister",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout",
                "Zener Cards"
            });
        _solveCount = BombInfo.GetSolvableModuleNames().Count(i => !_ignoredModules.Contains(i));
        if (_solveCount == 0)
        {
            _moduleSolved = true;
            Module.HandlePass();
            Debug.LogFormat("[Forget Me Maybe #{0}] No non-ignored modules were detected. Solving module...", _moduleId);
            yield break;
        }
        Debug.LogFormat("[Forget Me Maybe #{0}] Logging is done in the format: Current stage, displayed digit, added digit, calculated digit.", _moduleId);
        StartCoroutine(GenerateDigits());
    }

    private void Update()
    {
        if (_moduleSolved)
            return;
        _currentSolves = BombInfo.GetSolvedModuleNames().Count(i => !_ignoredModules.Contains(i));
    }

    private IEnumerator GenerateDigits()
    {
        while (_currentSolves != _solveCount)
        {
            var waitTime = Rnd.Range(45f, 60f);
            var elapsed = 0f;
            while (elapsed < waitTime)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (_currentSolves == _solveCount && elapsed >= 7f)
                {
                    _inputLength = _displayedDigits.Count();
                    if (_inputLength == 0)
                    {
                        Debug.LogFormat("[Forget Me Maybe #{0}] No stages were generated. Solving module...", _moduleId);
                        _moduleSolved = true;
                        Module.HandlePass();
                        yield break;
                    }
                    var str = "";
                    for (int i = 0; i < _calculatedDigits.Count; i++)
                    {
                        str += _calculatedDigits[i].ToString();
                        if (i % 3 == 2 && i != _calculatedDigits.Count - 1)
                            str += " ";
                    }
                    Debug.LogFormat("[Forget Me Maybe #{0}] Final calculated sequence: {1}", _moduleId, str);
                    PrepareInput();
                    yield break;
                }
            }
            Audio.PlaySoundAtTransform("Stage", transform);
            int r = Rnd.Range(0, 10);
            _displayedDigits.Add(r);
            _currentStage = _displayedDigits.Count() - 1;
            StageScreen.text = ((_currentStage + 1) % 100).ToString("00");
            DisplayScreen.text = _displayedDigits[_currentStage].ToString();
            int t;
            if (_currentStage == 0)
            {
                var s = _snSum.ToString();
                if (BombInfo.GetBatteryCount() % 2 == 0)
                    t = s[0] - '0';
                else
                    t = s[s.Length - 1] - '0';
            }
            else if (_currentStage == 1)
            {
                var c = BombInfo.GetSolvableModuleNames().Count().ToString();
                if (BombInfo.GetPortCount() % 2 == 1)
                    t = c[0] - '0';
                else
                    t = c[c.Length - 1] - '0';
            }
            else
            {
                if (_calculatedDigits[_currentStage - 2] == 0 || _calculatedDigits[_currentStage - 1] == 0)
                    t = _alphNum;
                else if (_calculatedDigits[_currentStage - 2] % 2 == 0 && _calculatedDigits[_currentStage - 1] % 2 == 0)
                    t = (BombInfo.GetIndicators().Count() + BombInfo.GetPortCount()) % 10;
                else
                    t = (_calculatedDigits[_currentStage - 2] + _calculatedDigits[_currentStage - 1]) % 10;
            }
            _addedDigits.Add(t);
            _calculatedDigits.Add((r + t) % 10);
            Debug.LogFormat("[Forget Me Maybe #{0}] Stage {1}: {2} {3} {4}", _moduleId, _currentStage + 1, _displayedDigits[_currentStage], _addedDigits[_currentStage], _calculatedDigits[_currentStage]);
            yield return null;
        }
    }

    private void PrepareInput()
    {
        _isInputting = true;
        StageScreen.text = "--";
        DisplayScreen.text = "";
        DisplayInputScreen();
    }

    private void DisplayInputScreen()
    {
        string displayedText = "";
        int currentStage = _inputIx;
        int startingStage = 0;
        int finalStage = _inputLength;

        while (currentStage > 23 && finalStage != 24)
        {
            currentStage -= 12;
            finalStage -= 12;
            startingStage += 12;
        }

        for (int i = startingStage; i < Math.Min(startingStage + 24, _inputLength); i++)
        {
            string d = "-";
            if (i < _inputIx)
                d = _calculatedDigits[i].ToString();
            if (i > startingStage)
            {
                if (i % 3 == 0)
                {
                    if (i % 12 == 0)
                        displayedText += "\n";
                    else
                        displayedText += " ";
                }
            }
            displayedText += d;
        }
        InputScreen.text = displayedText;
    }

    private void LedSetup(int? led = null)
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            if (led != null && (led + 9) % 10 == i)
                Lights[i].material = LightColors[1];
            else
                Lights[i].material = LightColors[0];
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 1234567890 [Presses buttons 1234567890.] | You can use 'press' or 'submit'. Commands may contain spaces.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        int cut;
        if (command.StartsWith("submit "))
            cut = 7;
        else if (command.StartsWith("press "))
            cut = 6;
        else
        {
            yield return "sendtochaterror Use either 'press' or 'submit' followed by a number sequence.";
            yield break;
        }
        var digits = new List<int>();
        var strSplit = command.Substring(cut).ToCharArray();
        foreach (var c in strSplit)
        {
            if (!"0123456789 ".Contains(c))
            {
                yield return "sendtochaterror Invalid character in number sequence: '" + c + "'.\nValid characters are 0-9 and spaces.";
                yield break;
            }
            if (c >= '0' && c <= '9')
                digits.Add(c - '0');
        }
        if (digits.Count == 0)
            yield break;
        yield return null;
        yield return "solve";
        yield return "awardpointsonsolve " + _inputLength.ToString();
        foreach (int d in digits)
        {
            ButtonSels[(d + 9) % 10].OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isInputting)
            yield return true;
        while (!_moduleSolved)
        {
            ButtonSels[(_calculatedDigits[_inputIx] + 9) % 10].OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
    }
}