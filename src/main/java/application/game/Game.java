package application.game;

import application.game.validation.PlayerStatusChecker;
import model.GameState;
import model.basic.Tile;
import model.basic.TileTypeEnum;
import model.log.Log;
import model.players.*;
import model.tiles.HandTiles;

import java.awt.Toolkit;
import java.util.*;

public class Game {
    private boolean ended;
    private final List<Tile> tiles;
    private final List<Player> players;
    private final Player player;
    private Player turnPlayer;
    private Player lastActionPlayer;
    private Player lastDiscardPlayer;
    private Player winner;
    private Tile lastPlayedTile;
    private Tile winningTile;
    private String lastActionText;
    private String statusText;
    private int actionVersion;
    private GameTurn gameTurn;
    private final Log log;

    public Game() {
        this.ended = false;
        this.lastActionText = "";
        this.statusText = "Game starting...";
        this.actionVersion = 0;
        this.log = new Log();
        this.tiles = new ArrayList<>();
        players = new ArrayList<Player>() {{
            add(new Player("player", new ArrayList<>(), 0));
            add(new AI1("ai1", new ArrayList<>(), 1));
            add(new AI2("ai2", new ArrayList<>(), 2));
            add(new AI3("ai3", new ArrayList<>(), 3));
        }};
        this.player = players.get(0);
        TileTypeEnum[] categories = {TileTypeEnum.B,
                TileTypeEnum.C, TileTypeEnum.D};
        for (TileTypeEnum category : categories) {
            for (int i = 1; i <= 9; i++) {
                for (int j = 0; j < 4; j++) {
                    this.tiles.add(new Tile(category, i));
                }
            }
        }
        Collections.shuffle(tiles);
        log.addMessage("Tiles created and shuffled");
        this.deal();
        this.next();
        log.addMessage("AIs played their first turn");
    }

    private void deal() {
        for (Player player : players) {
            List<Tile> hand = new ArrayList<>();
            for (int i = 0; i < 13; i++) {
                Tile nextTile = this.getNextTile();
                hand.add(nextTile);
            }
            player.setHand(new HandTiles(hand));
        }
        int random = new Random().nextInt(this.players.size());
        Player player = players.get(random);
        player.addTile(this.getNextTile());
        player.setPlayingStatus();
        this.lastActionPlayer = player;
        this.lastActionText = player.getName() + " starts";
        this.statusText = player.getName() + " starts the game.";

        log.addMessage("Tiles dealt");
        log.addMessage(player.getName() + " starts the game");
        this.gameTurn = new GameTurn(players, player);
    }

    private void unfairDeal() {
        this.players.get(1).setHand(new HandTiles(new ArrayList<Tile>() {{
            add(new Tile(TileTypeEnum.C, 1));
            add(new Tile(TileTypeEnum.C, 1));
            add(new Tile(TileTypeEnum.C, 2));
            add(new Tile(TileTypeEnum.C, 2));
            add(new Tile(TileTypeEnum.B, 3));
            add(new Tile(TileTypeEnum.B, 3));
            add(new Tile(TileTypeEnum.C, 4));
            add(new Tile(TileTypeEnum.C, 5));
            add(new Tile(TileTypeEnum.B, 6));
            add(new Tile(TileTypeEnum.B, 6));
            add(new Tile(TileTypeEnum.C, 7));

        }}));
        for (Player player : players) {
            if (player.getName().equals(this.players.get(1).getName())) {
                continue;
            }
            List<Tile> hand = new ArrayList<>();
            for (int i = 0; i < 13; i++) {
                Tile nextTile = this.getNextTile();
                hand.add(nextTile);
            }
            player.setHand(new HandTiles(hand));
        }
        Player player = players.get(0);
        player.addTile(new Tile(TileTypeEnum.B, 2));
        player.setPlayingStatus();
        this.lastActionPlayer = player;
        this.lastActionText = player.getName() + " starts";
        this.statusText = player.getName() + " starts the game.";

        log.addMessage("Tiles dealt");
        log.addMessage(player.getName() + " starts the game");
        this.gameTurn = new GameTurn(players, player);
    }

    private Tile getNextTile() {
        if (tiles.size() == 0) {
            this.endDrawGame();
            return null;
        }
        return this.tiles.remove(0);
    }

    private void next() {
        if (this.ended) {
            return;
        }
        this.turnPlayer = gameTurn.next();
        if (turnPlayer.getStatus().contains(PlayerStatusEnum.HU)) {
            this.ended = true;
            return;
        }
        log.addMessage(turnPlayer.getName() + "'s turn");
        Tile drawnTile = null;
        if (gameTurn.getRound() != 1) {
            drawnTile = this.getNextTile();
            if (drawnTile == null) {
                return;
            }
            turnPlayer.addTile(drawnTile);
        }
        this.turnPlayer.setPlayingStatus();
        new PlayerStatusChecker(turnPlayer, turnPlayer.getHand().getNewTile());
        if (turnPlayer == this.player) {
            this.statusText = this.buildPlayerTurnPrompt(drawnTile);
        } else {
            this.statusText = turnPlayer.getName() + " is taking a turn.";
        }
        this.action();
    }

    private void action() {
        if (this.ended) {
            return;
        }
        if (turnPlayer == this.player) {
            log.addMessage("directing to " + turnPlayer.getName() + " for action");
        } else {
            try {
                turnPlayer.playAction();
                this.processPlayed();
            } catch (RuntimeException e) {
                this.endErroredGame(turnPlayer, e);
            }
        }
    }

    public void processPlayed() {
        List<Player> next3Players = this.gameTurn.peek3();
        Tile lastPlayedTile = this.turnPlayer.getTable().getLast();
        this.lastDiscardPlayer = this.turnPlayer;
        this.lastPlayedTile = lastPlayedTile;
        for (Player player : next3Players) {
            new PlayerStatusChecker(player, lastPlayedTile);
        }
        this.recordAction(this.turnPlayer, "played " + lastPlayedTile);
        log.addMessage(this.turnPlayer.getName() + " played " + this.turnPlayer.getTable().getLast());
        this.turnPlayer.setWaitingStatus();

        for (Player p : next3Players) {
            if (p.containsHu() && p == this.player) {
                this.statusText = this.buildResponsePrompt(this.turnPlayer, lastPlayedTile);
                log.addMessage("For " + lastPlayedTile + ", press h to hu, or s to skip");
                return;
            } else if (p.containsHu()) {
                this.processHu(p);
                return;
            } else if (p.containsChouPungKong() && p == this.player) {
                this.statusText = this.buildResponsePrompt(this.turnPlayer, lastPlayedTile);
                log.addMessage("For " + lastPlayedTile + ", press c to chow, p to pung, k to kong, or s to skip");
                return;
            } else if (p.containsChouPungKong()) {
                PlayerActionEnum action = p.otherAction(lastPlayedTile);
                if (action == PlayerActionEnum.CHOW) {
                    this.processChou(p);
                    return;
                } else if (action == PlayerActionEnum.PUNG) {
                    this.processPung(p);
                    return;
                } else if (action == PlayerActionEnum.KONG) {
                    this.processKong(p);
                    return;
                } else {
                    p.clearStatus();
                    this.recordAction(p, "skip");
                    log.addMessage(p.getName() + " skipped");
                }
            }
        }
        this.next();
    }

    public void processHu(Player player) {
        Tile tile = player.getHand().getNewTile();
        if (player != this.turnPlayer && this.lastPlayedTile != null) {
            tile = this.lastPlayedTile;
            if (this.turnPlayer != null && !this.turnPlayer.getTable().toList().isEmpty()
                    && this.turnPlayer.getTable().getLast().equals(this.lastPlayedTile)) {
                this.turnPlayer.getTable().removeLast();
            }
            if (player.getHand().getNewTile() == null) {
                player.addTile(tile);
            }
        }
        this.winningTile = tile;
        this.winner = player;
        this.ended = true;
        this.recordAction(player, "wins");
        this.statusText = player.getName() + " wins"
                + (tile == null ? "" : " on " + this.formatTile(tile))
                + ". Click View winning hand to inspect the result.";
        log.addMessage(player.getName() + " wins");
        System.out.println(player.getHand());
    }

    public void processChou(Player player) {
        player.setHuStatus();
        this.turnPlayer.getTable().removeLast();
        this.recordAction(player, "chow");
        log.addMessage(player.getName() + " chou");
        this.processHu(player);
    }

    public void processPung(Player player) {
        Tile claimedTile = this.turnPlayer.getTable().getLast();
        player.getHand().addPung(claimedTile);
        this.turnPlayer.getTable().removeLast();
        this.recordAction(player, "pung");
        this.gameTurn = new GameTurn(this.players, player);
        this.turnPlayer = gameTurn.next();
        this.turnPlayer.setPlayingStatus();
        this.turnPlayer.clearStatus();
        this.statusText = this.buildClaimTurnPrompt(player, "punged", claimedTile);
        log.addMessage(this.turnPlayer.getName() + " pung, now play 1 tile");
        this.action();
    }

    public void processKong(Player player) {
        Tile claimedTile = this.turnPlayer.getTable().getLast();
        if (player.getStatus().contains(PlayerStatusEnum.NORMAL_KONG)) {
            player.getHand().addNormalKong(claimedTile);
            this.turnPlayer.getTable().removeLast();
        } else if (player.getStatus().contains(PlayerStatusEnum.ADD_KONG)) {
            claimedTile = player.getHand().getNewTile();
            player.getHand().addAddKong();
        } else if (player.getStatus().contains(PlayerStatusEnum.HIDDEN_KONG)) {
            claimedTile = player.getHand().getNewTile();
            player.getHand().addHiddenKong();
        } else {
            throw new RuntimeException("Invalid kong");
        }
        this.recordAction(player, "kong");
        Tile drawnTile = this.getNextTile();
        if (drawnTile == null) {
            return;
        }
        player.addTile(drawnTile);
        player.setPlayingStatus();
        new PlayerStatusChecker(player, player.getHand().getNewTile());
        if (this.player.containsChouPungKong()) {
            this.statusText = "You konged " + this.formatTile(claimedTile)
                    + " and drew " + this.formatTile(drawnTile)
                    + ". Choose K Kong or S Skip.";
            log.addMessage("press k to kong or s to skip");
            return;
        }
        this.gameTurn = new GameTurn(this.players, player);
        this.turnPlayer = gameTurn.next();
        this.turnPlayer.setPlayingStatus();
        this.turnPlayer.clearStatus();
        this.statusText = this.buildClaimTurnPrompt(player, "konged", claimedTile);
        log.addMessage(this.turnPlayer.getName() + " kong, now play 1 tile");
        this.action();
    }

    public void processSkip(Player player) {
        player.clearStatus();
        gameTurn.getPlayerAfter(turnPlayer).setPlayingStatus();
        this.recordAction(player, "skip");
        log.addMessage(player.getName() + " skipped");
        this.next();
    }

    private void recordAction(Player player, String actionText) {
        this.lastActionPlayer = player;
        this.lastActionText = player.getName() + " " + actionText;
        this.actionVersion++;
        Toolkit.getDefaultToolkit().beep();
    }

    private void endDrawGame() {
        if (this.ended) {
            return;
        }
        log.addMessage("No more tiles");
        this.ended = true;
        this.lastActionPlayer = this.turnPlayer;
        this.lastActionText = "wall exhausted";
        this.statusText = "No more tiles. The hand ends in a draw.";
        this.actionVersion++;
        Toolkit.getDefaultToolkit().beep();
    }

    private void endErroredGame(Player actor, RuntimeException e) {
        if (this.ended) {
            return;
        }
        this.ended = true;
        this.lastActionPlayer = actor;
        this.lastActionText = actor.getName() + " error";
        this.statusText = actor.getName() + " hit an AI/action error: " + e.getMessage()
                + ". Start a new game.";
        this.actionVersion++;
        log.addMessage(this.statusText);
        e.printStackTrace();
        Toolkit.getDefaultToolkit().beep();
    }

    private String buildPlayerTurnPrompt(Tile drawnTile) {
        Tile currentNewTile = drawnTile != null ? drawnTile : this.player.getHand().getNewTile();
        StringBuilder prompt = new StringBuilder();
        if (drawnTile != null && lastDiscardPlayer != null && lastDiscardPlayer != this.player && lastPlayedTile != null) {
            prompt.append(lastDiscardPlayer.getName())
                    .append(" played ")
                    .append(this.formatTile(lastPlayedTile))
                    .append("; you cannot respond, drew ")
                    .append(this.formatTile(drawnTile))
                    .append(". ");
        } else if (currentNewTile != null) {
            prompt.append("You drew ")
                    .append(this.formatTile(currentNewTile))
                    .append(". ");
        }
        prompt.append("Click one tile to discard.");

        List<String> actions = new ArrayList<>();
        if (this.player.getStatus().contains(PlayerStatusEnum.HU)) {
            actions.add("H Hu");
        }
        if (this.player.containsKong()) {
            actions.add("K Kong");
            actions.add("S Skip");
        }
        if (!actions.isEmpty()) {
            prompt.append(" Available: ").append(String.join(" / ", actions)).append(".");
        }
        return prompt.toString();
    }

    private String buildResponsePrompt(Player actor, Tile tile) {
        List<String> actions = new ArrayList<>();
        if (this.player.containsHu()) {
            actions.add("H Hu");
        }
        if (this.player.containsChow()) {
            actions.add("C Chow");
        }
        if (this.player.containsPung()) {
            actions.add("P Pung");
        }
        if (this.player.containsKong()) {
            actions.add("K Kong");
        }
        actions.add("S Skip");
        return actor.getName() + " played " + this.formatTile(tile)
                + "; choose response: " + String.join(" / ", actions) + ".";
    }

    private String buildClaimTurnPrompt(Player player, String action, Tile tile) {
        if (player == this.player) {
            return "You " + action + " " + this.formatTile(tile) + ". Click one tile to discard.";
        }
        return player.getName() + " " + action + " " + this.formatTile(tile) + " and must discard.";
    }

    private String formatTile(Tile tile) {
        if (tile == null) {
            return "";
        }
        String[] chineseNumbers = {"", "一", "二", "三", "四", "五", "六", "七", "八", "九"};
        if (tile.getType() == TileTypeEnum.B && tile.getNumber() == 1) {
            return "幺鸡";
        }
        return chineseNumbers[tile.getNumber()] + tile.getType().getChinese();
    }

    public void showInvalidInput(String message) {
        this.statusText = message;
        Toolkit.getDefaultToolkit().beep();
    }


    // getters
    public GameState getGameState() {
        HandTiles playerHand = this.players.get(0).getHand();
        HandTiles ai1Hand = this.players.get(1).getHand();
        HandTiles ai2Hand = this.players.get(2).getHand();
        HandTiles ai3Hand = this.players.get(3).getHand();

        List<Tile> playerHandList = playerHand.toList();
        List<Tile> ai1HandList = ai1Hand.toList();
        List<Tile> ai2HandList = ai2Hand.toList();
        List<Tile> ai3HandList = ai3Hand.toList();

        List<Tile> playerTable = this.players.get(0).getTable().toList();
        List<Tile> ai1Table = this.players.get(1).getTable().toList();
        List<Tile> ai2Table = this.players.get(2).getTable().toList();
        List<Tile> ai3Table = this.players.get(3).getTable().toList();

        List<Tile> kong = new ArrayList<>();
        playerHand.getKong().forEach(group -> kong.addAll(group.toList()));
        List<Tile> pung = new ArrayList<>();
        playerHand.getPung().forEach(group -> pung.addAll(group.toList()));
        Tile newTile = playerHand.getNewTile();

        return new GameState(this.turnPlayer, this.players, gameTurn.getRound(), playerHandList,
                kong, pung, newTile, playerTable, ai1Table, ai2Table, ai3Table);
    }

    public List<Player> getPlayers() {
        return players;
    }

    public Log getLog() {
        return log;
    }

    public boolean isOver() {
        return ended;
    }

    public Player getTurnPlayer() {
        return turnPlayer;
    }

    public Player getLastActionPlayer() {
        return lastActionPlayer;
    }

    public Player getWinner() {
        return winner;
    }

    public Tile getWinningTile() {
        return winningTile;
    }

    public String getLastActionText() {
        return lastActionText;
    }

    public String getStatusText() {
        return statusText;
    }

    public int getActionVersion() {
        return actionVersion;
    }

}
