package application.gameframe;

import application.config.Config;
import model.basic.Entity;
import model.basic.Tile;
import model.players.Player;
import model.tiles.Group;
import model.tiles.GroupEnum;

import javax.imageio.ImageIO;
import java.awt.*;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Map;

public class Drawer {

    private final Graphics2D g2;
    private final int width;
    private final int height;
    private final TileImageLoader imageLoader;
    private static BufferedImage photoBackground;
    private static boolean photoBackgroundLoadAttempted = false;

    public Drawer(Graphics2D g2, int width, int height, TileImageLoader imageLoader) {
        this.g2 = g2;
        this.width = width;
        this.height = height;
        this.imageLoader = imageLoader;
    }

    public void drawBackground() {
        if (Config.USE_PHOTO_BACKGROUND && drawPhotoBackground()) {
            return;
        }
        drawFeltBackground();
    }

    private boolean drawPhotoBackground() {
        BufferedImage background = getPhotoBackground();
        if (background == null) {
            return false;
        }

        int sourceWidth = background.getWidth();
        int sourceHeight = background.getHeight();
        double scale = Math.max(this.width / (double) sourceWidth, this.height / (double) sourceHeight);
        int scaledWidth = (int) Math.ceil(sourceWidth * scale);
        int scaledHeight = (int) Math.ceil(sourceHeight * scale);
        int x = (this.width - scaledWidth) / 2;
        int y = (this.height - scaledHeight) / 2;

        g2.drawImage(background, x, y, scaledWidth, scaledHeight, null);

        g2.setColor(new Color(0, 20, 32, 120));
        g2.fillRect(0, 0, this.width, this.height);
        g2.setColor(new Color(0, 0, 0, 45));
        g2.fillRect(0, 0, this.width, this.height);
        return true;
    }

    private BufferedImage getPhotoBackground() {
        if (!photoBackgroundLoadAttempted) {
            photoBackgroundLoadAttempted = true;
            try {
                photoBackground = ImageIO.read(Paths.get(Config.BACKGROUND_IMAGE_PATH).toFile());
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        return photoBackground;
    }

    private void drawFeltBackground() {
        Color baseGreen = new Color(30, 100, 60);
        Color highlightGreen = new Color(60, 130, 80);

        // Draw the base background
        g2.setColor(baseGreen);
        g2.fillRect(0, 0, this.width, this.height);

        // Draw "felt" texture
        int rectSize = 100; // Size of the small rectangles used for texture
        g2.setColor(highlightGreen);

        for (int y = 0; y < this.height; y += rectSize) {
            for (int x = y % (2 * rectSize); x < this.width; x += 2 * rectSize) {
                g2.fillRect(x, y, rectSize, rectSize);
            }
        }
    }

    public void drawLogs(List<String> logs) {
        g2.setFont(new Font("Arial", Font.PLAIN, 15));
        FontMetrics metrics = g2.getFontMetrics();
        int lineHeight = metrics.getHeight();
        int top = 140;
        int bottomPadding = 80;
        int logWindowWidth = 400;
        int logWindowX = this.width - logWindowWidth - 24;
        int logWindowHeight = this.height - top - bottomPadding;
        int textPadding = 20;
        int textX = logWindowX + textPadding;
        int textY = top + textPadding;
        int textWidth = logWindowWidth - textPadding * 2;

        g2.setColor(Color.BLACK);
        g2.fillRect(logWindowX, top, logWindowWidth, logWindowHeight);
        g2.setColor(Color.WHITE);

        List<String> wrappedLines = wrapLogLines(logs, metrics, textWidth);
        int maxVisibleLines = Math.max(0, (logWindowHeight - textPadding * 2) / lineHeight);
        int start = Math.max(0, wrappedLines.size() - maxVisibleLines);
        for (int i = start; i < wrappedLines.size(); i++) {
            int visibleIndex = i - start;
            g2.drawString(wrappedLines.get(i), textX, textY + visibleIndex * lineHeight);
        }
    }

    private List<String> wrapLogLines(List<String> logs, FontMetrics metrics, int maxWidth) {
        List<String> lines = new ArrayList<>();
        for (String log : logs) {
            lines.addAll(wrapText(log, metrics, maxWidth));
        }
        return lines;
    }

    private List<String> wrapText(String text, FontMetrics metrics, int maxWidth) {
        List<String> lines = new ArrayList<>();
        if (text == null || text.isEmpty()) {
            lines.add("");
            return lines;
        }

        String[] words = text.split(" ");
        StringBuilder line = new StringBuilder();
        for (String word : words) {
            if (word.isEmpty()) {
                continue;
            }
            String next = line.length() == 0 ? word : line + " " + word;
            if (metrics.stringWidth(next) <= maxWidth) {
                line = new StringBuilder(next);
            } else {
                if (line.length() > 0) {
                    lines.add(line.toString());
                }
                line = new StringBuilder(word);
                while (metrics.stringWidth(line.toString()) > maxWidth && line.length() > 1) {
                    int split = findFittingPrefixLength(line.toString(), metrics, maxWidth);
                    lines.add(line.substring(0, split));
                    line = new StringBuilder(line.substring(split));
                }
            }
        }
        if (line.length() > 0) {
            lines.add(line.toString());
        }
        return lines;
    }

    private int findFittingPrefixLength(String text, FontMetrics metrics, int maxWidth) {
        for (int i = text.length(); i > 1; i--) {
            if (metrics.stringWidth(text.substring(0, i)) <= maxWidth) {
                return i;
            }
        }
        return 1;
    }

    public void drawRect(Entity entity) {
        g2.setColor(Color.WHITE);
        g2.drawRect((int) entity.x, (int) entity.y, entity.width, entity.height);
    }

    public void drawInstructions() {
        g2.setColor(Color.WHITE);
        g2.setFont(new Font("Arial", Font.PLAIN, 18));
        g2.drawString("Press 'H' to Hu", 30, 430);
        g2.drawString("Press 'C' to Chi", 30, 450);
        g2.drawString("Press 'P' to Pung", 30, 470);
        g2.drawString("Press 'K' to Kong", 30, 490);
        g2.drawString("Press 'S' to Skip", 30, 510);
    }

    public void drawHelperBoxes(Player turnPlayer, Player lastActionPlayer, String lastActionText, String statusText,
                                Player winner, Rectangle winningHandButtonBounds,
                                Map<String, Rectangle> actionButtonBounds,
                                Map<String, Rectangle> endGameButtonBounds,
                                Rectangle llmPlayButtonBounds, boolean llmPlayEnabled,
                                Rectangle autoPlayButtonBounds, boolean autoPlayEnabled) {
        this.drawPlayerArea(new Entity(Config.PLAYER_HAND_X, Config.PLAYER_HAND_Y, Config.PLAYER_HAND_WIDTH, Config.PLAYER_HAND_HEIGHT),
                "YOU", 0, turnPlayer, lastActionPlayer);
        this.drawPlayerArea(new Entity(Config.PLAYER_TABLE_X, Config.PLAYER_TABLE_Y, Config.PLAYER_TABLE_WIDTH, Config.PLAYER_TABLE_HEIGHT),
                "YOUR DISCARDS", 0, turnPlayer, lastActionPlayer);
        this.drawPlayerArea(new Entity(Config.AI1_TABLE_X, Config.AI1_TABLE_Y, Config.AI1_TABLE_WIDTH, Config.AI1_TABLE_HEIGHT),
                "NEXT: AI1", 1, turnPlayer, lastActionPlayer);
        this.drawPlayerArea(new Entity(Config.AI2_TABLE_X, Config.AI2_TABLE_Y, Config.AI2_TABLE_WIDTH, Config.AI2_TABLE_HEIGHT),
                "OPPOSITE: AI2", 2, turnPlayer, lastActionPlayer);
        this.drawPlayerArea(new Entity(Config.AI3_TABLE_X, Config.AI3_TABLE_Y, Config.AI3_TABLE_WIDTH, Config.AI3_TABLE_HEIGHT),
                "PREV: AI3", 3, turnPlayer, lastActionPlayer);

        this.drawStatusBanner(lastActionText, statusText, winner, winningHandButtonBounds,
                actionButtonBounds, endGameButtonBounds,
                llmPlayButtonBounds, llmPlayEnabled, autoPlayButtonBounds, autoPlayEnabled);
    }

    private void drawStatusBanner(String lastActionText, String statusText, Player winner, Rectangle winningHandButtonBounds,
                                  Map<String, Rectangle> actionButtonBounds,
                                  Map<String, Rectangle> endGameButtonBounds,
                                  Rectangle llmPlayButtonBounds, boolean llmPlayEnabled,
                                  Rectangle autoPlayButtonBounds, boolean autoPlayEnabled) {
        int x = 25;
        int y = 25;
        int width = this.width - 50;
        int height = 95;

        g2.setColor(new Color(0, 0, 0, 105));
        g2.fillRect(x, y, width, height);
        g2.setColor(new Color(255, 245, 120));
        g2.setStroke(new BasicStroke(2));
        g2.drawRect(x, y, width, height);
        g2.setStroke(new BasicStroke(1));

        g2.setFont(new Font("Arial", Font.BOLD, 20));
        g2.setColor(new Color(255, 245, 120));
        if (lastActionText != null && !lastActionText.isEmpty()) {
            g2.drawString("Last action: " + lastActionText, x + 14, y + 30);
        }

        g2.setFont(new Font("Arial", Font.PLAIN, 18));
        g2.setColor(Color.WHITE);
        int statusTextWidth = width - 28;
        if (llmPlayButtonBounds != null && !llmPlayButtonBounds.isEmpty()) {
            statusTextWidth = Math.min(statusTextWidth, llmPlayButtonBounds.x - x - 28);
        }
        if (autoPlayButtonBounds != null && !autoPlayButtonBounds.isEmpty()) {
            statusTextWidth = Math.min(statusTextWidth, autoPlayButtonBounds.x - x - 28);
        }
        if (endGameButtonBounds != null && !endGameButtonBounds.isEmpty()) {
            int leftMostButtonX = endGameButtonBounds.values().stream()
                    .mapToInt(bounds -> bounds.x)
                    .min()
                    .orElse(x + width);
            statusTextWidth = Math.min(statusTextWidth, leftMostButtonX - x - 28);
        } else if (winner != null && winningHandButtonBounds != null) {
            statusTextWidth = Math.min(statusTextWidth, winningHandButtonBounds.x - x - 28);
        } else if (actionButtonBounds != null && !actionButtonBounds.isEmpty()) {
            int leftMostButtonX = actionButtonBounds.values().stream()
                    .mapToInt(bounds -> bounds.x)
                    .min()
                    .orElse(x + width);
            statusTextWidth = Math.min(statusTextWidth, leftMostButtonX - x - 28);
        }
        this.drawWrappedText(statusText, x + 14, y + 58, statusTextWidth, 22, 2);

        if (winner != null && winningHandButtonBounds != null) {
            this.drawActionButton("View winning hand", winningHandButtonBounds);
        }

        if (endGameButtonBounds != null && !endGameButtonBounds.isEmpty()) {
            for (Map.Entry<String, Rectangle> entry : endGameButtonBounds.entrySet()) {
                this.drawActionButton(entry.getKey(), entry.getValue());
            }
        }

        if (winner == null && actionButtonBounds != null && !actionButtonBounds.isEmpty()) {
            for (Map.Entry<String, Rectangle> entry : actionButtonBounds.entrySet()) {
                this.drawActionButton(entry.getKey(), entry.getValue());
            }
        }

        if (autoPlayButtonBounds != null && !autoPlayButtonBounds.isEmpty()) {
            this.drawModeButton(autoPlayButtonBounds, autoPlayEnabled, llmPlayEnabled,
                    "Auto Play", "Auto Play: ON");
        }

        if (llmPlayButtonBounds != null && !llmPlayButtonBounds.isEmpty()) {
            this.drawModeButton(llmPlayButtonBounds, llmPlayEnabled, autoPlayEnabled,
                    "LLM Play", "LLM Play: ON");
        }
    }

    private void drawActionButton(String label, Rectangle bounds) {
        g2.setColor(new Color(0, 0, 0, 150));
        g2.fillRoundRect(bounds.x + 4, bounds.y + 4, bounds.width, bounds.height, 6, 6);

        g2.setColor(new Color(255, 242, 78));
        g2.fillRoundRect(bounds.x, bounds.y, bounds.width, bounds.height, 6, 6);
        g2.setColor(new Color(20, 35, 30));
        g2.setStroke(new BasicStroke(2));
        g2.drawRoundRect(bounds.x, bounds.y, bounds.width, bounds.height, 6, 6);
        g2.setStroke(new BasicStroke(1));

        g2.setFont(new Font("Arial", Font.BOLD, 15));
        FontMetrics metrics = g2.getFontMetrics();
        int textX = bounds.x + (bounds.width - metrics.stringWidth(label)) / 2;
        int textY = bounds.y + (bounds.height + metrics.getAscent() - metrics.getDescent()) / 2;
        g2.drawString(label, textX, textY);
    }

    private void drawModeButton(Rectangle bounds, boolean enabled, boolean disabled,
                                String offLabel, String onLabel) {
        g2.setColor(new Color(0, 0, 0, 150));
        g2.fillRoundRect(bounds.x + 4, bounds.y + 4, bounds.width, bounds.height, 8, 8);

        if (disabled) {
            g2.setColor(new Color(210, 205, 155));
        } else {
            g2.setColor(enabled ? new Color(104, 220, 95) : new Color(255, 242, 78));
        }
        g2.fillRoundRect(bounds.x, bounds.y, bounds.width, bounds.height, 8, 8);
        g2.setColor(disabled ? new Color(110, 110, 100)
                : enabled ? new Color(65, 150, 255) : new Color(20, 35, 30));
        g2.setStroke(new BasicStroke(3));
        g2.drawRoundRect(bounds.x, bounds.y, bounds.width, bounds.height, 8, 8);
        g2.setStroke(new BasicStroke(1));

        String label = enabled ? onLabel : offLabel;
        g2.setColor(disabled ? new Color(120, 120, 120) : new Color(20, 35, 30));
        g2.setFont(new Font("Arial", Font.BOLD, 15));
        FontMetrics metrics = g2.getFontMetrics();
        int textX = bounds.x + (bounds.width - metrics.stringWidth(label)) / 2;
        int textY = bounds.y + (bounds.height + metrics.getAscent() - metrics.getDescent()) / 2;
        g2.drawString(label, textX, textY);
    }

    private void drawWrappedText(String text, int x, int y, int maxWidth, int lineHeight, int maxLines) {
        if (text == null || text.isEmpty()) {
            return;
        }
        FontMetrics metrics = g2.getFontMetrics();
        String[] words = text.split(" ");
        StringBuilder line = new StringBuilder();
        int linesDrawn = 0;
        for (String word : words) {
            String nextLine = line.length() == 0 ? word : line + " " + word;
            if (metrics.stringWidth(nextLine) <= maxWidth) {
                line = new StringBuilder(nextLine);
            } else {
                g2.drawString(line.toString(), x, y + linesDrawn * lineHeight);
                linesDrawn++;
                if (linesDrawn == maxLines) {
                    return;
                }
                line = new StringBuilder(word);
            }
        }
        if (line.length() > 0 && linesDrawn < maxLines) {
            g2.drawString(line.toString(), x, y + linesDrawn * lineHeight);
        }
    }

    private void drawPlayerArea(Entity entity, String label, int playerPosition, Player turnPlayer, Player lastActionPlayer) {
        boolean isCurrentTurn = turnPlayer != null && turnPlayer.getPosition() == playerPosition;
        boolean isLastAction = lastActionPlayer != null && lastActionPlayer.getPosition() == playerPosition;

        if (isLastAction) {
            g2.setColor(new Color(255, 225, 80, 70));
            g2.fillRect((int) entity.x, (int) entity.y, entity.width, entity.height);
        } else if (isCurrentTurn) {
            g2.setColor(new Color(90, 190, 255, 45));
            g2.fillRect((int) entity.x, (int) entity.y, entity.width, entity.height);
        }

        if (isLastAction) {
            g2.setColor(new Color(255, 225, 80));
            g2.setStroke(new BasicStroke(4));
        } else if (isCurrentTurn) {
            g2.setColor(new Color(90, 190, 255));
            g2.setStroke(new BasicStroke(3));
        } else {
            g2.setColor(Color.WHITE);
            g2.setStroke(new BasicStroke(1));
        }
        g2.drawRect((int) entity.x, (int) entity.y, entity.width, entity.height);
        g2.setStroke(new BasicStroke(1));

        g2.setFont(new Font("Arial", Font.BOLD, 15));
        g2.setColor(Color.WHITE);
        g2.drawString(label, (int) entity.x + 10, (int) entity.y + 22);
    }

    public void drawTile(Tile tile, Color color) {
        Image image = null;
        if (tile.width == Config.TILE_WIDTH) {
            image = imageLoader.getImage(tile);
        } else if (tile.width == Config.TABLE_TILE_WIDTH) {
            image = imageLoader.getTableImage(tile);
        }
        assert image != null;
        g2.setColor(color);
        g2.fillRect((int) tile.x, (int) tile.y, tile.width, tile.height);
        g2.drawImage(image, (int) tile.x, (int) tile.y, null);
    }

    public void drawTable(List<Tile> tiles, int playerPosition) {
        if (playerPosition == 0) {
            for (int i = 0; i < tiles.size(); i++) {
                Tile tile = tiles.get(i);
                tile.x = Config.PLAYER_TABLE_X + Config.TABLE_TILE_PADDING * i + Config.TABLE_TILE_WIDTH * i;
                tile.y = Config.PLAYER_TABLE_Y;
                tile.width = Config.TABLE_TILE_WIDTH;
                tile.height = Config.TABLE_TILE_HEIGHT;
                this.drawTile(tile, Color.WHITE);
            }
        } else {
            final List<Integer> aiTableX = Arrays.asList(Config.AI1_TABLE_X, Config.AI2_TABLE_X, Config.AI3_TABLE_X);
            int currentLine = 0;
            int currentTile = 0;
            for (Tile tile : tiles) {
                tile.x = aiTableX.get(playerPosition - 1) + Config.TABLE_TILE_PADDING * currentTile + Config.TABLE_TILE_WIDTH * currentTile;
                tile.y = Config.AI_TABLE_Y + Config.TABLE_TILE_HEIGHT * currentLine + Config.TABLE_TILE_PADDING * currentLine;
                tile.width = Config.TABLE_TILE_WIDTH;
                tile.height = Config.TABLE_TILE_HEIGHT;
                this.drawTile(tile, Color.WHITE);
                currentTile++;
                if (currentTile == Config.AI_TABLE_NUM_TILES_PER_LINE) {
                    currentLine++;
                    currentTile = 0;
                }
            }
        }
    }

    public void drawPungKong(List<Group> pungKong, int playerPosition) {
        if (pungKong.size() <= 0) {
            return;
        }
        final List<Integer> pungKongX = Arrays.asList(
                Config.PLAYER_TABLE_X + Config.PLAYER_TABLE_WIDTH,
                Config.AI1_TABLE_X + Config.AI_TABLE_WIDTH,
                Config.AI2_TABLE_X + Config.AI_TABLE_WIDTH,
                Config.AI3_TABLE_X + Config.AI_TABLE_WIDTH);

        final List<Integer> pungKongY = Arrays.asList(
                Config.PLAYER_TABLE_Y, Config.AI1_TABLE_Y, Config.AI2_TABLE_Y, Config.AI3_TABLE_Y);
        Color color;
        for (int i = 0; i < pungKong.size(); i++) {
            Group group = pungKong.get(i);
            if (group.getCategory() == GroupEnum.PUNG) {
                color = Color.YELLOW;
            } else {
                color = Color.ORANGE;
            }
            int totalTilesInGroup = group.toList().size();
            int groupX = pungKongX.get(playerPosition);
            for (int j = 0; j < totalTilesInGroup; j++) {
                Tile tile = group.toList().get(j);
                tile.x = groupX + Config.TABLE_TILE_WIDTH * j;
                tile.y = pungKongY.get(playerPosition) + Config.TABLE_TILE_HEIGHT * i + Config.TABLE_TILE_PADDING * i;
                tile.width = Config.TABLE_TILE_WIDTH;
                tile.height = Config.TABLE_TILE_HEIGHT;
                this.drawTile(tile, color);
            }
        }
    }
    public List<Tile> drawPlayerHand(List<Tile> hand, Tile newTile) {
        for (int i = 0; i < hand.size(); i++) {
            Tile tile = hand.get(i);
            tile.x = Config.PLAYER_HAND_X + Config.PLAYER_HAND_TILE_PADDING * i + Config.TILE_WIDTH * i;
            tile.y = Config.PLAYER_HAND_TOP_INDENT;
            tile.width = Config.TILE_WIDTH;
            tile.height = Config.TILE_HEIGHT;
            this.drawTile(tile, Color.WHITE);
        }

        if (newTile != null) {
            newTile.x = Config.PLAYER_HAND_X + Config.PLAYER_HAND_TILE_PADDING * hand.size()
                    + Config.TILE_WIDTH * hand.size()
                    + Config.FOURTEENTH_TILE_INDENT;
            newTile.y = Config.PLAYER_HAND_TOP_INDENT;
            newTile.width = Config.TILE_WIDTH;
            newTile.height = Config.TILE_HEIGHT;
            this.drawTile(newTile, Color.WHITE);
        }
        return new ArrayList<Tile>() {{
            addAll(hand);
            if (newTile != null) {
                add(newTile);
            }
        }};
    }
}
