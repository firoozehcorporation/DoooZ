// <copyright file="GameControllers.cs" company="Firoozeh Technology LTD">
// Copyright (C) 2019 Firoozeh Technology LTD. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>



using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiroozehGameService.Core;
using FiroozehGameService.Core.GSLive;
using FiroozehGameService.Handlers;
using FiroozehGameService.Models;
using FiroozehGameService.Models.GSLive;
using FiroozehGameService.Models.GSLive.Command;
using FiroozehGameService.Models.GSLive.TB;
using FiroozehGameService.Utils;
using Handlers;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using LogType = FiroozehGameService.Utils.LogType;

/**
* @author Alireza Ghodrati
*/


public class GameControllers : MonoBehaviour {
   
    public Canvas startMenu;
    public Canvas LoginCanvas;
    public Canvas GamePlay;
    public Button startGameBtn;
    public Text startMenuText;
    public Text Status;
    
    
    public InputField NickName;
    public InputField Email;
    public InputField Password;
    public Button Submit;
    public GameObject SwitchToRegisterOrLogin;
    public Text LoginErr;

    
    

    private int _whoTurn; // 0 -> X, 1 -> O
    private int _turnCount;
    public GameObject[] turnIcons; //turn UI signs
    public Sprite[] playIcons;
    public Button[] Spaces;
    private int[] _markTabel; // which player select which button
    private int _xPlayerScore = 0;
    private int _oPlayerScore = 0;
    public Text oPlayerTextScore;
    public Text xPlayerTextScore;
    public GameObject RestartGame;
    public Text Turn;


    private Member _me,_opponent,_currentTurnMember,_whoIsX;
    private Dictionary<string, Outcome> _outcomes;
    
    private List<Member> members;
    
    
   
    
    // Start is called before the first frame update
    async void Start ()
    {
        try
        {
            GameInit ();
            SetEventListeners();
            await ConnectToGamesService();
        }
        catch (Exception e)
        {
            Status.text = "Start Err : " + e.Message;
            Debug.LogError("Start Err : " + e.Message);
        }
      
    }
    
    
    // Update is called once per frame
    void Update ()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        GameService.Logout();
        Application.Quit();
    }

    /// <summary>
    /// Get Last Save User Data
    /// It May Throw Exception
    /// </summary>
    private async Task GetSaveData()
    {
        try
        {
            var save = await GameService.GetSaveGame<Save>();
            FileUtil.SaveWins(save.WinCounts);
            Debug.Log("GetSaveData Wins : " + save.WinCounts);
        }
        catch (Exception e)
        {
            Debug.LogError("GetSaveData Err : " + e.Message);
        }
    }

    
    /// <summary>
    /// Connect To GameService -> Login Or SignUp
    /// It May Throw Exception
    /// </summary>
    private async Task ConnectToGamesService () {
        //connecting to GamesService
        Status.text = "Status : Connecting...";
        startGameBtn.interactable = false;
        SwitchToRegisterOrLogin.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (NickName.IsActive())
            {
                NickName.gameObject.SetActive(false);
                SwitchToRegisterOrLogin.GetComponent<Text>().text = "Dont have an account? Register!";
            }
            else
            {
                NickName.gameObject.SetActive(true);
                SwitchToRegisterOrLogin.GetComponent<Text>().text = "Have an Account? Login!";
            }
        });

        if (FileUtil.IsLoginBefore())
        {
            try
            {
                await GameService.Login(FileUtil.GetUserToken());
                
                // Disable LoginUI
                startMenu.enabled = true;
                LoginCanvas.enabled = false;
            }
            catch (Exception e)
            {
                Status.color = Color.red;
                if (e is GameServiceException) Status.text = "GameServiceException : " + e.Message;
                else Status.text = "InternalException : " + e.Message;
            }
           
        }
        else
        {
            // Enable LoginUI
            startMenu.enabled = false;
            LoginCanvas.enabled = true;
            
            Submit.onClick.AddListener(async () =>
            {
                try
                {
                    if (NickName.IsActive()) // is SignUp
                    {
                        var nickName = NickName.text.Trim();
                        var email = Email.text.Trim();
                        var pass = Password.text.Trim();

                        if (string.IsNullOrEmpty(nickName)
                            && string.IsNullOrEmpty(email)
                            && string.IsNullOrEmpty(pass))
                            LoginErr.text = "Invalid Input!";
                        else
                        {
                            var userToken = await GameService.SignUp(nickName, email, pass);
                            FileUtil.SaveUserToken(userToken);
                        }

                    }
                    else
                    {
                        var email = Email.text.Trim();
                        var pass = Password.text.Trim();

                        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(pass))
                            LoginErr.text = "Invalid Input!";
                        else
                        {
                            var userToken = await GameService.Login(email, pass);
                            FileUtil.SaveUserToken(userToken);
                            
                            // Disable LoginUI
                            startMenu.enabled = true;
                            LoginCanvas.enabled = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e is GameServiceException) LoginErr.text = "GameServiceException : " + e.Message;
                    else LoginErr.text = "InternalException : " + e.Message;
                }
               
            });
        }        
    }

     /// <summary>
    /// Set Event Listeners
    /// </summary>
    private void SetEventListeners()
    {
        CoreEventHandlers.SuccessfullyLogined += OnSuccessfullyLogined;
        TurnBasedEventHandlers.Error += OnError;
                
        TurnBasedEventHandlers.JoinedRoom += OnJoinRoom;
        TurnBasedEventHandlers.Completed += OnCompleted;
        TurnBasedEventHandlers.AutoMatchUpdated += AutoMatchUpdated;
        TurnBasedEventHandlers.Finished += OnFinished;
        TurnBasedEventHandlers.ChoosedNext += OnChooseNext;
        TurnBasedEventHandlers.TakeTurn += OnTakeTurn;
        TurnBasedEventHandlers.LeftRoom += OnLeaveRoom;
        TurnBasedEventHandlers.RoomMembersDetailReceived += OnRoomMembersDetailReceived;
        TurnBasedEventHandlers.CurrentTurnMemberReceived += OnCurrentTurnMember;

        LogUtil.LogEventHandler += LogEventHandler;
    }

     


     private void LogEventHandler(object sender, Log e)
    {
       if(e.Type == LogType.Normal) Debug.Log(e.Txt);
        else Debug.LogError(e.Txt);
    }


    private void GameInit () {
        _markTabel = new int[9];
        _outcomes = new Dictionary<string, Outcome>();

        _whoTurn = 0;
        turnIcons[0].GetComponent<Image>().enabled = true;
        turnIcons[1].GetComponent<Image>().enabled = false;
        
        Turn.text = null;
        _turnCount = 0;
        

        SetButtonListeners();
        foreach (var button in Spaces)
        {
            button.interactable = true;
            button.image.enabled = true;
        }
        
        for (var i = 0; i < _markTabel.Length; i++) 
            _markTabel[i] = -100; //noBody
        
    }

    private void SetButtonListeners()
    {
        Spaces[0].onClick.AddListener(() =>
        {
             OnCellClick(0);
        });
        Spaces[1].onClick.AddListener(() =>
        {
             OnCellClick(1);
        });
        Spaces[2].onClick.AddListener(() =>
        {
             OnCellClick(2);
        });
        Spaces[3].onClick.AddListener(() =>
        {
             OnCellClick(3);
        });
        Spaces[4].onClick.AddListener(() =>
        {
             OnCellClick(4);
        });
        Spaces[5].onClick.AddListener(() =>
        {
             OnCellClick(5);
        });
        Spaces[6].onClick.AddListener(() =>
        {
             OnCellClick(6);
        });
        Spaces[7].onClick.AddListener(() =>
        {
             OnCellClick(7);
        });
        Spaces[8].onClick.AddListener(() =>
        {
             OnCellClick(8);
        });

    }

    private async void OnCellClick (int buttonNumber) {
        // Is My Turn
        try
        {
            if (!_currentTurnMember.User.IsMe) return;
            // Send TakeTurnData To Opponent
            await GsLiveHandler.TakeTurn(_whoTurn == 1 ? 0 : 1, buttonNumber,_whoTurn,_opponent.Id);
        }
        catch (Exception e)
        {
           Debug.LogError("OnCellClick Err : " + e);
        }
    }

    private async Task<bool> WinnerCheck (Member winner,Member loser) {
        var s1 = _markTabel[0] + _markTabel[1] + _markTabel[2];
        var s2 = _markTabel[3] + _markTabel[4] + _markTabel[5];
        var s3 = _markTabel[6] + _markTabel[7] + _markTabel[8];
        var s4 = _markTabel[0] + _markTabel[3] + _markTabel[6];
        var s5 = _markTabel[1] + _markTabel[4] + _markTabel[7];
        var s6 = _markTabel[2] + _markTabel[5] + _markTabel[8];
        var s7 = _markTabel[0] + _markTabel[4] + _markTabel[8];
        var s8 = _markTabel[0] + _markTabel[4] + _markTabel[6];
       
        var results = new[] { s1, s2, s3, s4, s5, s6, s7, s8 };
        if (results.All(t => t != 3 * (_whoTurn + 1))) return false;
      
        if (_whoTurn == 0) {
            _xPlayerScore++;
            xPlayerTextScore.text = _xPlayerScore.ToString ();
        } else {
            _oPlayerScore++;
            oPlayerTextScore.text = _oPlayerScore.ToString ();
          
        }
        
         
        _outcomes.Add(winner.Id,new Outcome
        {
            Placement = 1,
            Result = "Win"
        });
        _outcomes.Add(loser.Id,new Outcome
        {
            Placement = 2,
            Result = "GameOver"
        });
        // Send Result To Server
        await GameService.GSLive.TurnBased.Finish(_outcomes);

        foreach (var button in Spaces)
            button.enabled = false;

        return true;

    }

    
    private void OnCurrentTurnMember(object sender, Member currentMember)
    {
        try
        {
            _currentTurnMember = currentMember;
             
            // Only Set In First
            if(_whoIsX == null)
                _whoIsX = currentMember;
        
            if (_currentTurnMember != null)
                Turn.text = _currentTurnMember.User.IsMe ? "You Turn" : "Opponent Turn";
            else Turn.text = null;

        }
        catch (Exception e)
        {
            Turn.text = "OnCurrentTurnMember Err : " + e.Message;
            Debug.LogError("OnCurrentTurnMember Err : " + e.Message);
        }
       
    }
    
    private void OnRoomMembersDetailReceived(object sender, List<Member> members)
    {
        // Set Players Info 
        foreach (var member in members)
        {
            if (member.User.IsMe) _me = member;
            else _opponent = member;
        }
    }

    private void OnLeaveRoom(object sender, Member e)
    {
        Debug.Log("OnLeaveRoom : " + e.Name);
        try
        {
            Turn.text = "Opponent Left The Game";

            RestartGame.SetActive(true);
            RestartGame.GetComponent<Button>().onClick.AddListener(ResetGame);
        }
        catch (Exception exception)
        {
            Turn.text = "OnLeaveRoom Err : " + exception.Message;
            Debug.LogError("OnLeaveRoom Err : " + exception.Message);
        }
       
    }

    
    private async void OnTakeTurn(object sender, Turn turn)
    {
        Debug.Log("OnTakeTurn : " + turn.WhoTakeTurn.Name);
        try
        {
            var turnData = JsonConvert.DeserializeObject<TurnData>(turn.Data);
            
            var buttonNumber = turnData.CurrentPositionSelect;
            Spaces[buttonNumber].image.enabled = true;
            Spaces[buttonNumber].interactable = false;
            
            // Update Before Turn
            Spaces[buttonNumber].image.sprite = playIcons[turnData.BeforeSign];

            _markTabel[buttonNumber] = _whoTurn + 1; // mark button for player // +1 is for logics
            _turnCount++;
            if (_turnCount > 4)
            {
                var winner = turn.WhoTakeTurn;
                var loser = winner.Id == _me.Id ? _opponent : _me;
                await WinnerCheck(winner,loser);
            }

            _whoTurn = turnData.WhoTurn;
            // _whoTurn == 0 is X , _whoTurn == 1 is O
            turnIcons[0].GetComponent<Image>().enabled = _whoTurn == 0;
            turnIcons[1].GetComponent<Image>().enabled = _whoTurn == 1;
           
        }
        catch (Exception e)
        {
            Turn.text = "OnTakeTurn Err : " + e.Message;
            Debug.LogError("OnTakeTurn Err : " + e.Message);
        }
              
    }

    private void OnChooseNext(object sender, Member whoIsNext)
    {
        Debug.Log("OnChooseNext : " + whoIsNext.Name);
        try
        {
            _currentTurnMember = whoIsNext.User.IsMe ? _me : _opponent;
            if (_currentTurnMember != null)
                Turn.text = _currentTurnMember.User.IsMe ? "You Turn" : "Opponent Turn";
            else Turn.text = null;
          
        }
        catch (Exception e)
        {
            Turn.text = "OnChooseNext Err : " + e.Message;
            Debug.LogError("OnChooseNext Err : " + e.Message);
        }
      
    }

    private async void OnFinished(object sender, Finish finish)
    {
        try
        {
            var ok = false;
            // if All Data is Compatible -> Complete With Winner 
            foreach (var outcome in finish.Outcomes)
                foreach (var meOutcome in _outcomes)
                    if (outcome.Key == meOutcome.Key && outcome.Value.Placement == meOutcome.Value.Placement)
                        ok = true;
                    
            if(ok)
                await GameService.GSLive.TurnBased.Complete(finish.Outcomes.First(o => o.Value.Placement == 1).Key);
            
        }
        catch (Exception e)
        {
            Turn.text = "OnFinished Err : " + e.Message;
            Debug.LogError("OnFinished Err : " + e.Message);
        }
       
    }

    private async void OnCompleted(object sender, Complete complete)
    {
        try
        {
            RestartGame.SetActive(true);
            RestartGame.GetComponent<Button>().onClick.AddListener(ResetGame);
            // Show Winner
            Turn.color = Color.magenta;
            Turn.text = complete.Result.Any(o => o.Key == _me.Id && o.Value.Placement == 1) ? "You wins!" : "Opponent wins!";
            
            // You Win!
            if(complete.Result.Any(o => o.Key == _me.Id && o.Value.Placement == 1))
                await FileUtil.IncreaseWin();
        }
        catch (Exception e)
        {
            Turn.text = "OnCompleted Err : " + e.Message;
            Debug.LogError("OnCompleted Err : " + e.Message);
        }
       
    }

    private async void OnJoinRoom(object sender, JoinEvent e)
    {

        try
        {
            startMenu.enabled = false;
            GamePlay.enabled = true;

            if (e.JoinData.JoinedMember.User.IsMe)
                _me = e.JoinData.JoinedMember;
            else _opponent = e.JoinData.JoinedMember;
                    
            // Get Players Info
            if(_me == null || _opponent == null)
                await GameService.GSLive.TurnBased.GetRoomMembersDetail();

            // Get CurrentTurn Info
            if (_currentTurnMember == null)
                await GameService.GSLive.TurnBased.GetCurrentTurnMember();
        }
        catch (Exception exception)
        {
            Turn.text = "OnJoinRoom Err : " + exception.Message;
            Debug.LogError("OnJoinRoom Err : " + exception.Message);
        }

    }

    private void OnError(object sender, ErrorEvent e)
    {
        Status.color = Color.red;
        Status.text = "Error : " + e.Error + " , From : " + e.Type;
    }

    private async void OnSuccessfullyLogined(object sender, EventArgs e)
    {
        try
        {
            await GetSaveData();
            Status.text = "Status : Connected!";
            startGameBtn.interactable = true;
        
            // Start Game
            startGameBtn.onClick.AddListener(async () =>
            {
                await GameService.GSLive.TurnBased.AutoMatch(new GSLiveOption.AutoMatchOption("partner",2,2));

                // go to waiting ui
                startGameBtn.interactable = false;
                startMenuText.text = "MatchMaking...";
            });
        }
        catch (Exception exception)
        {
            Debug.LogError("OnSuccessfullyLogined Err : " + exception.Message);
            Status.text = "OnSuccessfullyLogined Err : " + exception.Message;
        }
       
    }
    
    private void AutoMatchUpdated(object sender, AutoMatchEvent e)
    {
        foreach (var member in e.Players)
        {
            Debug.Log(member.Name);
        }
       
    }

    private async void ResetGame()
    {
        // Show MainUI
        startMenu.enabled = true;
        GamePlay.enabled = false;
        Status.text = "Status : Connected!";
        startMenuText.text = "DoooZ!";
        startGameBtn.interactable = true;
        RestartGame.SetActive(false);
        _oPlayerScore = 0;
        _xPlayerScore = 0;
        
        // Leave Game
        await GameService.GSLive.TurnBased.LeaveRoom();

        Turn.text = null;
        _outcomes.Clear();
        _me = null;
        _opponent = null;
        _currentTurnMember = null;
        _whoIsX = null;
        _whoTurn = 0;
        _turnCount = 0;

        foreach (var button in Spaces)
        {
            button.interactable = true;
            button.enabled = true;
            button.image.sprite = null;
        }

        for (var i = 0; i < _markTabel.Length; i++) 
            _markTabel[i] = -100; //noBody
    }
    

}

