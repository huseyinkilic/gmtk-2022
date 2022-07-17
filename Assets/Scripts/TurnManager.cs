using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Linq;
using static CreatureController;
using static Move;
using System;

using Random = UnityEngine.Random;

public class TurnManager : MonoBehaviour, ITurnManager
{
    [Serializable]
    public enum Type
    {
        ATTACK, DEFEND, NEUTRAL
    }

    
    [Serializable]
    public class FieldState
    {
        // stub, in case we want to add this
        // would contain stuff like weather
    }

    [Serializable]
    public class SingleSidedFieldState 
    {
        // stub, in case we want to add this
        // would contain stuff like stealth rocks
    }

    [Serializable]
    public class TeamState
    {
        public CreatureState[] team;
        public bool luckSpendingEnabled;
    }

    [Serializable]
    public class GameState
    {
        public int turnNumber;

        public float luckBalance; // positive means in favor of player1, negative means in favor of player2
        public TeamState[] playersTeams = new TeamState[2]; // will be set in the Initialize function

        public FieldState fieldState; // stub, in case we want to add this
        public SingleSidedFieldState[] playersSideStates = new SingleSidedFieldState[2]; // stub, in case we want to add this
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

    public delegate void HandleActionDelegate(PlayerAction action);

    public static TurnManager Instance { get; private set; }


    public List<GameState> previousStates = new();
    public GameState currentState = new();
    public List<PlayerController> players = new();
    public List<PlayerAction> pendingActions = new();

    private Dictionary<PlayerController, PlayerAction> nextTurnActions = new();

    private void Start()
    {
        Instance = this;
        ITurnManager.Instance = this;
        players = new();
        
        for (int i = 0; i < 2; i++) players.Add(null);
    }

    //
    // Interface functions
    // 

    public int TurnNumber { get { return currentState.turnNumber; } }
    public float LuckBalance { get { return currentState.luckBalance; } }
    public GameState CurrentGameState { get { return currentState; } }
    
    public Move[] GetUsableMoves(int playerNum)
    {
        return players[playerNum].activeCreature.state.knownMoves;
    }

    public int GetActiveCreatureCurrentHP(int playerNum)
    {
        CreatureState s = players[playerNum].activeCreature.state;
        return s.definition.hp - s.currentDamage;
    }
    public int GetActiveCreatureMaxHP(int playerNum)
    {
        return players[playerNum].activeCreature.state.definition.hp;
    }
    public List<CreatureController> GetPlayerCreatures(int playerNum)
    {
        return players[playerNum].team;
    }

    public CreatureController GetActiveCreature(int playerNum)
    {
        return players[playerNum].activeCreature;
    }

    public float GetLuckAdustedAccuracy(int playerNum, Move move)
    {
        float relative = playerNum == 0 ? 1 : -1;
        float luckActivationChance = relative*currentState.luckBalance;
        float flatHitChance = move.accuracy/100f;
        
        float chanceToMiss = (1-luckActivationChance) * (1-flatHitChance); // both of these rolls have to fail for this move to miss
        float chanceToHit = 1-chanceToMiss;

        return chanceToHit;
    }

    
    public PlayerAction MakeSwitchAction(int playerNum, int switchToIndex)
    {
        PlayerController player = players[playerNum];
        PlayerAction action = player.MakeSwitchAction(switchToIndex);
        //SubmitPlayerAction(player, action);
        return action;
    }

    public PlayerAction MakeMoveAction(int playerNum, int moveIndex, CreatureController targetCreature)
    {
        PlayerController player = players[playerNum];
        PlayerAction action = player.MakeMoveAction(moveIndex, targetCreature);
        //SubmitPlayerAction(player, action);
        return action;
    }

    public void SubmitAction(PlayerAction action)
    {
        Debug.LogWarning($"Player action queued (player {action.team+1})");
        QueuePlayerAction(players[action.team], action);
    }

    public void InitializePlayer(int playerNum, CreatureState[] creatures)
    {
        if (currentState.playersTeams == null || currentState.playersTeams.Length < 2) currentState.playersTeams = new TeamState[2];

        // finish initializing the creatures' states
        for (var i = 0; i < creatures.Length; i++)
        {
            creatures[i].team = playerNum;
            creatures[i].indexOnTeam = i;
            creatures[i].currentDamage = 0;
            creatures[i].isActiveCreature = i == 0;
            creatures[i].currentType = creatures[i].definition.type;
        }

        // add the creatures' states to the currentState
        currentState.playersTeams[playerNum] = new TeamState() {
            team = creatures
        };
        
        // set up the PlayerController objects
        players[playerNum] = new PlayerController()
        {
            teamNumber = playerNum,
            team = creatures.Select(creatureState => new CreatureController() { state = creatureState }).ToList()
        };

        // if all players have been initialized, we're ready to go!
        if (players.All(p => p != null)) IUI.Instance.TurnManagerReadyToRecieveInput();
    }

    //
    // Logic functions
    //

    private void QueuePlayerAction(PlayerController player, PlayerAction action)
    {
        nextTurnActions[player] = action;
        foreach(PlayerController p in players) if (nextTurnActions.ContainsKey(p)) Debug.LogWarning($"\tPlayer {p.teamNumber+1} is ready...");
        foreach(PlayerController p in players) if (!nextTurnActions.ContainsKey(p)) return; // if not all players have picked an action for next turn, end the function

        RunTurn(nextTurnActions.Values.ToList());
        //Debug.LogWarning("clearing next turn actions 1");
        //nextTurnActions.Clear(); // this line of code is haunted, I don't know what to tell you. It runs without the above RunTurn() line running. Uncomment at your own peril, we will not send extortionists to save you.
    }

    private void RunTurn(List<PlayerAction> playerActions)
    {
        // increment the turn counter
        currentState.turnNumber++;
        
        ActionLogger.LogMessage($"==== TURN {currentState.turnNumber} ====");
        ActionLogger.LogMessage($"Luck is {Mathf.FloorToInt(Mathf.Abs(currentState.luckBalance*100f))}% in Player {(currentState.luckBalance >= 0 ? 1 : 2)}'s favor.");

        // apply pendingMoves that should activate this turn
        var pendingActionIter = pendingActions.ToList();
        for(int i = 0; i < pendingActionIter.Count; i++)
        {
            PlayerAction action = pendingActionIter[i];
            if (currentState.turnNumber == action.madeOnTurn+action.moveTaken.delayTurns)
            {
                pendingActions.Remove(action);
                if (action.moveTaken != null) action.moveTaken.priority += 10;
                playerActions.Add(action);
            }
        }

        // sort actions by priority breaking ties by speed
        playerActions.Sort((PlayerAction a, PlayerAction b) => {
            
            float aSpeed = a.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[a.team]);
            float bSpeed = b.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[b.team]);
            
            if (aSpeed == bSpeed) return 0;
            if (aSpeed > bSpeed) return 1;
            if (aSpeed < bSpeed) return -1;
            return 0;
        });
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
                // TODO: await for the switch to happen??
            }
        }

        // make actions happen
        for(int i = 0; i < playerActions.Count; i++)
        {
            HandleAction(playerActions[i]);
        }

        // determine if the game is over
        var player1Lost = players[0].team.All(c => !c.CanStillFight());
        var player2Lost = players[1].team.All(c => !c.CanStillFight());
        if (player1Lost && player2Lost) IUI.Instance.GameOver(-1);
        else if (player1Lost)           IUI.Instance.GameOver(ITurnManager.PLAYER_2);
        else if (player2Lost)           IUI.Instance.GameOver(ITurnManager.PLAYER_1);

        // finalize
        CopyStateToStack();
        nextTurnActions.Clear();
        Debug.LogWarning("clearing next turn actions 2");
        IUI.Instance.TurnManagerReadyToRecieveInput();
    }

    public void HandleAction(PlayerAction action)
    {
        PlayerController player = players[action.team];
        SingleSidedFieldState playerFieldSideState = currentState.playersSideStates[action.team];

        if (!action.activeCreature.CanStillFight()) // creature fainted due to earlier action or ApplyStartOfTurnEffects. Cancel this action
        {
            ActionLogger.LogMessage($"Player {action.activeCreature.state.team+1}'s {action.activeCreature.state.definition.name} fainted and can no longer battle!");
            return;
        }

        if (action.actionType == PlayerAction.ActionType.SWITCH)
        {
            // switch

            action.activeCreature.state.isActiveCreature = false;
            action.targetCreature.state.isActiveCreature = true;
                
            CreatureController switchFrom = player.team[action.targetCreature.state.indexOnTeam];
            CreatureController switchTo   = player.team[action.activeCreature.state.indexOnTeam];
            // player.activeCreature = player.team[action.targetCreature.state.indexOnTeam]; // no longer neccessary
                
            ApplySwapEffects(switchFrom, switchTo, currentState.fieldState, playerFieldSideState);
            IUI.Instance.SwapActiveCreature(player.teamNumber, switchTo);
            ActionLogger.LogMessage($"Player {player.teamNumber + 1} swapped from {switchFrom.state.definition.name} to {switchTo.state.definition.name}");
        } 
        else
        {
            ActionLogger.LogMessage($"Player {player.teamNumber + 1}'s {action.activeCreature.state.definition.name} used {action.moveTaken.name}");

            // move
            if (action.activeCreature.state.movesDisabled) // if the pokemon flinched or something
            { 
                ActionLogger.LogMessage($"{action.activeCreature.state.definition.name} was unable to move!");
                return; 
            } 

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
            if (!moveHits) // move does not hit, skip damage calc and do not apply secondary effects
            {
                ActionLogger.LogMessage($"{action.activeCreature.state.definition.name} missed its attack!");
                return;
            }

            // damage
            int damage = CalculateDamage(action.moveTaken, action.activeCreature.state, action.targetCreature.state);
            action.targetCreature.TakeDamage(damage);
            
            string effectivenessText = "";
            float matchup = GetMatchup(action.moveTaken.type, action.targetCreature.state.definition.type);
            if (matchup > 1) effectivenessText = " It's super effective!";
            if (matchup < 1) effectivenessText = " It's not very effective!";
            ActionLogger.LogMessage($"{action.activeCreature.state.definition.name} hit its attack! It dealt {damage} damage to Player {action.targetCreature.state.team+1}'s {action.targetCreature.state.definition.name}.{effectivenessText}");

            if (!action.targetCreature.CanStillFight())
            {
                ActionLogger.LogMessage($"Player {action.targetCreature.state.team+1}'s {action.targetCreature.state.definition.name} fainted and can no longer battle!");
                // force the player to make a switch
                players[action.targetCreature.state.team].ForceSwitch();
                // TODO: await for the switch to happen??
                return; // no secondary effects if the target faints
            }

            // secondary effects
            foreach(SecondaryEffect secondaryEffect in action.moveTaken.secondaryEffects ?? new())
            {
                SecondaryEffectHandler handler = GetSecondaryEffectHandler(secondaryEffect.name);
                if (handler == null) continue;

                handler.Invoke(currentState, action.targetCreature, action.activeCreature, damage, secondaryEffect.parameters);
            }
        }

        // apply end of turn effects to all active creatures and check to see if any active creatures fainted for any reason
        foreach(PlayerController team in players)
        {
            if (team.activeCreature.CanStillFight()) team.activeCreature.ApplyEndOfTurnEffects();

            // we check CanStillFight() again in case ApplyEndOfTurnEffects() caused the creature to faint
            if (!team.activeCreature.CanStillFight())
            {
                ActionLogger.LogMessage($"Player {team.activeCreature.state.team+1}'s {team.activeCreature.state.definition.name} fainted and can no longer battle!");
                // force the player to make a switch
                players[action.activeCreature.state.team].ForceSwitch();
                // TODO: await for the switch to happen??
            }
        }
    }

    public delegate void SecondaryEffectHandler(GameState state, CreatureController targetedCreature, CreatureController attackingCreature, int damageDealt, string[] parameters);
    public SecondaryEffectHandler GetSecondaryEffectHandler(string secondaryEffectName)
    {
        switch (secondaryEffectName)
        {
            case "heal_percentage": // params: successChance, minPercent, maxPercent
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float successChance = float.Parse(parameters[0])/100f;
                    float minPercent = float.Parse(parameters[1]);
                    float maxPercent = float.Parse(parameters[2]);
                    
                    bool success = MakeBooleanRoll(successChance, attackingCreature.state.team);
                    if (!success) return;

                    float percentageRolled = MakeFloatRoll(attackingCreature.state.team, minPercent, maxPercent);
                    int healFor = Mathf.FloorToInt(attackingCreature.state.definition.hp*percentageRolled);
                    attackingCreature.TakeDamage(-healFor);

                    ActionLogger.LogMessage($"Player {attackingCreature.state.team+1}'s {attackingCreature.state.definition.name} healed {healFor} hp!");
                };
            case "heal_value": // params: successChance, minValue, maxValue
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float successChance = float.Parse(parameters[0])/100f;
                    int minValue = int.Parse(parameters[1]);
                    int maxValue = int.Parse(parameters[2]);
                    
                    bool success = MakeBooleanRoll(successChance, attackingCreature.state.team);
                    if (!success) return;

                    int valueRolled = MakeDiceRoll(attackingCreature.state.team, minValue, maxValue);
                    attackingCreature.TakeDamage(-valueRolled);
                    
                    ActionLogger.LogMessage($"Player {attackingCreature.state.team+1}'s {attackingCreature.state.definition.name} healed {valueRolled} hp!");
                };
            case "heal_damage_dealt": // params: percentageOfDamageDealtConvertedToHP
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float percentageOfDamageDealtConvertedToHP = float.Parse(parameters[0])/100f;
                    int healFor = Mathf.FloorToInt(damageDealt*percentageOfDamageDealtConvertedToHP);
                    attackingCreature.TakeDamage(-healFor);
                    
                    ActionLogger.LogMessage($"Player {attackingCreature.state.team+1}'s {attackingCreature.state.definition.name} healed {healFor} hp!");
                };
            case "self_stat_buff": // params: successChance, stat name, magnitude of buff (can be negative)
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float successChance = float.Parse(parameters[0])/100f; 
                    string stat = parameters[1];
                    int levels = int.Parse(parameters[2]);
                    
                    bool success = MakeBooleanRoll(successChance, attackingCreature.state.team);
                    if (!success) return;
                    
                    switch(stat)
                    {
                        case "ATTACK": 
                            if(attackingCreature.state.attackBuffLevel < -6 || attackingCreature.state.attackBuffLevel > 6) return; 
                            attackingCreature.state.attackBuffLevel += levels; 
                            break;
                        case "DEFENSE": 
                            if(attackingCreature.state.defenseBuffLevel < -6 || attackingCreature.state.defenseBuffLevel > 6) return; 
                            attackingCreature.state.defenseBuffLevel += levels; 
                            break;
                        case "SPEED": 
                            if(attackingCreature.state.speedBuffLevel < -6 || attackingCreature.state.speedBuffLevel > 6) return;
                            attackingCreature.state.speedBuffLevel += levels; 
                            break;
                    }
                    IUI.Instance.PlayStatBuffEffect(attackingCreature, stat, levels);
                    
                    ActionLogger.LogMessage($"Player {attackingCreature.state.team+1}'s {attackingCreature.state.definition.name}'s {stat} {(levels >= 0 ? "rose" : "fell")} by {levels}!");
                };
            case "opponent_stat_debuff": // params: chance, statname, magnitude of debuff (can be negative)
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float successChance = float.Parse(parameters[0])/100f; 
                    string stat = parameters[1];
                    int levels = -int.Parse(parameters[2]);
                    
                    bool success = MakeBooleanRoll(successChance, attackingCreature.state.team);
                    if (!success) return;
                    
                    switch(stat)
                    {
                        case "ATTACK": 
                            if(targetedCreature.state.attackBuffLevel < -6 || targetedCreature.state.attackBuffLevel > 6) return; 
                            targetedCreature.state.attackBuffLevel += levels; 
                            break;
                        case "DEFENSE": 
                            if(targetedCreature.state.defenseBuffLevel < -6 || targetedCreature.state.defenseBuffLevel > 6) return; 
                            targetedCreature.state.defenseBuffLevel += levels; 
                            break;
                        case "SPEED": 
                            if(targetedCreature.state.speedBuffLevel < -6 || targetedCreature.state.speedBuffLevel > 6) return;
                            targetedCreature.state.speedBuffLevel += levels; 
                            break;
                    }
                    IUI.Instance.PlayStatBuffEffect(targetedCreature, stat, levels);

                    ActionLogger.LogMessage($"Player {targetedCreature.state.team+1}'s {targetedCreature.state.definition.name}'s {stat} {(levels >= 0 ? "rose" : "fell")} by {levels}!");
                };
            case "opponent_status_condition": // params: chance, condition
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    float successChance = float.Parse(parameters[0])/100f; 
                    string condition = parameters[1];
                    
                    bool success = MakeBooleanRoll(successChance, attackingCreature.state.team);
                    if (!success) return;
                    
                    switch(condition)
                    {
                        case "SLEEP": targetedCreature.ApplyStatusCondition(StatusContidion.SLEEP); break;
                        case "BURN": targetedCreature.ApplyStatusCondition(StatusContidion.BURN); break;
                        case "POISON": targetedCreature.ApplyStatusCondition(StatusContidion.POISONED); break;
                        case "PARALYSIS": targetedCreature.ApplyStatusCondition(StatusContidion.PARALYZED); break;
                    }

                    // no log since the log happens inside ApplyStatusCondition
                };
            default: return null;
        }
        //return null;
    }

    //
    // HELPER LOGIC
    //

    // note: DOES NOT change the luck balance
    public int CalculateDamage(Move move, CreatureState attacker, CreatureState target)
    {
        float ADRatio = CreatureController.GetAttackStat(attacker) / CreatureController.GetDefenseStat(target);
        float STAB = move.type == (Move.Type)attacker.currentType ? 1.5f : 1; // STAB = same type attack bonus
        float effectiveness = GetMatchup(move.type, target.currentType);
        return Mathf.FloorToInt(move.basePower * ADRatio * STAB * effectiveness);
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
    private static int GetPriority(PlayerAction action, GameState state)
    {
        // state is here as a parameter in case we want to allow the state to affect priority later
        return action.actionType == PlayerAction.ActionType.SWITCH ? 9999 : action.moveTaken.priority;
    }

    private static void ApplySwapEffects(CreatureController from, CreatureController to, FieldState globalFieldState, SingleSidedFieldState playerSideFieldState)
    {
        // stub
    }


    //
    // Utility
    //

    // this function implements the whole luck mechanic
    // NOTE: this is the only place that depends on there being only two players. To implement multiple players, we just have to give each player their own luckBalance stat (and add a "int[] teamsNegativelyAffectedByPositiveOutcome" parameter)
    // team is the team who benefits from a positive outcome
    public bool MakeBooleanRoll(float positiveOutcomeChance, int team)
    {
        bool success = false;
        float relative = team == 0 ? 1 : -1;
        float negativeOutcomeChance = (1-positiveOutcomeChance);

        // first, make a roll against the luck. if this succeeds, then the roll as a whole is considered successful
        if (Random.value < relative*currentState.luckBalance) success = true;

        // if the luck roll failed, roll against positiveOutcomeChance, if this succeeds, then the roll as a whole is considered successful
        if (Random.value < positiveOutcomeChance) success = true;

        // if the player decided to spend their luck, force the roll to be successful and double the luck punishment
        if (currentState.playersTeams[team].luckSpendingEnabled && relative*currentState.luckBalance > 0) 
        {
            success = true; 
            negativeOutcomeChance *= 2;
        }

        // now influence the luck - if the roll succeeded, sway the luck in favor of the opposing team by (1-positiveOutcomeChance)
        // if the roll failed, sway the luck in favor of this team by positiveOutcomeChance
        if (success) currentState.luckBalance -= relative*negativeOutcomeChance; // sway the luck in favor of the opposing team by the chance of thier desired outcome (which did not happen)
        else         currentState.luckBalance += relative*positiveOutcomeChance; // sway the luck in favor of this team by the chance of thier desired outcome (which did not happen)

        return success;
    }

    // team should be the team that a high roll would be good for
    public int MakeDiceRoll(int team, int min, int max)
    {
        //float relative = team == 0 ? 1 : -1;
        //int advantage = 0;

        //if (relative*currentState.luckBalance >= 0.5f) advantage++;
        //if (relative*currentState.luckBalance >= 1.0f) advantage++;

        //if (relative*currentState.luckBalance <= -0.5f) advantage--;
        //if (relative*currentState.luckBalance <= -1.0f) advantage--;

        //int roll = Random.Range(min, max+1);
        //for (int i = 0; i < Mathf.Abs(advantage); i++)
        //{
        //    int newRoll = Random.Range(min, max+1);
        //    if (advantage > 0) roll = Mathf.Max(roll, newRoll);
        //    if (advantage < 0) roll = Mathf.Min(roll, newRoll);
        //}

        //// luck balance update formula: ((singleDiceAverage-rolledValue)/diceSideCount)
        //// this means if you roll a 6 on a six sided dice, your luck balance decreases by around 50%
        //// if you roll a 3.5, your luck balance does not change
        //float singleDiceAverage = min + ((float)max-(float)min)/2f;
        //float numDiceSides = 1+max-min;
        //currentState.luckBalance += relative*((singleDiceAverage-roll)/numDiceSides);

        return Mathf.FloorToInt(MakeFloatRoll(team, min, max));
    }

    public float MakeFloatRoll(int team, float min, float max)
    {
        float relative = team == 0 ? 1 : -1;
        int advantage = 0;

        if (relative*currentState.luckBalance >= 0.5f) advantage++;
        if (relative*currentState.luckBalance >= 1.0f) advantage++;

        if (relative*currentState.luckBalance <= -0.5f) advantage--;
        if (relative*currentState.luckBalance <= -1.0f) advantage--;

        float range = max-min;
        float roll = Random.value*range + min;
        for (int i = 0; i < Mathf.Abs(advantage); i++)
        {
            float newRoll = Random.value*range + min;
            if (advantage > 0) roll = Mathf.Max(roll, newRoll);
            if (advantage < 0) roll = Mathf.Min(roll, newRoll);
        }

        // if the player decided to spend their luck, force the roll to be as high as possible and double the luck punishment
        if (currentState.playersTeams[team].luckSpendingEnabled && relative*currentState.luckBalance > 0) 
        {
            roll = max; 
            relative *= 2;
        }

        // luck balance update formula: ((singleDiceAverage-rolledValue)/diceSideCount)
        // this means if you roll a 6 on a six sided dice, your luck balance decreases by around 50%
        // if you roll a 3.5, your luck balance does not change
        float singleDiceAverage = min + (max-min)/2f;
        float numDiceSides = max-min;
        currentState.luckBalance += relative*((singleDiceAverage-roll)/numDiceSides);

        return roll;
    }

    private void CopyStateToStack()
    {
        // TODO: disabled this function. we don't need it for any moves currently planned and deep copying stuff is just such a pain
        //previousStates.Add(DeepCopy(currentState));
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
