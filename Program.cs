using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        // Create and start the server listener
        var listener = new ServerListener<MyPlayer, CustomServer>();
        listener.Start(29294);

        System.Threading.Thread.Sleep(-1);
    }
}

class MyPlayer : Player<MyPlayer>
{
    public int NumberOfKills;
    public int Health = 100;
    public float Damage = 1f;
    public float MoveSpeed = 1f;
}

class CustomServer : GameServer<MyPlayer>
{
    public MyPlayer JuggernautPlayer = null;

    public override async Task OnTick()
    {
        if (this.RoundSettings.State == GameState.WaitingForPlayers)
            ForceStartGame();

        await Task.Delay(1000);
    }

    public async Task OnRoundStarted()
    {
        // Randomly select a player to be the juggernaut
        var playersList = AllPlayers.ToList();
        var random = new Random();
        int randomIndex = random.Next(playersList.Count);
        JuggernautPlayer = playersList[randomIndex];

        // Apply attributes to the juggernaut player
        SetJuggernautAttributes(JuggernautPlayer);

        // Inform players about the juggernaut and start of the round
        foreach (var player in AllPlayers)
        {
            if (player.Team.Equals(JuggernautPlayer.Team))
            {
                player.Message($"You are the Juggernaut!");
            }
            else
            {
                player.Message($"The Juggernaut is {JuggernautPlayer.Name}");
            }

            player.Message("Round has started! Get ready to fight!");
        }
    }

    // Method to broadcast a message to all players
    public void BroadcastMessage(string message)
    {
        foreach (var player in AllPlayers)
        {
            player.Message(message);
        }
    }

    // Method to set attributes for the juggernaut player
    public void SetJuggernautAttributes(MyPlayer player)
    {
        // Have team A be the Juggernaut team
        player.Team = Team.TeamA;
        player.SetHP(player.Health);
        player.SetFallDamageMultiplier(0.1f);
        player.SetPrimaryWeapon(new WeaponItem() { Tool = Weapons.P90, MainSight = Attachments.FYouSight }, 20, false);
        player.SetRunningSpeedMultiplier(player.MoveSpeed);
    }

    public async Task OnAPlayerKilledAnotherPlayer(MyPlayer killer, MyPlayer victim, OnPlayerKillArguments<MyPlayer> args)
    {
        if (killer.Team.Equals(JuggernautPlayer.Team))
        {
            if (victim.Team.Equals(JuggernautPlayer.Team))
            {
                // Assign to a random player
                var playersList = AllPlayers.ToList();
                var random = new Random();
                int randomIndex = random.Next(playersList.Count);
                JuggernautPlayer = playersList[randomIndex];
                SetJuggernautAttributes(JuggernautPlayer);
                BroadcastMessage($"{killer.Name} killed {victim.Name}! {JuggernautPlayer.Name} is the new Juggernaut!");

                // Move the juggernaut to team B
                victim.Team = Team.TeamB;
            }
            else
            {
                killer.NumberOfKills++;
                killer.SetHP(killer.Health + 10);
                killer.SetGiveDamageMultiplier(killer.Damage + 0.1f);
                killer.SetRunningSpeedMultiplier(killer.MoveSpeed + 0.1f);
            }

            // Announce kill streaks
            if (killer.NumberOfKills % 3 == 0)
            {
                BroadcastMessage($"{killer.Name} is on a killing spree!");
            }
        }
        else if (!killer.Team.Equals(JuggernautPlayer.Team) && victim.Team.Equals(JuggernautPlayer.Team))
        {
            // Set the killed player back to team B
            victim.Team = Team.TeamB;
            JuggernautPlayer = killer;
            SetJuggernautAttributes(JuggernautPlayer);

            // Announce juggernaut transfer
            BroadcastMessage($"{killer.Name} has become the new Juggernaut!");
        }
    }

    // Method called when a player disconnects
    public async Task OnPlayerDisconnected(MyPlayer player)
    {
        if (JuggernautPlayer == player)
        {
            // Assign the Juggernaut status to another player
            var playersList = AllPlayers.ToList();
            playersList.Remove(player); // Remove the disconnected player
            if (playersList.Count > 0)
            {
                var random = new Random();
                int randomIndex = random.Next(playersList.Count);
                JuggernautPlayer = playersList[randomIndex];
                SetJuggernautAttributes(JuggernautPlayer);
                BroadcastMessage($"{player.Name} has disconnected. {JuggernautPlayer.Name} is the new Juggernaut!");
            }
        }
    }
}
