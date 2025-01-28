using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace EvilMimicSpawner
{
    [ApiVersion(2, 1)]
    public class EvilMimicSpawner : TerrariaPlugin
    {
        public override string Name => "EvilMimicSpawner";
        public override string Author => "TRANQUILZOIIP - github.com/bbeeeeenn";
        public override string Description => base.Description;
        public override Version Version => base.Version;
        private static readonly Random random = new();

        readonly Dictionary<string, DateTime> LastSummon = new();

        public EvilMimicSpawner(Main game)
            : base(game) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerUpdate)
                OnPlayerUpdate(args);
        }

        private void OnPlayerUpdate(GetDataEventArgs args)
        {
            using BinaryReader reader = new(
                new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)
            );
            var playerId = reader.ReadByte();
            BitsByte control = reader.ReadByte();
            _ = reader.ReadByte();
            _ = reader.ReadByte();
            _ = reader.ReadByte();
            var selectedSlot = reader.ReadByte();

            bool useItem = control[5];
            if (!useItem)
                return;

            TSPlayer player = TShock.Players[playerId];
            Item selectedItem = player.TPlayer.inventory[selectedSlot];

            if (
                useItem
                && new List<int>()
                {
                    Terraria.ID.ItemID.NightKey,
                    Terraria.ID.ItemID.LightKey,
                }.Contains(selectedItem.netID)
                && (
                    !LastSummon.ContainsKey(player.Name)
                    || (DateTime.Now - LastSummon[player.Name]).Seconds >= 2
                )
            )
            {
                LastSummon[player.Name] = DateTime.Now;
                selectedItem.stack--;
                NetMessage.SendData(
                    (int)PacketTypes.PlayerSlot,
                    -1,
                    -1,
                    null,
                    playerId,
                    selectedSlot,
                    selectedItem.stack,
                    selectedItem.prefix,
                    selectedItem.netID
                );
                SpawnMimic(player, selectedItem.netID == Terraria.ID.ItemID.NightKey);
            }
        }

        private static void SpawnMimic(TSPlayer player, bool nightKey)
        {
            Vector2 playerPosition = player.TPlayer.position;
            int offset = random.Next(-200, 200);
            Vector2 position = new(playerPosition.X + offset, playerPosition.Y);
            List<int> evilMimics = new()
            {
                Terraria.ID.NPCID.BigMimicCorruption,
                Terraria.ID.NPCID.BigMimicCrimson,
            };
            int type = nightKey
                ? evilMimics[random.Next(evilMimics.Count)]
                : Terraria.ID.NPCID.BigMimicHallow;
            int index = NPC.NewNPC(null, (int)position.X, (int)position.Y, type);
            if (index != 200)
            {
                player.SendSuccessMessage(
                    $"You summoned {TShock.Utils.GetNPCById(type).FullName}!"
                );
            }
            else
            {
                player.SendErrorMessage("Can't summon a mimic.");
            }
        }
    }
}
