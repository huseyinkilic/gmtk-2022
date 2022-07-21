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

        internal float speed; // just to make an error go away
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


    public GameObject builderMenu;
    public GameObject battleMenu;
    public void HANDLE_START()
    {
        if (TeamBuilderButton.includedCreatures.Count < 3) return;

        var creatures = Database.Instance.GetAllCreatures();
        CreatureState[] team = new CreatureState[3];
        for (int j = 0; j < team.Length; j++)
        {
            int c = Random.Range(0, creatures.Count-1);
            team[j] = new CreatureState()
            {
                definition = creatures[j], // just use the last 3 creatures listed in the database (test creatures)
                knownMoves = new Move[]
                {
                    Database.Instance.MoveFromID(creatures[j].allowedMoves[0]), // just use the first 4 allowable moves
                    Database.Instance.MoveFromID(creatures[j].allowedMoves[1]),
                    Database.Instance.MoveFromID(creatures[j].allowedMoves[2]),
                    Database.Instance.MoveFromID(creatures[j].allowedMoves[3])
                }
            };
        }

        TurnManager.Instance.InitializePlayer(1, team);

    
        CreatureState[] team1 = new CreatureState[3];
        Creature[] creatures1 = TeamBuilderButton.includedCreatures.ToArray();
        for (int j = 0; j < team1.Length; j++)
        {
            team1[j] = new CreatureState()
            {
                definition = creatures1[j], // just use the last 3 creatures listed in the database (test creatures)
                knownMoves = new Move[]
                {
                    Database.Instance.MoveFromID(creatures1[j].allowedMoves[0]), // just use the first 4 allowable moves
                    Database.Instance.MoveFromID(creatures1[j].allowedMoves[1]),
                    Database.Instance.MoveFromID(creatures1[j].allowedMoves[2]),
                    Database.Instance.MoveFromID(creatures1[j].allowedMoves[3])
                }
            };
        }

        builderMenu.SetActive(false);
        battleMenu.SetActive(true);
    
        BattleUI.Instance.player2AI = new SimpleAI();
        TurnManager.Instance.InitializePlayer(0, team1);
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
        return action;
    }

    public PlayerAction MakeMoveAction(int playerNum, int moveIndex, CreatureController targetCreature)
    {
        PlayerController player = players[playerNum];
        PlayerAction action = player.MakeMoveAction(moveIndex, targetCreature);
        return action;
    }

    public void SubmitAction(PlayerAction action)
    {
        if (players[action.team].pendingForcedSwitch && action.actionType == PlayerAction.ActionType.SWITCH)
        {
            // if this is a response to a forced switch, handle the action immediately
            HandleAction(action);
            return;
        }

        action.speed = action.activeCreature.GetSpeed(currentState.fieldState, /*currentState.playersSideStates[action.team]*/ null);
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
        if (players.All(p => p != null)) 
        {
            IUI.Instance.TurnManagerReadyToRecieveInput();
            // "swap in" the players' starting creatures
            BattleUI.Instance.SwapActiveCreature(0, players[0].team[0]);
            BattleUI.Instance.SwapActiveCreature(1, players[1].team[0]);
        }
    }

    //
    // Logic functions
    //

    private void QueuePlayerAction(PlayerController player, PlayerAction action)
    {
        // handle forced actions
        if (player.pendingForcedSwitch && action.actionType == PlayerAction.ActionType.SWITCH)
        {
            ActionLogger.LogMessage($"Player {player.teamNumber + 1} is responding to a forced switch.");
            HandleAction(action);
            player.pendingForcedSwitch = false;
            return;
        }

        // not a forced action, this action will take place at the start of the next turn

        nextTurnActions[player] = action;
        foreach(PlayerController p in players) if (nextTurnActions.ContainsKey(p)) Debug.LogWarning($"\tPlayer {p.teamNumber+1} is ready...");
        foreach(PlayerController p in players) if (!nextTurnActions.ContainsKey(p)) return; // if not all players have picked an action for next turn, end the function

        BattleUI.Instance.SetCurrentTurn(RunTurn());
    }

    public IEnumerator RunTurn()
    {
        var playerActions = nextTurnActions.Values.ToList();

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
            float aSpeed = a.speed;//a.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[a.team]);
            float bSpeed = b.speed;//b.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[b.team]);
            
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
            yield return false;
        }

        // determine if the game is over
        var player1Lost = players[0].team.All(c => !c.CanStillFight());
        var player2Lost = players[1].team.All(c => !c.CanStillFight());
        if (player1Lost && player2Lost) IUI.Instance.GameOver(-1);
        else if (player1Lost)           IUI.Instance.GameOver(ITurnManager.PLAYER_2);
        else if (player2Lost)           IUI.Instance.GameOver(ITurnManager.PLAYER_1);

        // finalize
        ActionLogger.LogMessage($"Luck is now {Mathf.FloorToInt(Mathf.Abs(currentState.luckBalance*100f))}% in Player {(currentState.luckBalance >= 0 ? 1 : 2)}'s favor.");

        CopyStateToStack();
        nextTurnActions.Clear();
        Debug.LogWarning("clearing next turn actions 2");
        IUI.Instance.TurnManagerReadyToRecieveInput();

        yield return true;
    }

    public void HandleAction(PlayerAction action)
    {
        PlayerController player = players[action.team];
        SingleSidedFieldState playerFieldSideState = null; //currentState.playersSideStates[action.team];

        bool playerIsRespondingToAForcedSwitch = player.pendingForcedSwitch && action.actionType == PlayerAction.ActionType.SWITCH;
        if (!action.activeCreature.CanStillFight() && !playerIsRespondingToAForcedSwitch) // creature fainted due to earlier action or ApplyStartOfTurnEffects. Cancel this action
        {
            ActionLogger.LogMessage($"1 Player {action.activeCreature.state.team+1}'s {action.activeCreature.state.definition.name} fainted and can no longer battle!");
            players[action.activeCreature.state.team].ForceSwitch();
            return;
        }

        if (action.actionType == PlayerAction.ActionType.SWITCH)
        {
            // switch

            action.activeCreature.state.isActiveCreature = false;
            action.targetCreature.state.isActiveCreature = true;
                
            CreatureController switchFrom = player.team[action.activeCreature.state.indexOnTeam];
            CreatureController switchTo   = player.team[action.targetCreature.state.indexOnTeam];
            // player.activeCreature = player.team[action.targetCreature.state.indexOnTeam]; // no longer neccessary
            switchFrom.state.isActiveCreature = false;
            switchTo.state.isActiveCreature = true;

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
            bool crit = action.moveTaken.basePower > 0 ? MakeBooleanRoll(0.1f, action.team, 0.1f) : false;
            int damage = CalculateDamage(action.moveTaken, action.activeCreature.state, action.targetCreature.state, crit);
            action.targetCreature.TakeDamage(damage);
            
            string effectivenessText = "";
            float matchup = GetMatchup(action.moveTaken.type, action.targetCreature.state.definition.type);
            if (matchup > 1) effectivenessText = "It's super effective!";
            if (matchup < 1) effectivenessText = "It's not very effective!";
            if (damage > 0 && effectivenessText != "") ActionLogger.LogMessage(effectivenessText);
            // if (damage > 0)   ActionLogger.LogMessage($"{action.activeCreature.state.definition.name} hit its attack! It dealt {damage} damage to Player {action.targetCreature.state.team+1}'s {action.targetCreature.state.definition.name}.{effectivenessText}");
            if (crit)         ActionLogger.LogMessage($"It's a critical hit!");

            if (!action.targetCreature.CanStillFight())
            {
                ActionLogger.LogMessage($"2 Player {action.targetCreature.state.team+1}'s {action.targetCreature.state.definition.name} fainted and can no longer battle!");
                // force the player to make a switch
                players[action.targetCreature.state.team].ForceSwitch();
                // TODO: await for the switch to happen??
                return; // no secondary effects if the target faints
            }

            // secondary effects
            foreach(SecondaryEffect secondaryEffect in action.moveTaken.secondaryEffects ?? new())
            {
                SecondaryEffectHandler handler = GetSecondaryEffectHandler(secondaryEffect.name);
                if (handler == null) { Debug.LogError($"No secondary effect handler found for {secondaryEffect.name}.");  continue; }

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
                ActionLogger.LogMessage($"3 Player {team.activeCreature.state.team+1}'s {team.activeCreature.state.definition.name} fainted and can no longer battle!");
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
            case "change_type": // params: type
                return (state, targetedCreature, attackingCreature, damageDealt, parameters) =>
                {
                    string type = parameters[0];
                    
                    string typeName = "";
                    Creature.Type t;
                    switch(type)
                    {
                        case "ATTACK": t=Creature.Type.ATTACK; typeName = "Attacking"; break;
                        case "DEFENSE": t=Creature.Type.DEFEND; typeName = "Defensive"; break;
                        case "NEUTRAL": t=Creature.Type.NEUTRAL; typeName = "Neutral"; break;
                        default: return;
                    }
                    attackingCreature.state.currentType = t;
                    ActionLogger.LogMessage($"Player {targetedCreature.state.team+1}'s {targetedCreature.state.definition.name} changed to the {typeName} type!");
                };
            default: return null;
        }
        //return null;
    }

    //
    // HELPER LOGIC
    //

    // note: DOES NOT change the luck balance
    public int CalculateDamage(Move move, CreatureState attacker, CreatureState target, bool crit = false)
    {
        float ADRatio = CreatureController.GetAttackStat(attacker) / CreatureController.GetDefenseStat(target);
        float STAB = move.type == (Move.Type)attacker.currentType ? 1.5f : 1; // STAB = same type attack bonus
        float effectiveness = GetMatchup(move.type, target.currentType);
        float critBonus = crit? 2f : 1f;
        return Mathf.FloorToInt(move.basePower * ADRatio * STAB * effectiveness * critBonus);
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
    public bool MakeBooleanRoll(float positiveOutcomeChance, int team, float luckFactor=1f)
    {
        bool success = false;
        float relative = team == 0 ? 1 : -1;
        float negativeOutcomeChance = (1-positiveOutcomeChance);

        // first, make a roll against the luck. if this succeeds, then the roll as a whole is considered successful
        if (Random.value < relative*currentState.luckBalance*luckFactor) success = true;
        if (success) ActionLogger.LogMessage($"LUCKY ROLL! Player {team+1}'s roll was an automatic success!");

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
        if (success) currentState.luckBalance -= relative*negativeOutcomeChance*luckFactor; // sway the luck in favor of the opposing team by the chance of thier desired outcome (which did not happen)
        else         currentState.luckBalance += relative*positiveOutcomeChance*luckFactor; // sway the luck in favor of this team by the chance of thier desired outcome (which did not happen)

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
