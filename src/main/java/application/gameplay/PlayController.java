package application.gameplay;

import application.game.Game;
import model.players.Player;

public interface PlayController {
    String getName();

    PlayDecision choose(Game game, Player player);
}
