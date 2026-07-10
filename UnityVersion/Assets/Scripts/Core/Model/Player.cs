using System;
using System.Collections.Generic;

namespace SichuanMahjong.Core.Model
{
    public class Player
    {
        protected HashSet<PlayerStatusEnum> status;
        protected readonly string name;
        protected readonly int position;
        protected HandTiles hand;
        protected readonly Tiles table;

        public Player(string name, List<Tile> hand, int position)
        {
            this.name = name;
            this.position = position;
            this.hand = new HandTiles(hand);
            table = new Tiles();
            status = new HashSet<PlayerStatusEnum> { PlayerStatusEnum.WAITING };
        }

        public void Plays(Tile tile)
        {
            Tile playedTile = hand.RemoveForDiscard(tile);
            if (playedTile == null)
            {
                playedTile = PlayFallbackTile();
            }
            if (playedTile == null)
            {
                throw new ArgumentException(name + " cannot play unavailable tile: " + tile);
            }
            table.Add(playedTile);
        }

        private Tile PlayFallbackTile()
        {
            Tile newTile = hand.GetNewTile();
            if (newTile != null)
            {
                return hand.RemoveForDiscard(newTile);
            }
            List<Tile> tiles = hand.ToList();
            if (tiles.Count > 0)
            {
                return hand.RemoveForDiscard(tiles[0]);
            }
            return null;
        }

        public string GetName()
        {
            return name;
        }

        public HandTiles GetHand()
        {
            return hand;
        }

        public Tiles GetTable()
        {
            return table;
        }

        public void SetHand(HandTiles tiles)
        {
            hand = tiles;
        }

        public HashSet<PlayerStatusEnum> GetStatus()
        {
            return status;
        }

        public bool IsPlaying()
        {
            return status.Contains(PlayerStatusEnum.PLAYING);
        }

        public bool IsWaiting()
        {
            return status.Contains(PlayerStatusEnum.WAITING);
        }

        public bool ContainsChow()
        {
            return status.Contains(PlayerStatusEnum.CHOW);
        }

        public bool ContainsPung()
        {
            return status.Contains(PlayerStatusEnum.PUNG);
        }

        public bool ContainsKong()
        {
            return status.Contains(PlayerStatusEnum.NORMAL_KONG)
                   || status.Contains(PlayerStatusEnum.HIDDEN_KONG)
                   || status.Contains(PlayerStatusEnum.ADD_KONG);
        }

        public bool ContainsHu()
        {
            return status.Contains(PlayerStatusEnum.HU);
        }

        public bool ContainsResponseAction()
        {
            return ContainsHu() || ContainsChouPungKong();
        }

        public bool ContainsChouPungKong()
        {
            return status.Contains(PlayerStatusEnum.CHOW)
                   || status.Contains(PlayerStatusEnum.PUNG)
                   || ContainsKong();
        }

        // Status setters
        public void SetPlayingStatus()
        {
            if (status.Contains(PlayerStatusEnum.WAITING))
            {
                status.Remove(PlayerStatusEnum.WAITING);
                status.Add(PlayerStatusEnum.PLAYING);
            }
        }

        public void SetWaitingStatus()
        {
            if (status.Contains(PlayerStatusEnum.PLAYING))
            {
                status.Remove(PlayerStatusEnum.PLAYING);
                status.Add(PlayerStatusEnum.WAITING);
            }
        }

        public void SetHuStatus()
        {
            status.Add(PlayerStatusEnum.HU);
        }

        public void SetNormalKongStatus()
        {
            status.Add(PlayerStatusEnum.NORMAL_KONG);
        }

        public void SetAddKongStatus()
        {
            status.Add(PlayerStatusEnum.ADD_KONG);
        }

        public void SetHiddenKongStatus()
        {
            status.Add(PlayerStatusEnum.HIDDEN_KONG);
        }

        public void SetPungStatus()
        {
            status.Add(PlayerStatusEnum.PUNG);
        }

        public void SetChowStatus()
        {
            status.Add(PlayerStatusEnum.CHOW);
        }

        public void ClearKongStatus()
        {
            status.Remove(PlayerStatusEnum.NORMAL_KONG);
            status.Remove(PlayerStatusEnum.ADD_KONG);
            status.Remove(PlayerStatusEnum.HIDDEN_KONG);
        }

        public void ClearPungStatus()
        {
            status.Remove(PlayerStatusEnum.PUNG);
        }

        public void ClearChowStatus()
        {
            status.Remove(PlayerStatusEnum.CHOW);
        }

        public void ClearStatus()
        {
            status.Remove(PlayerStatusEnum.HU);
            ClearChowStatus();
            ClearKongStatus();
            ClearPungStatus();
        }

        public void AddTile(Tile tile)
        {
            hand.Add(tile);
        }

        public virtual void PlayAction()
        {
        }

        public virtual PlayerActionEnum? OtherAction(Tile tile)
        {
            return null;
        }

        public int GetPosition()
        {
            return position;
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o == null || GetType() != o.GetType()) return false;
            Player player = (Player)o;
            return Equals(name, player.name);
        }

        public override int GetHashCode()
        {
            return name == null ? 0 : name.GetHashCode();
        }

        public override string ToString()
        {
            return "Player{status=[" + string.Join(", ", status) + "], name='" + name + "', hand=" + hand + "}";
        }
    }
}
