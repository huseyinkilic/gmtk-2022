using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Linq;
using static CreatureController;
using static Move;

public class TurnManager : MonoBehaviour
{
    public enum Type
    {
        ATTACK, DEFEND, NEUTRAL
    }

    

    public class FieldState
    {
        // stub, in case we want to add this
        // would contain stuff like weather
    }

    public class SingleSidedFieldState 
    {
        // stub, in case we want to add this
        // would contain stuff like stealth rocks
    }

    public class TeamState
    {
        public CreatureState[] team;
    }

    public class State
    {
        public int turnNumber;

        public float luckBalance; // positive means in favor of player1, negative means in favor of player2
        public TeamState[] playersTeams;

        public FieldState fieldState; // stub, in case we want to add this
        public SingleSidedFieldState[] playersSideStates; // stub, in case we want to add this
    }

    public class PlayerAction
    {
        public enum ActionType { SWITCH, MOVE }

        public int team; // 0 for player 1, 1 for player 2
        public int targetTeam;
        public int madeOnTurn;
        public CreatureController activeCreature;

        public ActionType actionType;
        public CreatureController targetCreature; 
        public Move moveTaken;                    // this will be null if actionType is not MOVE
    }



    public class PlayerController // Temp, will make its own file later
    {
        public int teamNumber;
        public List<CreatureController> team;
        public CreatureController activeCreature;

        public void ForceSwitch() { UIInterface.Instance.ForceSwitch(this.teamNumber, TurnManager.Instance.HandleAction); }
    }

    public delegate void HandleActionDelegate(PlayerAction action);

    public static TurnManager Instance { get; private set; }


    public List<State> previousStates;
    public State currentState;
    public List<PlayerController> players;
    public List<PlayerAction> pendingActions;

    private Dictionary<PlayerController, PlayerAction> nextTurnActions = new();

    private void Start()
    {
        Instance = this;
    }

    public void SetPlayerAction(PlayerController player, PlayerAction action)
    {
        nextTurnActions[player] = action;
        foreach(PlayerController p in players) if (!nextTurnActions.ContainsKey(p)) return; // if not all players have picked an action for next turn, end the function

        RunTurn(nextTurnActions.Values.ToList());
        nextTurnActions.Clear();
    }

    private void RunTurn(List<PlayerAction> playerActions)
    {
        // increment the turn counter
        currentState.turnNumber++;

        // apply pendingMoves that should activate this turn
        var pendingActionIter = pendingActions.ToList();
        foreach(PlayerAction action in pendingActionIter)
        {
            if (currentState.turnNumber == action.madeOnTurn+action.moveTaken.delayTurns)
            {
                pendingActions.Remove(action);
                playerActions.Add(action);
            }
        }

        // sort actions by priority breaking ties by speed
        playerActions.Sort((PlayerAction a, PlayerAction b) => b.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[b.team]) - a.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[a.team]));
        playerActions.Sort((PlayerAction a, PlayerAction b) => GetPriority(b, currentState) - GetPriority(a, currentState));

        // before evaluating actions, apply start of turn effects and see if any creatures faint 
        foreach(PlayerController team in players)
        {
            if (team.activeCreature.CanStillFight()) team.activeCreature.ApplyStartOfTurnEffects();

            // we check CanStillFight() again in case ApplyStartOfTurnEffects() caused the creature to faint
            if (!team.activeCreature.CanStillFight())
            {
                // force the player to make a switch
                players[team.activeCreature.state.team].ForceSwitch();
            }
        }

        // make actions happen
        for(int i = 0; i < playerActions.Count; i++)
        {
            HandleAction(playerActions[i]);
        }

        CopyStateToStack();
    }

    private void HandleAction(PlayerAction action)
    {
        PlayerController player = players[action.team];
        SingleSidedFieldState playerFieldSideState = currentState.playersSideStates[action.team];

        if (!action.activeCreature.CanStillFight()) return; // creature fainted due to earlier action or ApplyStartOfTurnEffects. Cancel this action

        if (action.actionType == PlayerAction.ActionType.SWITCH)
        {
            // switch

            action.activeCreature.state.isActiveCreature = false;
            action.targetCreature.state.isActiveCreature = true;
                
            CreatureController switchFrom = player.team[action.targetCreature.state.indexOnTeam];
            CreatureController switchTo   = player.team[action.activeCreature.state.indexOnTeam];
            player.activeCreature = player.team[action.targetCreature.state.indexOnTeam];
                
            ApplySwapEffects(switchFrom, switchTo, currentState.fieldState, playerFieldSideState);
            UIInterface.Instance.SwapActiveCreature(player.teamNumber, switchTo);
        } 
        else
        {
            // move

            // check to see if this move should be pending
            if (action.moveTaken.delayTurns > 0)
            {
                pendingActions.Add(action);
                return;
            }

            // reset the action's target creature - if the other player switched this turn we HAVE to update this value
            action.targetCreature = players[action.targetTeam].activeCreature;
        
            // accuracy roll

            var moveHits = MakeBooleanRoll((float)action.moveTaken.accuracy/100f, action.team);
            if (!moveHits) return; // move does not hit, skip damage calc and do not apply secondary effects

            // damage
            int damage = CalculateDamage(action.moveTaken, action.activeCreature.state, action.targetCreature.state);
            action.targetCreature.TakeDamage(damage);

            if (!action.targetCreature.CanStillFight())
            {
                // force the player to make a switch
                players[action.targetCreature.state.team].ForceSwitch();
                return; // no secondary effects if the target faints
            }

            // secondary effects
            foreach(SecondaryEffect secondaryEffect in action.moveTaken.secondaryEffects ?? new())
            {
                SecondaryEffectHandler handler = GetSecondaryEffectHandler(secondaryEffect.name);
                if (handler == null) continue;

                handler.Invoke(currentState, action.targetCreature, action.activeCreature, secondaryEffect.parameters);
            }
        }

        // apply end of turn effects to all active creatures and check to see if any active creatures fainted for any reason
        foreach(PlayerController team in players)
        {
            if (team.activeCreature.CanStillFight()) team.activeCreature.ApplyEndOfTurnEffects();

            // we check CanStillFight() again in case ApplyEndOfTurnEffects() caused the creature to faint
            if (!team.activeCreature.CanStillFight())
            {
                // force the player to make a switch
                players[action.activeCreature.state.team].ForceSwitch();
                continue; // no secondary effects if the target faints
            }
        }
    }

    public delegate void SecondaryEffectHandler(State state, CreatureController applyTo, CreatureController applyFrom, string[] parameters);
    public SecondaryEffectHandler GetSecondaryEffectHandler(string secondaryEffectName)
    {
        switch (secondaryEffectName)
        {
            default: return null;
        }
        return null;
    }

    public int CalculateDamage(Move move, CreatureState attacker, CreatureState target)
    {
        float ADRatio = CreatureController.GetAttackStat(attacker) / CreatureController.GetDefenseStat(target);
        float STAB = move.type == (Move.Type)attacker.currentType ? 1.5f : 1; // STAB = same type attack bonus
        float effectiveness = GetMatchup(move.type, target.currentType);
        return Mathf.FloorToInt(move.basePower * ADRatio * STAB);
    }

    public float GetMatchup(Move.Type moveType, Creature.Type creatureType)
    {
        switch (moveType)
        {
            case Move.Type.ATTACK: 
                if (creatureType == Creature.Type.ATTACK) return 1;
                if (creatureType == Creature.Type.DEFEND) return 0.5f;
                if (creatureType == Creature.Type.NEUTRAL) return 2;
                return 1;
            case Move.Type.DEFEND:
                if (creatureType == Creature.Type.ATTACK) return 2;
                if (creatureType == Creature.Type.DEFEND) return 1f;
                if (creatureType == Creature.Type.NEUTRAL) return 0.5f;
                return 1;
            case Move.Type.NEUTRAL:
                if (creatureType == Creature.Type.ATTACK) return 0.5f;
                if (creatureType == Creature.Type.DEFEND) return 2f;
                if (creatureType == Creature.Type.NEUTRAL) return 1f;
                return 1;
            default:
                return 1;
        }
    }

    // for UI
    public float GetLuckAdustedAccuracy(int playerNum, Move move)
    {
        float relative = playerNum == 0 ? 1 : -1;
        float luckActivationChance = relative*currentState.luckBalance;
        float flatHitChance = move.accuracy;
        
        float chanceToMiss = (1-luckActivationChance) * (1-flatHitChance); // both of these rolls have to fail for this move to miss
        float chanceToHit = 1-chanceToMiss;

        return chanceToHit;
    }

    // this function implements the whole luck mechanic
    // NOTE: this is the only place that depends on there being only two players. To implement multiple players, we just have to give each player their own luckBalance stat (and add a "int[] teamsNegativelyAffectedByPositiveOutcome" parameter)
    public bool MakeBooleanRoll(float positiveOutcomeChance, int team)
    {
        bool success = false;
        float relative = team == 0 ? 1 : -1;
        float negativeOutcomeChance = (1-positiveOutcomeChance);

        // first, make a roll against the luck. if this succeeds, then the roll as a whole is considered successful
        if (Random.value < relative*currentState.luckBalance) success = true;

        // if the luck roll failed, roll against positiveOutcomeChance, if this succeeds, then the roll as a whole is considered successful
        if (Random.value < positiveOutcomeChance) success = true;

        // now influence the luck - if the roll succeeded, sway the luck in favor of the opposing team by (1-positiveOutcomeChance)
        // if the roll failed, sway the luck in favor of this team by positiveOutcomeChance
        if (success) currentState.luckBalance -= relative*negativeOutcomeChance; // sway the luck in favor of the opposing team by the chance of thier desired outcome (which did not happen)
        else         currentState.luckBalance += relative*positiveOutcomeChance; // sway the luck in favor of this team by the chance of thier desired outcome (which did not happen)

        return success;
    }

    private void CopyStateToStack()
    {
        previousStates.Add(DeepCopy(currentState));
    }

    private static int GetPriority(PlayerAction action, State state)
    {
        // state is here as a parameter in case we want to allow the state to affect priority later
        return action.actionType == PlayerAction.ActionType.SWITCH ? 9999 : action.moveTaken.priority;
    }

    private static void ApplySwapEffects(CreatureController from, CreatureController to, FieldState globalFieldState, SingleSidedFieldState playerSideFieldState)
    {
        // stub
    }


    // https://stackoverflow.com/a/11336951/9643841s
    static public T DeepCopy<T>(T obj)
    {
        BinaryFormatter s = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            s.Serialize(ms, obj);
            ms.Position = 0;
            T t = (T)s.Deserialize(ms);

            return t;
        }
    }
}
