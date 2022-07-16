using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TurnManager;

public class SimpleAI : IAI
{
    public PlayerAction GetAction(int playerNum, GameState state)
    {
        var myActiveCreature = ITurnManager.Instance.GetActiveCreature(playerNum);
        var opponentActiveCreature = ITurnManager.Instance.GetActiveCreature(1-playerNum);
        
        var myMoves = myActiveCreature.state.knownMoves.ToList();
        var movesByTypeMatchup = myMoves.ToList();
        movesByTypeMatchup.Sort((a, b) =>
            {
                var aEffectiveness = ITurnManager.Instance.GetMatchup(a.type, opponentActiveCreature.state.currentType);
                var bEffectiveness = ITurnManager.Instance.GetMatchup(b.type, opponentActiveCreature.state.currentType);
                    
                if (aEffectiveness == bEffectiveness) return 0;
                if (aEffectiveness > bEffectiveness) return 1;
                if (aEffectiveness < bEffectiveness) return -1;

                return 0;
            }
        );

        var chosenMove = movesByTypeMatchup[0];
        return ITurnManager.Instance.MakeMoveAction(playerNum, myMoves.IndexOf(chosenMove), opponentActiveCreature);
    }

    public PlayerAction ForceSwitch(int playerNum, GameState state)
    {
        var switchingTo = state.playersTeams[playerNum].team.Where(c => c.currentDamage < c.definition.hp).First();
        return ITurnManager.Instance.MakeSwitchAction(playerNum, switchingTo.indexOnTeam);
    }
}
