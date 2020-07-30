// <copyright file="LeaderBoardHandler.cs" company="Firoozeh Technology LTD">
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
using FiroozehGameService.Models.BasicApi;
using Statics;

/**
* @author Alireza Ghodrati
*/

namespace Handlers
{
    /// <summary>
    /// Represents LeaderBoardHandler
    /// </summary>
    public static class LeaderBoardHandler
    {
        
        
        public static async Task<SubmitScoreResponse> SubmitScore(int score)
        {
            if (GameService.IsAuthenticated())
               return await GameService.SubmitScore(GameServiceIds.DoooZMasters, score);
            return null;
        }
    }
}