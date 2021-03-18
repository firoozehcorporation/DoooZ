// <copyright file="GsLiveHandler.cs" company="Firoozeh Technology LTD">
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

using System.Threading.Tasks;
using FiroozehGameService.Core;
using Models;
using Newtonsoft.Json;

/**
* @author Alireza Ghodrati
*/

namespace Handlers
{
    public static class GsLiveHandler
    {
        public static async Task TakeTurn(int whoIsTurn , int pos ,int beforeSign, string opponentId)
        {
            var turnData = new TurnData
            {
                WhoTurn = whoIsTurn ,
                CurrentPositionSelect = pos,
                BeforeSign = beforeSign
            };

            var dataToSend = JsonConvert.SerializeObject(turnData);
            
            if(GameService.GSLive.IsTurnBasedAvailable())
               await GameService.GSLive.TurnBased().TakeTurn(dataToSend, opponentId);
        }

    }
}