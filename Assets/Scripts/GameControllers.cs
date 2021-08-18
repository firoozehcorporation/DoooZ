﻿// <copyright file="GameControllers.cs" company="Firoozeh Technology LTD">
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
using FiroozehGameService.Models.Enums.GSLive;
using FiroozehGameService.Models.GSLive;
using FiroozehGameService.Models.GSLive.Command;
using FiroozehGameService.Models.GSLive.TB;
using Handlers;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Debug = UnityEngine.Debug;

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
    
    private List<Member> _members;

    private bool _isSuccessfullyLogin;
    private bool _isMatchmaking;




    // Start is called before the first frame update
    private async void Start ()
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
        if (_isSuccessfullyLogin && !GameService.GSLive.IsCommandAvailable())
        {
            startGameBtn.interactable = false;
            Status.color = Color.red;
            Status.text = "GameService Not Connected";
        }
        else if (_isSuccessfullyLogin && GameService.GSLive.IsCommandAvailable())
        {
            if(!_isMatchmaking) startGameBtn.interactable = true;
            Status.color = Color.black;
            Status.text = "Status : Connected!";
        }
        
        
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        Application.Quit();
    }

    /// <summary>
    /// Connect To GameService -> Login Or SignUp
    /// It May Throw Exception
    /// </summary>
    private async Task ConnectToGamesService ()
    {
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
                await GameService.LoginOrSignUp.LoginWithToken(FileUtil.GetUserToken());
                
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
                            var userToken = await GameService.LoginOrSignUp.SignUp(nickName, email, pass);
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
                            var userToken = await GameService.LoginOrSignUp.Login(email, pass);
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
        TurnBasedEventHandlers.SuccessfullyLogined += OnSuccessfullyLogined;
        TurnBasedEventHandlers.Error += OnError;
        TurnBasedEventHandlers.Reconnected += Reconnected;
        TurnBasedEventHandlers.JoinedRoom += OnJoinRoom;
        TurnBasedEventHandlers.AcceptVoteReceived += AcceptVoteReceived;
        TurnBasedEventHandlers.AutoMatchUpdated += AutoMatchUpdated;
        TurnBasedEventHandlers.VoteReceived += VoteReceived;
        TurnBasedEventHandlers.ChoosedNext += OnChooseNext;
        TurnBasedEventHandlers.TakeTurn += OnTakeTurn;
        TurnBasedEventHandlers.LeftRoom += OnLeaveRoom;
        TurnBasedEventHandlers.CurrentTurnMemberReceived += OnCurrentTurnMember;
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

    private void OnCellClick (int buttonNumber) {
        // Is My Turn
        try
        {
            Debug.Log("TurnBased Available : " + GameService.GSLive.IsTurnBasedAvailable());
            if (!_currentTurnMember.User.IsMe) return;
            // Send TakeTurnData To Opponent
            GsLiveHandler.TakeTurn(_whoTurn == 1 ? 0 : 1, buttonNumber,_whoTurn,_opponent?.Id);
        }
        catch (Exception e)
        {
           Debug.LogError("OnCellClick Err : " + e);
        }
    }

    private void WinnerCheck(Member winner, Member loser) {
        var s1 = _markTabel[0] + _markTabel[1] + _markTabel[2];
        var s2 = _markTabel[3] + _markTabel[4] + _markTabel[5];
        var s3 = _markTabel[6] + _markTabel[7] + _markTabel[8];
        var s4 = _markTabel[0] + _markTabel[3] + _markTabel[6];
        var s5 = _markTabel[1] + _markTabel[4] + _markTabel[7];
        var s6 = _markTabel[2] + _markTabel[5] + _markTabel[8];
        var s7 = _markTabel[0] + _markTabel[4] + _markTabel[8];
        var s8 = _markTabel[0] + _markTabel[4] + _markTabel[6];
       
        var results = new[] { s1, s2, s3, s4, s5, s6, s7, s8 };
        
        var isDraw = _markTabel.All(m => m != -100);
        
        if (!isDraw && results.All(t => t != 3 * (_whoTurn + 1))) return;

        if (isDraw)
        {
            _outcomes.Add(winner.Id,new Outcome
            {
                Placement = 0,
                Result = "Draw"
            });
            _outcomes.Add(loser.Id,new Outcome
            {
                Placement = 0,
                Result = "Draw"
            });
        }
        else
        {
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
        }
        

        if (_whoTurn == 0) {
            _xPlayerScore++;
            xPlayerTextScore.text = _xPlayerScore.ToString();
        } else {
            _oPlayerScore++;
            oPlayerTextScore.text = _oPlayerScore.ToString();
        }
        
        // Send Result To Server
        GameService.GSLive.TurnBased().Vote(_outcomes);

        foreach (var button in Spaces)
            button.enabled = false;
    }

    
    private void Reconnected(object sender, ReconnectStatus status)
    {
        Debug.Log("Reconnected : " + status);
    }

    private void OnCurrentTurnMember(object sender, Member currentMember)
    {
        Debug.Log("OnCurrentTurnMember : " + currentMember.Name);

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

    
    private void OnTakeTurn(object sender, Turn turn)
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
                WinnerCheck(winner,loser);
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

    private void VoteReceived(object sender, Vote vote)
    {
        try
        {
            var ok = true;
            var opponentData = vote.Outcomes;
            // if All Data is Compatible -> Complete With Winner 
            foreach (var meOutcome in _outcomes)
            {
                if (opponentData[meOutcome.Key]?.Placement == meOutcome.Value.Placement &&
                    opponentData[meOutcome.Key]?.Result == meOutcome.Value.Result) continue;
                
                ok = false;
                break;
            }
                    
            if(ok)
                 GameService.GSLive.TurnBased().AcceptVote(vote.Submitter.Id);
            
        }
        catch (Exception e)
        {
            Turn.text = "VoteReceived Err : " + e.Message;
            Debug.LogError("VoteReceived Err : " + e.Message);
        }
        
    }

   
    private void AcceptVoteReceived(object sender, AcceptVote acceptVote)
    {
        try
        {
            RestartGame.SetActive(true);
            RestartGame.GetComponent<Button>().onClick.AddListener(ResetGame);
            // Show Winner
            Turn.color = Color.magenta;
            
            if (acceptVote.Result.All(r => r.Value.Result == "Draw" && r.Value.Placement == 0)) Turn.text = "Draw!";
            else Turn.text = acceptVote.Result.Any(o => o.Key == _me.Id && o.Value.Placement == 1) ? "You wins!" : "Opponent wins!";
        }
        catch (Exception e)
        {
            Turn.text = "AcceptVoteReceived Err : " + e.Message;
            Debug.LogError("AcceptVoteReceived Err : " + e.Message);
        }
    }

    private void OnJoinRoom(object sender, JoinEvent e)
    {

        Debug.Log("OnJoinRoom");
        Debug.Log("JoinedPlayers : " + e.JoinData.RoomData.JoinedPlayers);

        _isMatchmaking = false;


        try
        {
            startMenu.enabled = false;
            GamePlay.enabled = true;

            if (e.JoinData.JoinedMember.User.IsMe)
            {
                _me = e.JoinData.JoinedMember;
                
                // get current turn
                GameService.GSLive.TurnBased().GetCurrentTurnMember();
                Debug.Log("Getting Room Data...");
            }
            else _opponent = e.JoinData.JoinedMember;
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
        Debug.LogError("GameService Err : " + e.Error + " , From : " + e.Type);
    }

    private void OnSuccessfullyLogined(object sender, EventArgs e)
    {
        try
        {
            _isSuccessfullyLogin = true;
            Status.text = "Status : Connected!";
            startGameBtn.interactable = true;
        
            // Start Game
            startGameBtn.onClick.AddListener(() =>
            {
                if (GameService.GSLive.IsCommandAvailable())
                {
                    _isMatchmaking = true;
                    GameService.GSLive.TurnBased().AutoMatch(
                        new GSLiveOption.AutoMatchOption("partner"));
                    
                    // go to waiting ui
                    startGameBtn.interactable = false;
                    startMenuText.text = "MatchMaking...";
                }
                else
                {
                    startGameBtn.interactable = false;
                    Status.color = Color.red;
                    Status.text = "GameService Not Connected";
                }

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
        Debug.Log("AutoMatchUpdated :" + e.Status);
        foreach (var member in e.Players)
        {
            Debug.Log(member.User.Name);
        }
       
    }

    private void ResetGame()
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

